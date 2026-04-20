using System.Collections;
using UnityEngine;

public class AIPatrolPathBehaviour : AIBehavior
{
    [SerializeField] private PatrolArea patrolArea;
    [SerializeField] private FollowPlannedPath followPlannedPath;
    [SerializeField] private GlobalPathPlaner planer;
    [SerializeField, Range(0.1f, 10f)] private float arriveDistance = 5;

    [SerializeField] private float waitTime = 0.5f;
    [SerializeField] private bool isWaiting = false;

    [SerializeField] private Vector3 currentPatrolTarget;
    [SerializeField] private Vector3 shipCurrentPosition;
    [SerializeField] private Transform shipCPosition;

    public override string Name => "Patrol";

    private void Start()
    {
        patrolArea = GetComponentInChildren<PatrolArea>();

        if (planer == null)
        {
            planer = GetComponentInChildren<GlobalPathPlaner>();
        }

        if (followPlannedPath == null)
        {
            followPlannedPath = GetComponentInChildren<FollowPlannedPath>();
        }

        if (!patrolArea.bIsInitilized) return;
        currentPatrolTarget = patrolArea.GetCurrentTargetPosition();
        planer.SetDestination(currentPatrolTarget);
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        if (patrolArea == null) return;
        if (isWaiting) return;

        Vector3 shipPosition = shipCPosition.transform.position;
        shipPosition.y = 0f;

        currentPatrolTarget.y = 0f;

        if (Vector3.Distance(shipPosition, currentPatrolTarget) <= arriveDistance)
        {
            isWaiting = true;
            StartCoroutine(WaitAndSwapPoint());
            Debug.LogWarning("Arrived");
        }

        shipController.SetShootingState(false);

    }


    IEnumerator WaitCoroutine()
    {
        yield return new WaitForSeconds(waitTime);
        patrolArea.OnReachedPoint();
        followPlannedPath.ResetFollower();
        var nextPathPoint = patrolArea.GetCurrentTargetPosition();
        currentPatrolTarget = nextPathPoint;
        isWaiting = false;
    }


    IEnumerator WaitAndSwapPoint()
    {
        yield return new WaitForSeconds(waitTime);

        patrolArea.OnReachedPoint();

        Vector3 newGoal = patrolArea.GetCurrentTargetPosition();
        planer.SetDestination(newGoal);
        followPlannedPath.ResetFollower();
        currentPatrolTarget = newGoal;
        isWaiting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(shipCurrentPosition, currentPatrolTarget);
    }
}