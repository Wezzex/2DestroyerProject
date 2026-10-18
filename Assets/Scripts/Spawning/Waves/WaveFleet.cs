using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum JumpOrder
{
    Simultaneous, // the whole wave charges and jumps at once
    FrontFirst    // ships at the front jump first, the rest follow in succession
}

[Serializable]
public class FleetSettings
{
    [Tooltip("Within this distance of the target the wave fights; further away it travels by sub-light.")]
    public float engageDistance = 300f;
    [Tooltip("A sub-light jump aims to stop this far short of the target.")]
    public float arrivalStandoff = 150f;
    [Tooltip("In combat the front of the formation closes to this distance from the target.")]
    public float engageStandoff = 60f;
    [Tooltip("Ships drop out of sub-light early once the target is this close (inside firing range).")]
    public float dropOutRange = 120f;
    [Tooltip("Route legs shorter than this are flown at normal speed. Keep it well above the drive's " +
             "spin-up + braking distance, or ships overshoot short jumps.")]
    public float minJumpDistance = 400f;

    [Header("Form up and align")]
    public float formUpTolerance = 15f;
    public float formUpTimeout = 25f;
    [Tooltip("Degrees every ship must be within of the jump direction before charging.")]
    public float alignTolerance = 8f;
    public float alignTimeout = 20f;

    [Header("Jump")]
    public JumpOrder jumpOrder = JumpOrder.FrontFirst;
    [Tooltip("FrontFirst: seconds between each ship starting its charge.")]
    public float jumpStagger = 0.5f;
}

// Commands one reinforcement wave (created by WaveDirector) as a formation.
//  * Combat: close on the target in formation; per the EnemyDoctrine some wingmen may break off.
//  * Travel: if the target is far away, plan a route and fly it one straight sub-light leg at a time:
//    form up -> align with the leg -> charge and jump (together or front first) -> drop out -> next leg.
//  * The wave can't see a target that is in sub-light. It flies to where the target was last seen, and gets a
//    fresh position when the target drops out or any station spots it (WaveDirector reports sightings).
public class WaveFleet : MonoBehaviour
{
    private enum Phase { Combat, FormUp, Align, Jump }

    private class Ship
    {
        public GameObject root;
        public Transform body;
        public FormationMember member;
        public SubLightDrive drive;
        public UnitManager unit;
        public Vector3 slot;
        public float jumpAt;
        public bool bJumpStarted;
    }

    private static readonly List<WaveFleet> activeFleets = new List<WaveFleet>();

    private readonly List<Ship> ships = new List<Ship>();
    private NavMeshPath path;
    private FormationShape shape;
    private float spacing;
    private EnemyDoctrine doctrine;
    private FleetSettings settings;

    private Transform target;
    private SubLightDrive targetDrive;
    private bool hasIntel;
    private Vector3 lastKnownPosition;
    private Vector3 holdAnchor;
    private Vector3 holdFacing = Vector3.forward;

    private Phase phase = Phase.Combat;
    private float phaseStartTime;
    private Vector3 legDirection;
    private float legDistance;

    private float Threat01 => ThreatMeter.Instance != null ? ThreatMeter.Instance.Normalized : 0f;
    private Transform Leader => ships[0].body;

    public void Initialize(List<GameObject> units, FormationShape formation, float formationSpacing, Transform waveTarget, EnemyDoctrine waveDoctrine, FleetSettings fleetSettings)
    {
        shape = formation;
        spacing = formationSpacing;
        doctrine = waveDoctrine != null ? waveDoctrine : EnemyDoctrine.Fallback;
        settings = fleetSettings ?? new FleetSettings();
        path = new NavMeshPath();

        foreach (GameObject unit in units)
        {
            FormationMember member = unit.GetComponent<FormationMember>();
            if (member == null) member = unit.AddComponent<FormationMember>();
            if (!member.TryClaim(this)) continue;

            Transform body = UnitSetup.GetBody(unit);
            ships.Add(new Ship
            {
                root = unit,
                body = body,
                member = member,
                drive = body.GetComponent<SubLightDrive>(),
                unit = unit.GetComponentInChildren<UnitManager>(true),
            });
        }

        if (ships.Count > 0)
        {
            holdAnchor = Flat(Leader.position);
            holdFacing = FlatDirection(Leader.forward, Vector3.forward);
        }

        SetTarget(waveTarget);
    }

    // A station spotted the target: everyone gets its position, even while it is in sub-light.
    public static void ReportSightingToAll(Transform sighted)
    {
        foreach (WaveFleet fleet in activeFleets)
        {
            if (fleet.target == null || fleet.target == sighted) fleet.ReportSighting(sighted);
        }
    }

    public void ReportSighting(Transform sighted)
    {
        if (sighted == null) return;
        if (target != sighted) SetTarget(sighted);

        lastKnownPosition = sighted.position;
        hasIntel = true;
    }

    private void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetDrive = null;
        if (target != null)
        {
            targetDrive = target.GetComponentInParent<SubLightDrive>();
            if (targetDrive == null) targetDrive = target.GetComponentInChildren<SubLightDrive>();
        }
        UpdateIntel();
    }

    private void OnEnable() => activeFleets.Add(this);
    private void OnDisable() => activeFleets.Remove(this);

    private void OnDestroy()
    {
        foreach (Ship ship in ships)
        {
            if (ship.member != null) ship.member.Release(this);
        }
    }

    private void Update()
    {
        RemoveLostShips();
        if (ships.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        UpdateIntel();

        switch (phase)
        {
            case Phase.Combat: UpdateCombat(); break;
            case Phase.FormUp: UpdateFormUp(); break;
            case Phase.Align: UpdateAlign(); break;
            case Phase.Jump: UpdateJump(); break;
        }
    }

    // ---------------- Intel ----------------

    private void UpdateIntel()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            LoseIntel();
            return;
        }

        // A target in sub-light can't be tracked; keep flying to where it was last seen.
        if (targetDrive != null && targetDrive.IsCruising) return;

        lastKnownPosition = target.position;
        hasIntel = true;
    }

    private void LoseIntel()
    {
        if (!hasIntel || ships.Count == 0) return;

        hasIntel = false;
        holdAnchor = Flat(Leader.position);
        holdFacing = FlatDirection(lastKnownPosition - Leader.position, holdFacing);
    }

    // ---------------- Combat ----------------

    private void UpdateCombat()
    {
        if (hasIntel && FlatDistance(Leader.position, lastKnownPosition) > settings.engageDistance && TryPlanLeg())
        {
            BeginFormUp();
            return;
        }

        Vector3 anchor;
        Vector3 facing;
        if (hasIntel)
        {
            facing = FlatDirection(lastKnownPosition - Leader.position, holdFacing);
            anchor = Flat(lastKnownPosition) - facing * settings.engageStandoff;
            holdFacing = facing;
        }
        else
        {
            // Nothing to chase: hold formation, facing where the target was last heading.
            facing = holdFacing;
            anchor = holdAnchor;
        }

        List<Vector3> slots = FormationUtility.GetWorldPositions(shape, ships.Count, spacing, anchor, facing);

        // Waves are sent to fight, so they always close in. Discipline decides whether wingmen break off.
        int breakers = hasIntel ? doctrine.GetBreakCount(ships.Count - 1, Threat01) : 0;

        for (int i = 0; i < ships.Count; i++)
        {
            Vector3 shipFacing = hasIntel ? lastKnownPosition - slots[i] : facing;
            ships[i].member.SetSlot(slots[i], shipFacing);
            ships[i].member.SetBreaking(i > 0 && i >= ships.Count - breakers, target);
        }
    }

    // First straight leg of a NavMesh route to just short of the target (routes bend around stations).
    private bool TryPlanLeg()
    {
        // A ship without a drive could never make the jump; the wave flies there at normal speed instead.
        if (!ships.TrueForAll(s => s.drive != null)) return false;

        Vector3 start = Leader.position;
        Vector3 toTarget = Flat(lastKnownPosition - start);
        Vector3 destination = lastKnownPosition - toTarget.normalized * settings.arrivalStandoff;

        if (!NavMeshUtility.TrySample(start, NavMeshUtility.DefaultSearchRadius, NavMesh.AllAreas, out Vector3 navStart) ||
            !NavMeshUtility.TrySample(destination, NavMeshUtility.DefaultSearchRadius, NavMesh.AllAreas, out Vector3 navEnd) ||
            !NavMesh.CalculatePath(navStart, navEnd, NavMesh.AllAreas, path) ||
            path.corners.Length < 2)
        {
            return false;
        }

        Vector3 leg = Flat(path.corners[1] - start);
        if (leg.magnitude < settings.minJumpDistance) return false; // short hop: just fly it in formation

        legDirection = leg.normalized;
        legDistance = leg.magnitude;
        return true;
    }

    // ---------------- Travel ----------------

    private void BeginFormUp()
    {
        SetPhase(Phase.FormUp);

        List<Vector3> slots = FormationUtility.GetWorldPositions(shape, ships.Count, spacing, Flat(Leader.position), legDirection);
        for (int i = 0; i < ships.Count; i++)
        {
            ships[i].slot = slots[i];
            ships[i].bJumpStarted = false;
            ships[i].member.SetBreaking(false, null);
            ships[i].member.SetSlot(slots[i], legDirection);
        }
    }

    private void UpdateFormUp()
    {
        bool formed = true;
        foreach (Ship ship in ships)
        {
            if (FlatDistance(ship.body.position, ship.slot) > settings.formUpTolerance) formed = false;
        }

        if (formed || PhaseTime > settings.formUpTimeout) SetPhase(Phase.Align);
    }

    // Ships at their slot turn on the spot (AIHoldFormationBehaviour) to face the leg.
    private void UpdateAlign()
    {
        bool aligned = true;
        foreach (Ship ship in ships)
        {
            if (Vector3.Angle(Flat(ship.body.forward), legDirection) > settings.alignTolerance) aligned = false;
        }

        if (!aligned && PhaseTime <= settings.alignTimeout) return;

        // The target may have come to us while we were forming up.
        if (!hasIntel || FlatDistance(Leader.position, lastKnownPosition) <= settings.engageDistance)
        {
            SetPhase(Phase.Combat);
            return;
        }

        BeginJump();
    }

    private void BeginJump()
    {
        SetPhase(Phase.Jump);

        // Front first: the ship furthest along the leg goes first, so nobody flies through a squadmate.
        var order = new List<Ship>(ships);
        if (settings.jumpOrder == JumpOrder.FrontFirst)
        {
            Vector3 origin = Flat(Leader.position);
            order.Sort((a, b) => Vector3.Dot(b.body.position - origin, legDirection).CompareTo(Vector3.Dot(a.body.position - origin, legDirection)));
        }

        for (int i = 0; i < order.Count; i++)
        {
            order[i].jumpAt = Time.time + (settings.jumpOrder == JumpOrder.FrontFirst ? i * settings.jumpStagger : 0f);
            order[i].bJumpStarted = false;
        }
    }

    private void UpdateJump()
    {
        bool finished = true;

        foreach (Ship ship in ships)
        {
            if (!ship.bJumpStarted)
            {
                finished = false;
                if (Time.time < ship.jumpAt) continue;

                // Same distance for everyone keeps the formation. Its slot becomes its arrival point.
                ship.bJumpStarted = true;
                Vector3 arrival = Flat(ship.body.position) + legDirection * legDistance;
                ship.member.SetSlot(arrival, legDirection);
                if (ship.drive != null) ship.drive.TryBeginCharge(legDistance);
                continue;
            }

            if (ship.drive == null || !ship.drive.IsActive) continue;
            finished = false;

            // Drop out as soon as the target is within firing range.
            if (ship.drive.IsCruising && hasIntel && FlatDistance(ship.body.position, lastKnownPosition) <= settings.dropOutRange)
            {
                ship.drive.Exit();
            }
        }

        // Everyone is out of sub-light: fight, or plan the next leg (Combat decides).
        if (finished) SetPhase(Phase.Combat);
    }

    // ---------------- Helpers ----------------

    private void RemoveLostShips()
    {
        for (int i = ships.Count - 1; i >= 0; i--)
        {
            Ship ship = ships[i];
            if (ship.root != null && ship.unit != null && !ship.unit.IsDead && ship.root.activeInHierarchy) continue;

            if (ship.member != null) ship.member.Release(this);
            ships.RemoveAt(i);
        }
    }

    private void SetPhase(Phase next)
    {
        phase = next;
        phaseStartTime = Time.time;
    }

    private float PhaseTime => Time.time - phaseStartTime;

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    private static Vector3 FlatDirection(Vector3 vector, Vector3 fallback)
    {
        vector.y = 0f;
        return vector.sqrMagnitude > 0.0001f ? vector.normalized : fallback;
    }

    private static float FlatDistance(Vector3 a, Vector3 b) => NavMeshUtility.FlatDistance(a, b);

    private void OnDrawGizmosSelected()
    {
        if (ships.Count == 0) return;

        Gizmos.color = hasIntel ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(lastKnownPosition, 5f);
        Gizmos.DrawLine(Leader.position, lastKnownPosition);

        if (phase != Phase.Combat)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(Leader.position, Leader.position + legDirection * legDistance);
        }
    }
}
