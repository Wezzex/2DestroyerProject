using System;
using System.Collections.Generic;
using UnityEngine;

// Boid-style steering for squad units, layered on top of path following (FollowPlannedPath asks it to adjust
// the path direction every frame). Separation keeps squadmates apart, cohesion and alignment keep them together,
// and a soft pull towards a formation slot behind the leader replaces a rigid formation.
// While engaging, the formation pull is mostly released so each fighter can fly its own strafing runs.
// Lives on the moving body (Motor), next to FollowPlannedPath.
public class SquadFlocking : MonoBehaviour
{
    [Serializable]
    public struct Weights
    {
        [Tooltip("Push away from squadmates that are too close.")]
        public float separation;
        [Tooltip("Pull towards the centre of nearby squadmates.")]
        public float cohesion;
        [Tooltip("Match the heading of nearby squadmates.")]
        public float alignment;
        [Tooltip("Pull towards this unit's formation slot. Also controls how much the throttle is used to hold the slot.")]
        public float formation;
        [Tooltip("Gentle per-unit meandering, so squadmates don't fly in perfect lockstep.")]
        public float wander;
    }

    [SerializeField] private SquadMember member;

    [Header("Neighbours")]
    [SerializeField] private float separationRadius = 8f;
    [SerializeField] private float neighbourRadius = 40f;

    [Header("Weights (the path itself always has weight 1)")]
    [Tooltip("Patrolling or returning: fly together in a loose formation.")]
    [SerializeField] private Weights formationWeights = new Weights { separation = 1.5f, cohesion = 0.35f, alignment = 0.4f, formation = 0.6f, wander = 0.25f };
    [Tooltip("Engaging: free to strafe on their own, but still keep apart.")]
    [SerializeField] private Weights engagedWeights = new Weights { separation = 1.5f, cohesion = 0.1f, alignment = 0.05f, formation = 0.05f, wander = 0.3f };

    [Header("Looseness")]
    [Tooltip("Each slot drifts around by up to this many metres, so the formation breathes instead of holding rigid.")]
    [SerializeField] private float slotDrift = 4f;
    [Tooltip("How fast the slot drift and wander change.")]
    [SerializeField] private float wanderFrequency = 0.25f;

    [Header("Formation")]
    [Tooltip("Sideways distance from the slot at which the formation pull is at full strength.")]
    [SerializeField] private float formationSlack = 10f;
    [Tooltip("Wingmen steer at a point this far ahead of their slot, so they never turn back to reach it.")]
    [SerializeField] private float formationLeadDistance = 15f;
    [Tooltip("Leader throttle while in formation, leaving the wingmen some speed to catch up with.")]
    [SerializeField, Range(0.1f, 1f)] private float leaderThrottle = 0.8f;
    [Tooltip("Throttle change per metre a wingman is behind (+) or ahead of (-) its slot.")]
    [SerializeField] private float catchUpGain = 0.05f;
    [SerializeField, Range(0f, 1f)] private float minThrottle = 0.3f;

    private readonly List<SquadMember> squad = new List<SquadMember>();
    private List<Vector3> offsets;
    private FormationShape offsetsShape;
    private float offsetsSpacing;
    private int refreshedFrame = -1;
    private bool hasSquad;
    private int rank;
    private float noiseSeed;

    private void Awake()
    {
        if (member == null) member = GetComponentInParent<SquadMember>();
        noiseSeed = UnityEngine.Random.Range(0f, 1000f);
    }

    // Blends the path direction with the squad forces. throttle is a 0-1 multiplier for the path's thrust.
    public Vector3 Steer(Vector3 pathDirection, out float throttle)
    {
        throttle = 1f;
        if (!Refresh()) return pathDirection;

        Weights weights = member.OrderedToEngage ? engagedWeights : formationWeights;
        Vector3 position = Flat(transform.position);

        Vector3 separation = Vector3.zero;
        Vector3 centre = Vector3.zero;
        Vector3 heading = Vector3.zero;
        int neighbours = 0;

        foreach (SquadMember mate in squad)
        {
            if (mate == member || mate.Unit == null) continue;

            Transform body = mate.Unit.transform;
            Vector3 offset = Flat(body.position) - position;
            float distance = offset.magnitude;

            if (distance < separationRadius && distance > 0.001f)
            {
                separation -= offset / distance * (1f - distance / separationRadius);
            }
            if (distance < neighbourRadius)
            {
                centre += offset;
                heading += Flat(body.forward);
                neighbours++;
            }
        }

        Vector3 steer = pathDirection + separation * weights.separation;

        Vector3 right = Vector3.Cross(Vector3.up, Flat(transform.forward)).normalized;
        steer += right * Noise(0f) * weights.wander;

        if (neighbours > 0)
        {
            Vector3 toCentre = centre / neighbours;
            steer += Vector3.ClampMagnitude(toCentre / neighbourRadius, 1f) * weights.cohesion;
            if (heading.sqrMagnitude > 0.0001f) steer += heading.normalized * weights.alignment;
        }

        float formationBlend = Mathf.Clamp01(weights.formation);

        if (rank > 0)
        {
            Vector3 leaderForward = LeaderForward();
            Vector3 toSlot = GetSlot() - position;

            // Along the leader's heading the throttle holds the slot; sideways the steering does.
            // Turning to chase a slot that's behind would make wingmen loop around.
            float along = Vector3.Dot(toSlot, leaderForward);
            Vector3 sideways = toSlot - leaderForward * along;
            steer += Vector3.ClampMagnitude(sideways / formationSlack, 1f) * weights.formation;

            float slotThrottle = Mathf.Clamp(leaderThrottle + along * catchUpGain, minThrottle, 1f);
            throttle = Mathf.Lerp(1f, slotThrottle, formationBlend);
        }
        else
        {
            throttle = Mathf.Lerp(1f, leaderThrottle, formationBlend);
        }

        steer.y = 0f;
        return steer.sqrMagnitude > 0.0001f ? steer.normalized : pathDirection;
    }

    // Wingmen fly the leader's route instead of their own patrol points: a point just ahead of their slot.
    public bool TryGetFormationTarget(out Vector3 target)
    {
        target = transform.position;
        if (!Refresh() || rank == 0) return false;

        target = GetSlot() + LeaderForward() * formationLeadDistance;
        target.y = transform.position.y;
        return true;
    }

    // Squad lookup once per frame. False when alone, orphaned or not deployed.
    private bool Refresh()
    {
        if (refreshedFrame == Time.frameCount) return hasSquad;
        refreshedFrame = Time.frameCount;

        hasSquad = false;
        if (member == null || member.Owner == null) return false;

        member.Owner.GetDeployedSquad(member.SquadIndex, squad);
        rank = squad.IndexOf(member);
        if (rank < 0 || squad.Count < 2 || squad[0].Unit == null) return false;

        DeploySpawner owner = member.Owner;
        if (offsets == null || offsets.Count != squad.Count || offsetsShape != owner.Formation || !Mathf.Approximately(offsetsSpacing, owner.FormationSpacing))
        {
            offsetsShape = owner.Formation;
            offsetsSpacing = owner.FormationSpacing;
            offsets = FormationUtility.GetOffsets(offsetsShape, squad.Count, offsetsSpacing);
        }

        hasSquad = true;
        return true;
    }

    private Transform LeaderBody => squad[0].Unit.transform;

    private Vector3 LeaderForward()
    {
        Vector3 forward = Flat(LeaderBody.forward);
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    // Relative to the leader's own offset, so shapes without the leader at (0,0,0) (e.g. Line) still work.
    private Vector3 GetSlot()
    {
        Quaternion rotation = Quaternion.LookRotation(LeaderForward(), Vector3.up);
        Vector3 drift = new Vector3(Noise(50f), 0f, Noise(100f)) * slotDrift;
        return Flat(LeaderBody.position) + rotation * (offsets[rank] - offsets[0] + drift);
    }

    // Smooth per-unit noise in -1..1. Each channel uses its own offset so they don't move together.
    private float Noise(float channel)
    {
        return Mathf.PerlinNoise(noiseSeed + channel, Time.time * wanderFrequency) * 2f - 1f;
    }

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !Refresh() || rank == 0) return;

        Gizmos.color = Color.magenta;
        Vector3 slot = GetSlot();
        slot.y = transform.position.y;
        Gizmos.DrawWireSphere(slot, 1.5f);
        Gizmos.DrawLine(transform.position, slot);
    }
}
