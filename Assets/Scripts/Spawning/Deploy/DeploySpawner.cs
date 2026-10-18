using System.Collections.Generic;
using UnityEngine;

public enum DeployTrigger
{
    OnGameStart,      // e.g. destroyer garrison around a station
    OnSensorContact   // e.g. fighters launching when the player enters the station's sensors
}

public enum RecallMode
{
    DockAndStore,     // return to the deploy point, disappear, and remember their health
    ReturnAndPatrol   // stay out and keep patrolling around the owner
}

public enum ReplenishMode
{
    ResetWhenIdle,    // repair + replace dead only after every unit is docked or destroyed
    ContinuousRespawn // each dead unit is replaced after respawnTime, always running
}

// Drop on any ship or station that owns a group of units (station fighters, destroyer garrison,
// later carriers). The component owns a fixed number of slots, so it can never exceed its maximum.
public class DeploySpawner : MonoBehaviour
{
    private enum SlotState
    {
        Empty,
        Docked,
        Deployed,
        Dead
    }

    private class Slot
    {
        public int squadIndex;
        public GameObject unit;
        public SquadMember member;
        public UnitManager health;
        public SlotState state;
        public float respawnAt;
    }

    [Header("Owner")]
    [Tooltip("The station/ship that owns these units. Defaults to the closest UnitManager in parents.")]
    [SerializeField] private UnitManager owner;
    [Tooltip("The owner's sensor. Defaults to the AIDetector under the owner.")]
    [SerializeField] private AIDetector sensor;
    [Tooltip("Hangar / launch point. Units deploy from and dock to here. Defaults to this transform.")]
    [SerializeField] private Transform deployPoint;

    [Header("Squads")]
    [SerializeField] private GameObject[] unitPrefabs;
    [SerializeField, Min(1)] private int maxSquads = 1;
    [SerializeField, Min(1)] private int unitsPerSquad = 4;
    [SerializeField] private FormationShape formation = FormationShape.Wedge;
    [SerializeField] private float formationSpacing = 6f;
    [Tooltip("How far from the deploy point a squad appears.")]
    [SerializeField] private float launchDistanceMin = 10f;
    [SerializeField] private float launchDistanceMax = 20f;
    [Tooltip("Random spread (degrees) around the direction to the target when launching.")]
    [SerializeField, Range(0f, 180f)] private float launchSpreadAngle = 45f;

    [Header("Behaviour")]
    [SerializeField] private DeployTrigger trigger = DeployTrigger.OnSensorContact;
    [SerializeField] private RecallMode recallMode = RecallMode.DockAndStore;
    [SerializeField] private ReplenishMode replenishMode = ReplenishMode.ResetWhenIdle;
    [Tooltip("ResetWhenIdle: seconds after all units are docked/destroyed before they are repaired and refilled.")]
    [SerializeField] private float resetTime = 30f;
    [Tooltip("ContinuousRespawn: seconds after a unit dies before it is replaced.")]
    [SerializeField] private float respawnTime = 45f;

    private readonly List<Slot> slots = new List<Slot>();
    private Transform unitContainer;
    private float resetAt = -1f;
    private bool bOwnerDead;

    public UnitManager Owner => owner;
    // Owner's body (Motor) moves with the ship, so docking follows a moving carrier too.
    public Vector3 DockPosition => deployPoint != null ? deployPoint.position
                                 : owner != null ? owner.transform.position
                                 : transform.position;
    public bool HasContact => sensor != null && sensor.Target != null;
    public bool IsResetRunning => resetAt >= 0f;
    public float ResetTimeRemaining => IsResetRunning ? Mathf.Max(0f, resetAt - Time.time) : 0f;
    public int MaxUnits => maxSquads * unitsPerSquad;
    public int DeployedCount => CountSlots(SlotState.Deployed);
    public int DockedCount => CountSlots(SlotState.Docked);
    public RecallMode Recall => recallMode;
    public FormationShape Formation => formation;
    public float FormationSpacing => formationSpacing;

    // Every deployed, living unit in slot order (all squads). Used by StationDefense to hand out formation slots.
    public void GetDeployedUnits(List<GameObject> result)
    {
        result.Clear();
        foreach (Slot slot in slots)
        {
            if (slot.state == SlotState.Deployed && slot.unit != null && slot.unit.activeInHierarchy) result.Add(slot.unit);
        }
    }

    // Deployed, living members of one squad in slot order. Index 0 is the formation leader.
    public void GetDeployedSquad(int squadIndex, List<SquadMember> result)
    {
        result.Clear();
        foreach (Slot slot in slots)
        {
            if (slot.squadIndex != squadIndex || slot.state != SlotState.Deployed) continue;
            if (slot.member == null || slot.unit == null || !slot.unit.activeInHierarchy) continue;

            result.Add(slot.member);
        }
    }

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<UnitManager>();
        if (owner == null) owner = GetComponentInChildren<UnitManager>(true);
        if (sensor == null && owner != null) sensor = owner.GetComponentInChildren<AIDetector>();

        // Deployed units must NOT be children of the owner: when the owner dies it is disabled,
        // which would disable every unit under it instead of letting them become orphans.
        unitContainer = UnitSetup.GetOrCreateContainer("DeployedUnits");

        for (int squad = 0; squad < maxSquads; squad++)
        {
            for (int i = 0; i < unitsPerSquad; i++)
            {
                slots.Add(new Slot { squadIndex = squad, state = SlotState.Empty });
            }
        }
    }

    private void OnEnable()
    {
        if (sensor != null)
        {
            sensor.TargetAcquired += OnContact;
            sensor.TargetLost += OnContactLost;
        }
        if (owner != null) owner.Died += OnOwnerDied;
        if (owner != null && owner.IsDead) bOwnerDead = true;
    }

    private void OnDisable()
    {
        if (sensor != null)
        {
            sensor.TargetAcquired -= OnContact;
            sensor.TargetLost -= OnContactLost;
        }
        if (owner != null) owner.Died -= OnOwnerDied;
    }

    private void Start()
    {
        if (unitPrefabs == null || unitPrefabs.Length == 0)
        {
            Debug.LogWarning($"[DeploySpawner] {name} has no unit prefabs.", this);
            enabled = false;
            return;
        }

        foreach (Slot slot in slots)
        {
            CreateUnit(slot);
        }

        if (trigger == DeployTrigger.OnGameStart)
        {
            DeployDocked(sensor != null ? sensor.Target : null);
        }
        else if (HasContact)
        {
            OnContact(sensor.Target);
        }
    }

    private void Update()
    {
        if (bOwnerDead) return;

        bool replenished = false;

        if (replenishMode == ReplenishMode.ResetWhenIdle && IsResetRunning && Time.time >= resetAt)
        {
            resetAt = -1f;
            foreach (Slot slot in slots)
            {
                if (slot.state == SlotState.Dead || slot.state == SlotState.Empty) ReplaceUnit(slot);
                else if (slot.state == SlotState.Docked) slot.health?.Repair();
            }
            replenished = true;
        }

        if (replenishMode == ReplenishMode.ContinuousRespawn)
        {
            foreach (Slot slot in slots)
            {
                if (slot.state == SlotState.Dead && Time.time >= slot.respawnAt)
                {
                    ReplaceUnit(slot);
                    replenished = true;
                }
            }
        }

        if (replenished && ShouldBeDeployed())
        {
            DeployDocked(sensor != null ? sensor.Target : null);
        }
    }

    // ---------------- Sensor / owner events ----------------

    private void OnContact(Transform target)
    {
        if (bOwnerDead) return;

        // Player came back before the reset finished: redeploy the survivors as they are.
        CancelReset();
        DeployDocked(target);
        IssueOrderToDeployed(SquadOrder.Engage, target);
    }

    private void OnContactLost(Transform lastTarget)
    {
        if (bOwnerDead) return;

        SquadOrder order = recallMode == RecallMode.DockAndStore ? SquadOrder.ReturnToOwner : SquadOrder.Patrol;
        IssueOrderToDeployed(order, null);
    }

    private void OnOwnerDied(UnitManager deadOwner)
    {
        bOwnerDead = true;
        CancelReset();

        foreach (Slot slot in slots)
        {
            if (slot.health != null) slot.health.Died -= OnUnitDied;

            if (slot.state == SlotState.Deployed)
            {
                slot.member.Orphan();
            }
            else if (slot.state == SlotState.Docked && slot.unit != null)
            {
                // Docked units go down with the hangar.
                Destroy(slot.unit);
            }
        }
    }

    private void OnUnitDied(UnitManager deadUnit)
    {
        Slot slot = FindSlot(deadUnit);
        if (slot == null) return;

        deadUnit.Died -= OnUnitDied;
        slot.state = SlotState.Dead;
        slot.respawnAt = Time.time + respawnTime;

        TryStartReset();
    }

    // Called by SquadMember when it reaches the dock.
    public void OnMemberDocked(SquadMember member)
    {
        Slot slot = FindSlot(member);
        if (slot == null || slot.state != SlotState.Deployed) return;

        slot.unit.SetActive(false);
        UnitSetup.Teleport(slot.unit, DockPosition, Quaternion.identity);
        slot.state = SlotState.Docked;

        TryStartReset();
    }

    // ---------------- Deploying ----------------

    private void DeployDocked(Transform target)
    {
        // Launch each squad as its own formation.
        for (int squad = 0; squad < maxSquads; squad++)
        {
            List<Slot> docked = slots.FindAll(s => s.squadIndex == squad && s.state == SlotState.Docked);
            if (docked.Count == 0) continue;

            // Launch from the owner's centre, past the hangar's distance from the hull, towards the target.
            // This keeps squads outside the owner's hull/NavMesh hole whichever side the player is on.
            Vector3 origin = LaunchOrigin;
            Vector3 launchDirection = GetLaunchDirection(target);
            float clearance = FlatDistance(origin, DockPosition);
            Vector3 anchor = origin + launchDirection * (clearance + Random.Range(launchDistanceMin, launchDistanceMax));
            Vector3 facing = target != null ? target.position - anchor : launchDirection;

            List<Vector3> positions = FormationUtility.GetWorldPositions(formation, docked.Count, formationSpacing, anchor, facing);
            Quaternion rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.z).normalized, Vector3.up);

            for (int i = 0; i < docked.Count; i++)
            {
                UnitSetup.TryGetNavMeshPoint(positions[i], formationSpacing * 2f, out Vector3 position);
                Activate(docked[i], position, rotation, target);
            }
        }
    }

    private void Activate(Slot slot, Vector3 position, Quaternion rotation, Transform target)
    {
        slot.unit.transform.SetPositionAndRotation(position, rotation);
        slot.unit.SetActive(true);
        UnitSetup.Teleport(slot.unit, position, rotation);

        slot.state = SlotState.Deployed;
        slot.member.ReceiveOrder(target != null ? SquadOrder.Engage : SquadOrder.Patrol, this, target);
    }

    private Vector3 GetLaunchDirection(Transform target)
    {
        Vector3 direction;
        if (target != null)
        {
            direction = target.position - LaunchOrigin;
            direction.y = 0f;
            direction = Quaternion.Euler(0f, Random.Range(-launchSpreadAngle, launchSpreadAngle), 0f) * direction.normalized;
        }
        else
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    private void IssueOrderToDeployed(SquadOrder order, Transform target)
    {
        foreach (Slot slot in slots)
        {
            if (slot.state == SlotState.Deployed)
            {
                slot.member.ReceiveOrder(order, this, target);
            }
        }
    }

    private bool ShouldBeDeployed()
    {
        return HasContact || (trigger == DeployTrigger.OnGameStart && recallMode == RecallMode.ReturnAndPatrol);
    }

    // ---------------- Creating / replenishing ----------------

    private void CreateUnit(Slot slot)
    {
        GameObject prefab = unitPrefabs[Random.Range(0, unitPrefabs.Length)];
        GameObject unit = Instantiate(prefab, DockPosition, Quaternion.identity, unitContainer);
        unit.name = $"{(owner != null ? owner.name : name)}_Squad{slot.squadIndex}_{prefab.name}";
        unit.SetActive(false);

        SquadMember member = unit.GetComponent<SquadMember>();
        if (member == null) member = unit.AddComponent<SquadMember>();
        member.Bind(this, slot.squadIndex);

        UnitSetup.AssignHome(unit, owner != null ? owner.transform : transform);

        slot.unit = unit;
        slot.member = member;
        slot.health = unit.GetComponentInChildren<UnitManager>(true);
        slot.state = SlotState.Docked;

        if (slot.health != null)
        {
            // UnitManager only fills health in Start (first activation); fill it now so a fresh
            // docked unit doesn't count as damaged.
            slot.health.Repair();
            slot.health.Died += OnUnitDied;
        }
    }

    private void ReplaceUnit(Slot slot)
    {
        if (slot.health != null) slot.health.Died -= OnUnitDied;
        if (slot.unit != null) Destroy(slot.unit);

        CreateUnit(slot);
    }

    private void TryStartReset()
    {
        if (replenishMode != ReplenishMode.ResetWhenIdle) return;
        if (IsResetRunning) return;
        if (CountSlots(SlotState.Deployed) > 0) return; // only once everyone is home or gone
        if (!NeedsReplenish()) return;

        resetAt = Time.time + resetTime;
    }

    private void CancelReset()
    {
        resetAt = -1f;
    }

    private bool NeedsReplenish()
    {
        foreach (Slot slot in slots)
        {
            if (slot.state == SlotState.Dead || slot.state == SlotState.Empty) return true;
            if (slot.state == SlotState.Docked && slot.health != null && slot.health.Health < slot.health.MaxHealth) return true;
        }
        return false;
    }

    // ---------------- Helpers ----------------

    private Vector3 LaunchOrigin => owner != null ? owner.transform.position : DockPosition;

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Slot FindSlot(UnitManager unit) => slots.Find(s => s.health == unit);
    private Slot FindSlot(SquadMember member) => slots.Find(s => s.member == member);

    private int CountSlots(SlotState state)
    {
        int count = 0;
        foreach (Slot slot in slots)
        {
            if (slot.state == state) count++;
        }
        return count;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 dock = DockPosition;
        Vector3 origin = LaunchOrigin;
        float clearance = FlatDistance(origin, dock);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(dock, 2f);
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(origin, clearance + launchDistanceMin);
        Gizmos.DrawWireSphere(origin, clearance + launchDistanceMax);
    }
}
