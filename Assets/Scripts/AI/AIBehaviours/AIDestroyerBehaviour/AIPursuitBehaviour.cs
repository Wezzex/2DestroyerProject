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

        followPlannedPath.ResetFollower();
        followPlannedPath.StopMoving = false;
        Utility.LogAI("Pursuit Action is called", shipController);

        Vector3 targetPosition = aIDetector.Target.transform.position;

        planer.SetDestination(targetPosition);

    }
}
