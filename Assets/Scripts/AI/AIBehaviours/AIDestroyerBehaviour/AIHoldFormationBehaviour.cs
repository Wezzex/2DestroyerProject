using UnityEngine;

// Flies to the formation slot handed out by FormationMember, then holds there facing the formation's direction.
// Shooting is left to the "Shoot" action that follows it in the behaviour tree.
public class AIHoldFormationBehaviour : AIBehavior
{
    [SerializeField] private FormationMember member;
    [SerializeField] private FollowPlannedPath followPlannedPath;
    [SerializeField] private GlobalPathPlaner planer;
    [SerializeField] private Transform shipCPosition;

    [Tooltip("Counts as in the slot within this distance. Keep it above the ship's turning radius or it circles the slot.")]
    [SerializeField] private float arriveDistance = 12f;
    [Tooltip("Starts moving again once the slot is further away than this.")]
    [SerializeField] private float resumeDistance = 25f;

    private bool bHolding;

    public override string Name => "HoldFormation";

    private void Start()
    {
        if (member == null) member = GetComponent<FormationMember>();
        if (planer == null) planer = GetComponentInChildren<GlobalPathPlaner>();
        if (followPlannedPath == null) followPlannedPath = GetComponentInChildren<FollowPlannedPath>();
        if (shipCPosition == null) shipCPosition = UnitSetup.GetBody(gameObject);
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        Utility.LogAI("HoldFormation is called", shipController);
        if (member == null || planer == null || followPlannedPath == null) return;

        Vector3 slot = planer.SnapToNavMesh(member.SlotPosition);
        float distance = NavMeshUtility.FlatDistance(shipCPosition.position, slot);

        // Hysteresis, so a ship nudged off its slot doesn't keep starting and stopping.
        if (bHolding && distance > resumeDistance) bHolding = false;
        else if (!bHolding && distance <= arriveDistance) bHolding = true;

        if (bHolding)
        {
            followPlannedPath.StopMoving = true;
            followPlannedPath.HoldFacing = member.Facing;
            return;
        }

        followPlannedPath.HoldFacing = null;
        followPlannedPath.StopMoving = false;
        planer.SetDestination(slot);
    }

    private void OnDisable()
    {
        bHolding = false;
        if (followPlannedPath != null) followPlannedPath.HoldFacing = null;
    }
}
