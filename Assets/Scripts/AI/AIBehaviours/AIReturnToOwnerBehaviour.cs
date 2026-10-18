using UnityEngine;

// Flies the unit back to its owner's deploy point and docks it there.
// Used by any deployable unit (fighters, garrison destroyers) through the "ReturnToOwner" action.
public class AIReturnToOwnerBehaviour : AIBehavior
{
    [SerializeField] private SquadMember squadMember;
    [SerializeField] private FollowPlannedPath followPlannedPath;
    [SerializeField] private GlobalPathPlaner planer;
    [SerializeField] private Transform shipCPosition;

    [SerializeField, Range(0.5f, 30f)] private float dockDistance = 8f;

    public override string Name => "ReturnToOwner";

    private void Start()
    {
        if (squadMember == null) squadMember = GetComponent<SquadMember>();
        if (planer == null) planer = GetComponentInChildren<GlobalPathPlaner>();
        if (followPlannedPath == null) followPlannedPath = GetComponentInChildren<FollowPlannedPath>();
        // The Motor child is what moves; the root stays where the unit spawned.
        if (shipCPosition == null) shipCPosition = UnitSetup.GetBody(gameObject);
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        Utility.LogAI("ReturnToOwner is called", shipController);
        if (squadMember == null || planer == null || followPlannedPath == null) return;

        shipController.SetShootingState(false);
        followPlannedPath.StopMoving = false;

        Vector3 shipPosition = shipCPosition.position;
        shipPosition.y = 0f;

        // A dock point inside the owner's hull can't be reached; dock at the closest walkable point instead.
        Vector3 dock = planer.SnapToNavMesh(squadMember.DockPosition);
        dock.y = 0f;

        if (Vector3.Distance(shipPosition, dock) <= dockDistance)
        {
            // Orphans have no hangar to land in; they just hold at the last known dock position.
            if (squadMember.IsOrphaned)
            {
                followPlannedPath.StopMoving = true;
                return;
            }

            squadMember.NotifyDocked();
            return;
        }

        planer.SetDestination(dock);
    }
}
