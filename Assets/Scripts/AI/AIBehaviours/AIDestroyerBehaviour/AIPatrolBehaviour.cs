using System.Collections;
using UnityEngine;

public class AIPatrolBehaviour : AIBehavior
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
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        Utility.LogAI("Patrol Action is called", shipController);
        if (patrolArea == null) return;
        if (isWaiting) return;


        followPlannedPath.StopMoving = false;

        Vector3 shipPosition = shipCPosition.position;
        shipPosition.y = 0f;

        if (!patrolArea.bIsInitilized)
        {
            Debug.LogError("PatrolArea has not Initilized yet");
            return;
        }
        currentPatrolTarget = patrolArea.GetCurrentTargetPosition();
        planer.SetDestination(currentPatrolTarget);

        currentPatrolTarget.y = 0f;

        if (Vector3.Distance(shipPosition, currentPatrolTarget) <= arriveDistance)
        {
            isWaiting = true;
            StartCoroutine(WaitAndSwapPoint());
        }

        shipController.SetShootingState(false);
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