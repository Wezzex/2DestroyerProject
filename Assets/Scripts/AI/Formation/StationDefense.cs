using System.Collections.Generic;
using UnityEngine;

// Commands a station's garrison (the units of a DeploySpawner) as a stationary formation instead of patrols.
//  * Idle: an even ring around the station, covering every angle, ships facing outwards.
//  * Alert (player in sensor range): a defensive arc on the player's side, facing the player.
//    The EnemyDoctrine and player threat decide whether the arc holds, advances behind its leader,
//    or lets some wingmen break formation to engage.
//  * Player gone: the arc stays, facing where the player was last seen, so the station's back is open.
//  * Another station raises the alarm: an idle, previously alerted station turns its arc towards that station.
public class StationDefense : MonoBehaviour
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
    [Tooltip("How fast (degrees/second) the formation swings around to a new threat direction.")]
    [SerializeField] private float facingTurnRate = 20f;

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

        facing = Vector3.RotateTowards(facing, desiredFacing, facingTurnRate * Mathf.Deg2Rad * Time.deltaTime, 0f);

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

        List<Vector3> slots = bAlert
            ? FormationUtility.GetArcPositions(centre, facing, alertRadius, arcAngle, units.Count)
            : FormationUtility.GetArcPositions(centre, facing, idleRadius, 360f, units.Count);

        int breakers = contact != null ? Doctrine.GetBreakCount(units.Count - 1, Threat01) : 0;

        stillAssigned.Clear();
        for (int i = 0; i < units.Count; i++)
        {
            FormationMember member = GetMember(units[i]);
            if (member == null || !member.TryClaim(this)) continue;

            Vector3 slot = slots[i];
            Vector3 shipFacing = !bAlert ? slot - station
                               : contact != null ? contact.position - slot
                               : facing;

            member.SetSlot(slot, shipFacing);
            // The outermost wingmen (last slots) are the ones that break off.
            member.SetBreaking(i > 0 && i >= units.Count - breakers, contact);

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
