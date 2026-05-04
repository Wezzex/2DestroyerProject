using UnityEngine;

public class AIHoldPositionBehaviour : AIBehavior
{

    [SerializeField] private FollowPlannedPath followPlannedPath;
    public override string Name => "HoldPosition";

    private void Awake()
    {

        if (followPlannedPath == null)
        {
            followPlannedPath = GetComponentInChildren<FollowPlannedPath>();
        }
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {

        Utility.LogAI("HoldPosition Action is called", shipController);

        followPlannedPath.StopMoving = true;
    }
}
