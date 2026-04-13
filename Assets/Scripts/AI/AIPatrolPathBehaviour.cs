using System.Collections;
using UnityEngine;

public class AIPatrolPathBehaviour : AIBehavior
{
    [SerializeField] private PatrolArea patrolArea;
    [SerializeField] private FollowPlannedPath followPlannedPath;
    [SerializeField] private GlobalPathPlaner planer;
    [SerializeField, Range(0.1f, 1f)] private float arriveDistance = 1;

    [SerializeField] private float waitTime = 0.5f;
    [SerializeField] private bool isWaiting = false;

    [SerializeField] private Vector3 currentPatrolTarget;
    private Vector3 shipCurrentPosition;

    private void Start()
    {
        patrolArea = GetComponentInChildren<PatrolArea>();

        currentPatrolTarget = patrolArea.GetCurrentTargetPosition();

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
        if (patrolArea == null) return; 
        if (isWaiting) return;

        Vector3 goal = patrolArea.GetCurrentTargetPosition();
        planer.SetDestination(goal);

        Vector3 shipPosition = shipController.transform.position;
        shipPosition.y = 0f;

        goal.y = 0f;

        if (Vector3.Distance(shipPosition, goal) <= arriveDistance)
        {
            isWaiting = true;
            StartCoroutine(WaitAndSwapPoint());
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
        isWaiting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(shipCurrentPosition, currentPatrolTarget);
    }
}