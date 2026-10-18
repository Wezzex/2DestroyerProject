using UnityEngine;

public class AIPursuitBehaviour : AIBehavior
{
    [Header("Reference")]
    [SerializeField] private FollowPlannedPath followPlannedPath;
    [SerializeField] private GlobalPathPlaner planer;
    [SerializeField] private AIDetector detector;

    public override string Name => "Pursuit";

    private void Awake()
    {
        detector = GetComponentInChildren<AIDetector>();

        if (planer == null)
        {
            planer = GetComponentInChildren<GlobalPathPlaner>();
        }

        if (followPlannedPath == null)
        {
            followPlannedPath = GetComponentInChildren<FollowPlannedPath>();
        }
    }
    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {

        if (aIDetector == null || aIDetector.Target == null) return;

        followPlannedPath.ResetFollower();
        followPlannedPath.StopMoving = false;
        followPlannedPath.HoldFacing = null;
        Utility.LogAI("Pursuit Action is called", shipController);

        Vector3 targetPosition = aIDetector.Target.transform.position;

        planer.SetDestination(targetPosition);

    }
}
