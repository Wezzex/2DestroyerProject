using System.Collections;
using UnityEngine;

public class AIFighterPatrolBehaviour : AIBehavior
{
    [SerializeField] private PatrolArea patrolArea;
    [SerializeField] private FollowPlannedPath followPlannedPath;
    [SerializeField] private GlobalPathPlaner planer;
    [Tooltip("Optional. Wingmen follow their squad leader's patrol instead of their own points.")]
    [SerializeField] private SquadFlocking flocking;
    [SerializeField, Range(0.1f, 10f)] private float arriveDistance = 5;

    [SerializeField] private float waitTime = 0.5f;
    [SerializeField] private bool isWaiting = false;

    [SerializeField] private Vector3 currentPatrolTarget;
    [SerializeField] private Vector3 shipCurrentPosition;
    [SerializeField] private Transform shipCPosition;

    public override string Name => "FighterPatrol";

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

        if (flocking == null)
        {
            flocking = GetComponentInChildren<SquadFlocking>();
        }
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        Utility.LogAI("FighterPatrol Action is called", shipController);
        if (patrolArea == null) return;
        if (isWaiting) return;


        followPlannedPath.StopMoving = false;
        shipController.SetShootingState(false);

        Vector3 shipPosition = shipCPosition.position;
        shipPosition.y = 0f;

        // Wingmen fly the leader's route; their own points are used again if they become leader.
        if (flocking != null && flocking.TryGetFormationTarget(out Vector3 formationTarget))
        {
            currentPatrolTarget = planer.SnapToNavMesh(formationTarget);
            planer.SetDestination(currentPatrolTarget);
            return;
        }

        if (!patrolArea.bIsInitilized)
        {
            Debug.LogError("PatrolArea has not Initilized yet");
            return;
        }
        currentPatrolTarget = planer.SnapToNavMesh(patrolArea.GetCurrentTargetPosition());
        planer.SetDestination(currentPatrolTarget);

        currentPatrolTarget.y = 0f;

        if (Vector3.Distance(shipPosition, currentPatrolTarget) <= arriveDistance)
        {
            isWaiting = true;
            StartCoroutine(WaitAndSwapPoint());
        }
    }


    IEnumerator WaitAndSwapPoint()
    {
        yield return new WaitForSeconds(waitTime);

        patrolArea.OnReachedPoint();

        Vector3 newGoal = planer.SnapToNavMesh(patrolArea.GetCurrentTargetPosition());
        planer.SetDestination(newGoal);
        followPlannedPath.ResetFollower();
        currentPatrolTarget = newGoal;
        isWaiting = false;
    }

    // Coroutines die when the unit is disabled (docked), so make sure we don't stay stuck waiting.
    private void OnDisable()
    {
        isWaiting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(shipCurrentPosition, currentPatrolTarget);
    }
}
