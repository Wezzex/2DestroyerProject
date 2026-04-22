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
    }
    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        Vector3 targetPosition = aIDetector.Target.transform.position;

        planer.SetDestination(targetPosition);

    }
}
