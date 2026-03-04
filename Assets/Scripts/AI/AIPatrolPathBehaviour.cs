using System.Collections;
using UnityEngine;

public class AIPatrolPathBehaviour : AIBehavior
{
    [SerializeField] private PatrolPath patrolPath;
    [SerializeField, Range(0.1f, 1f)] private float arriveDistance = 1;

    [SerializeField] private float waitTime = 0.5f;
    [SerializeField] private bool isWaiting = false;

    bool isInitialized = false;
    private int currentIndex = -1;

    [SerializeField] private Vector3 currentPatrolTarget;

    private void Awake()
    {
        if (patrolPath == null)
        {
            patrolPath = GetComponentInChildren<PatrolPath>();
        }
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        if (patrolPath == null) return;
        if (patrolPath.Length < 2) return;
        if (isWaiting) return;

        if (!isInitialized)
        {
            var currentPathPoint = patrolPath.GetClosestPathPoint(shipController.transform.position);
            currentIndex = currentPathPoint.Index;
            currentPatrolTarget = currentPathPoint.Position;
            isInitialized = true;
        }

        Vector3 shipPosition = shipController.transform.position;
        Vector3 targetPosition = currentPatrolTarget;
        shipPosition.y = 0f;
        targetPosition.y = 0f;

        if (Vector3.Distance(shipPosition, targetPosition) <= arriveDistance)
        {
            isWaiting = true;
            StartCoroutine(WaitCoroutine());
            shipController.HandleMoveShip(Vector2.zero);
            return;
        }

        Vector3 toTarget = (currentPatrolTarget - shipController.transform.position);
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            shipController.HandleMoveShip(Vector2.zero);
            return;
        }
        toTarget.Normalize();

        Vector3 forwardDirection = shipController.transform.forward;
        forwardDirection.y = 0f;
        forwardDirection.Normalize();

        float angle = Vector3.SignedAngle(forwardDirection, toTarget, Vector3.up);

        float turn = Mathf.Clamp(angle / 45f, -1f, 1f);
        float thrust = Mathf.Abs(angle) < 15f ? 1f : 0.5f;

        shipController.HandleMoveShip(new Vector2(turn, thrust));
        shipController.SetShootingState(false);

    }


    IEnumerator WaitCoroutine()
    {
        yield return new WaitForSeconds(waitTime);
        var nextPathPoint = patrolPath.GetNextPathPoint(currentIndex);
        currentPatrolTarget = nextPathPoint.Position;
        currentIndex = nextPathPoint.Index;
        isWaiting = false;
    }
}

