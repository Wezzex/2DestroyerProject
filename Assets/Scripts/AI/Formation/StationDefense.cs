using System.Collections.Generic;
using UnityEngine;

// Commands a station's garrison (the units of a DeploySpawner) as a stationary formation instead of patrols.
//  * Idle: an even ring around the station, covering every angle, ships facing outwards.
//  * Alert (player in sensor range): a defensive arc on the player's side, facing the player.
//    The EnemyDoctrine and player threat decide whether the arc holds, advances behind its leader,
//    or lets some wingmen break formation to engage.
//  * Player gone: the arc stays, facing where the player was last seen, so the station's back is open.
//  * Another station raises the alarm: an idle, previously alerted station turns its arc towards that station.
public class StationDefense : MonoBehaviour, IDeployPlacement
{
    [Header("References")]
    [Tooltip("The station. Defaults to the UnitManager in parents/children.")]
    [SerializeField] private UnitManager owner;
    [Tooltip("The station's sensor. Defaults to the AIDetector under the owner.")]
    [SerializeField] private AIDetector sensor;
    [Tooltip("The garrison: the DeploySpawner whose units hold this formation.")]
    [SerializeField] private DeploySpawner garrison;
    [Tooltip("Leave empty to use the default doctrine.")]
    [SerializeField] private EnemyDoctrine doctrine;

    [Header("Idle ring")]
    [SerializeField] private float idleRadius = 90f;

    [Header("Defensive arc")]
    [Tooltip("Keep it well inside the sensor radius, so the arc sits between the station and the player.")]
    [SerializeField] private float alertRadius = 75f;
    [SerializeField, Range(10f, 360f)] private float arcAngle = 120f;
    [Tooltip("The arc only re-orients when the threat bearing is more than this many degrees off its current facing. " +
             "Sweeping it continuously moved the slots far faster than destroyers can fly, so they never arrived.")]
    [SerializeField, Range(5f, 90f)] private float refaceAngle = 30f;

    [Header("Advance")]
    [Tooltip("How far the formation may leave the station when advancing. While advancing, the station keeps " +
             "tracking the player up to this far beyond its sensor range.")]
    [SerializeField] private float maxAdvanceDistance = 150f;
    [Tooltip("Advancing stops when the front of the formation is this close to the player.")]
    [SerializeField] private float engageStandoff = 40f;

    [Header("Other stations")]
    [SerializeField] private bool respondToRemoteAlerts = true;
    [Tooltip("Only turn towards another station's alert if this station has been attacked before.")]
    [SerializeField] private bool onlyIfAlertedBefore = true;

    private readonly List<GameObject> units = new List<GameObject>();
    private readonly HashSet<FormationMember> assigned = new HashSet<FormationMember>();
    private readonly HashSet<FormationMember> stillAssigned = new HashSet<FormationMember>();
    private readonly Dictionary<GameObject, FormationMember> memberCache = new Dictionary<GameObject, FormationMember>();
    private readonly Dictionary<FormationMember, int> slotOf = new Dictionary<FormationMember, int>();
    private readonly List<FormationMember> members = new List<FormationMember>();
    private bool bAssignedAlert;
    private bool bForceReassign;
    private DistressBeacon beacon;

    private bool bAlert;
    private bool bAlertedBefore;
    private bool bSensorContact;
    private Transform contact;
    private Vector3 facing;
    private Vector3 desiredFacing;

    private EnemyDoctrine Doctrine => doctrine != null ? doctrine : EnemyDoctrine.Fallback;
    private float Threat01 => ThreatMeter.Instance != null ? ThreatMeter.Instance.Normalized : 0f;
    private Vector3 StationPosition => owner != null ? owner.transform.position : transform.position;

    public FormationStance CurrentStance { get; private set; }

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<UnitManager>();
        if (owner == null) owner = GetComponentInChildren<UnitManager>(true);
        if (sensor == null && owner != null) sensor = owner.GetComponentInChildren<AIDetector>();
        if (garrison == null) garrison = FindGarrison();
        beacon = GetComponent<DistressBeacon>();

        // Idle ring facing away from the map centre, so the station's first ship covers the outside.
        facing = Flat(StationPosition);
        facing = facing.sqrMagnitude > 0.01f ? facing.normalized : Vector3.forward;
        desiredFacing = facing;
    }

    private void OnEnable()
    {
        if (sensor != null)
        {
            sensor.TargetAcquired += OnContact;
            sensor.TargetLost += OnContactLost;
        }
        DistressBeacon.AnyAlert += OnAnyAlert;
        if (owner != null) owner.Died += OnOwnerDied;
    }

    private void OnDisable()
    {
        if (sensor != null)
        {
            sensor.TargetAcquired -= OnContact;
            sensor.TargetLost -= OnContactLost;
        }
        DistressBeacon.AnyAlert -= OnAnyAlert;
        if (owner != null) owner.Died -= OnOwnerDied;
    }

    private void Update()
    {
        if (garrison == null || (owner != null && owner.IsDead)) return;

        if (contact != null && !StillTracking()) contact = null;
        if (contact != null) SetDesiredFacing(contact.position - StationPosition);

        // Step to the new bearing (and hand out slots again) instead of sweeping round.
        if (Vector3.Angle(facing, desiredFacing) > refaceAngle)
        {
            facing = desiredFacing;
            bForceReassign = true;
        }

        garrison.GetDeployedUnits(units);
        AssignSlots();
    }

    private void AssignSlots()
    {
        CurrentStance = contact != null ? Doctrine.GetStance(Threat01) : FormationStance.Hold;

        Vector3 station = StationPosition;
        Vector3 centre = station;

        if (contact != null && CurrentStance != FormationStance.Hold)
        {
            // The whole formation moves out behind the leader at the front of the arc.
            float toContact = NavMeshUtility.FlatDistance(station, contact.position);
            float advance = Mathf.Clamp(toContact - alertRadius - engageStandoff, 0f, maxAdvanceDistance);
            centre += facing * advance;
        }

        members.Clear();
        foreach (GameObject unit in units)
        {
            FormationMember member = GetMember(unit);
            if (member != null && member.TryClaim(this)) members.Add(member);
        }

        List<Vector3> slots = GetSlots(centre, members.Count);
        if (NeedsReassignment()) AssignNearest(slots);

        int breakers = contact != null ? Doctrine.GetBreakCount(members.Count - 1, Threat01) : 0;

        stillAssigned.Clear();
        foreach (FormationMember member in members)
        {
            int index = slotOf[member];
            Vector3 slot = slots[index];

            member.SetSlot(slot, GetShipFacing(slot));
            // The outermost wingmen (last slots) are the ones that break off; slot 0 is the leader.
            member.SetBreaking(index > 0 && index >= members.Count - breakers, contact);

            stillAssigned.Add(member);
        }

        // Units that docked or died give their slot back.
        foreach (FormationMember member in assigned)
        {
            if (member != null && !stillAssigned.Contains(member)) member.Release(this);
        }
        assigned.Clear();
        assigned.UnionWith(stillAssigned);
    }

    private List<Vector3> GetSlots(Vector3 centre, int count)
    {
        return bAlert
            ? FormationUtility.GetArcPositions(centre, facing, alertRadius, arcAngle, count)
            : FormationUtility.GetArcPositions(centre, facing, idleRadius, 360f, count);
    }

    // Ring: face outwards. Arc: face the player, or where the threat was last seen.
    private Vector3 GetShipFacing(Vector3 slot)
    {
        if (!bAlert) return slot - StationPosition;
        return contact != null ? contact.position - slot : facing;
    }

    // Slots are handed out again only when the shape changes (ring <-> arc) or ships come and go,
    // otherwise ships would keep swapping places as they move.
    private bool NeedsReassignment()
    {
        if (bForceReassign || bAssignedAlert != bAlert || slotOf.Count != members.Count) return true;
        foreach (FormationMember member in members)
        {
            if (!slotOf.ContainsKey(member)) return true;
        }
        return false;
    }

    // Each slot (leader's first) goes to the closest ship still free, so ships move the shortest way
    // instead of crossing the station. Slow destroyers would otherwise take minutes to reach the arc.
    private void AssignNearest(List<Vector3> slots)
    {
        slotOf.Clear();
        bAssignedAlert = bAlert;
        bForceReassign = false;

        var free = new List<FormationMember>(members);
        for (int i = 0; i < slots.Count && free.Count > 0; i++)
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            for (int j = 0; j < free.Count; j++)
            {
                float distance = NavMeshUtility.FlatDistance(UnitSetup.GetBody(free[j].gameObject).position, slots[i]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = j;
                }
            }

            slotOf[free[best]] = i;
            free.RemoveAt(best);
        }
    }

    // IDeployPlacement: the garrison appears directly in its formation slots, facing the right way,
    // instead of in a launch line on one random side of the station.
    public bool TryGetDeployPose(DeploySpawner spawner, int rank, int count, out Vector3 position, out Quaternion rotation)
    {
        position = StationPosition;
        rotation = Quaternion.identity;
        if (spawner != garrison || count <= 0) return false;

        Vector3 slot = GetSlots(StationPosition, count)[Mathf.Clamp(rank, 0, count - 1)];
        UnitSetup.TryGetNavMeshPoint(slot, NavMeshUtility.DefaultSearchRadius, out position);

        Vector3 shipFacing = Flat(GetShipFacing(slot));
        rotation = shipFacing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(shipFacing.normalized, Vector3.up) : Quaternion.identity;
        return true;
    }

    // A holding formation lets the player go at the sensor edge; an advancing one keeps after them
    // until they're maxAdvanceDistance beyond it.
    private bool StillTracking()
    {
        if (!contact.gameObject.activeInHierarchy) return false;
        if (bSensorContact) return true;
        if (CurrentStance == FormationStance.Hold) return false;

        float sensorRadius = sensor != null ? sensor.ViewRadius : 0f;
        return NavMeshUtility.FlatDistance(StationPosition, contact.position) <= sensorRadius + maxAdvanceDistance;
    }

    private void OnContact(Transform target)
    {
        contact = target;
        bSensorContact = true;
        bAlert = true;
        bAlertedBefore = true;
        SetDesiredFacing(target.position - StationPosition);
    }

    // Once tracking ends the arc keeps facing where the player was last seen, and breakers fall back into formation.
    private void OnContactLost(Transform lastTarget)
    {
        bSensorContact = false;
    }

    private void OnAnyAlert(DistressBeacon alerted, Transform intruder)
    {
        if (!respondToRemoteAlerts || alerted == null || alerted == beacon) return;
        if (contact != null) return; // busy with our own fight
        if (onlyIfAlertedBefore && !bAlertedBefore) return;

        bAlert = true;
        SetDesiredFacing(alerted.Station.position - StationPosition);
    }

    private void OnOwnerDied(UnitManager deadOwner)
    {
        foreach (FormationMember member in assigned)
        {
            if (member != null) member.Release(this);
        }
        assigned.Clear();
    }

    private void SetDesiredFacing(Vector3 direction)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude > 0.01f) desiredFacing = direction.normalized;
    }

    private FormationMember GetMember(GameObject unit)
    {
        if (!memberCache.TryGetValue(unit, out FormationMember member))
        {
            member = unit.GetComponent<FormationMember>();
            memberCache[unit] = member;
        }
        return member;
    }

    // The garrison is the spawner whose units stay out (ReturnAndPatrol), not the fighters that dock.
    private DeploySpawner FindGarrison()
    {
        foreach (DeploySpawner spawner in GetComponents<DeploySpawner>())
        {
            if (spawner.Recall == RecallMode.ReturnAndPatrol) return spawner;
        }
        return null;
    }

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 station = Application.isPlaying ? StationPosition : transform.position;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(station, idleRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(station, station + (Application.isPlaying ? facing : transform.forward) * alertRadius);
    }
}
