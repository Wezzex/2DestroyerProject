using System.Collections;
using UnityEngine;

public enum SquadOrder
{
    None,
    Patrol,
    Engage,
    ReturnToOwner
}

// Lives on every deployable unit. A unit only takes orders from the DeploySpawner that owns it.
// When the owner dies the unit becomes "orphaned": it keeps its last order and slowly loses health.
public class SquadMember : MonoBehaviour
{
    [Header("Orphaned")]
    [SerializeField] private int orphanDamagePerTick = 5;
    [SerializeField] private float orphanTickInterval = 1f;

    private UnitManager unit;
    private Vector3 lastDockPosition;

    public DeploySpawner Owner { get; private set; }
    public int SquadIndex { get; private set; }
    public SquadOrder CurrentOrder { get; private set; } = SquadOrder.None;
    public Transform OrderTarget { get; private set; }
    public bool IsOrphaned { get; private set; }
    public UnitManager Unit => unit;

    // Read by the behaviour tree (EnemyAIRunner finds bool properties by name via reflection).
    public bool OrderedToReturn => CurrentOrder == SquadOrder.ReturnToOwner;
    public bool OrderedToEngage => CurrentOrder == SquadOrder.Engage;
    public bool OrderedToPatrol => CurrentOrder == SquadOrder.Patrol;

    public Vector3 DockPosition => Owner != null ? Owner.DockPosition : lastDockPosition;

    private void Awake()
    {
        if (unit == null) unit = GetComponentInChildren<UnitManager>(true);
    }

    private void OnEnable()
    {
        // Coroutines stop while docked/disabled; resume draining if we were orphaned.
        if (IsOrphaned) StartCoroutine(OrphanDrainCoroutine());
    }

    public void Bind(DeploySpawner owner, int squadIndex)
    {
        // Awake hasn't run yet if this component was added to an inactive object.
        if (unit == null) unit = GetComponentInChildren<UnitManager>(true);

        Owner = owner;
        SquadIndex = squadIndex;
        IsOrphaned = false;
        lastDockPosition = owner.DockPosition;
    }

    public bool ReceiveOrder(SquadOrder order, DeploySpawner issuer, Transform target = null)
    {
        if (IsOrphaned || issuer == null || issuer != Owner)
        {
            return false;
        }

        CurrentOrder = order;
        OrderTarget = target;
        lastDockPosition = issuer.DockPosition;

        AIDetector detector = GetComponentInChildren<AIDetector>(true);
        if (detector != null)
        {
            if (order == SquadOrder.Engage && target != null) detector.HoldTarget(target);
            else detector.ReleaseTarget();
        }

        return true;
    }

    // Called by AIReturnToOwnerBehaviour when the unit reaches its dock.
    public void NotifyDocked()
    {
        if (Owner != null)
        {
            Owner.OnMemberDocked(this);
        }
    }

    // Called by the owner as it dies. The unit finishes its last order while its hull fails.
    public void Orphan()
    {
        if (IsOrphaned) return;

        IsOrphaned = true;
        Owner = null;

        if (isActiveAndEnabled)
        {
            StartCoroutine(OrphanDrainCoroutine());
        }
    }

    private IEnumerator OrphanDrainCoroutine()
    {
        var wait = new WaitForSeconds(orphanTickInterval);

        while (unit != null && !unit.IsDead)
        {
            yield return wait;
            unit.TakeDamage(orphanDamagePerTick);
        }
    }
}
