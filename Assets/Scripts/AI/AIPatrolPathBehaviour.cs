using System.Collections;
using UnityEngine;

public class AIPatrolPathBehaviour : AIBehavior
{
    [SerializeField] private PatrolArea patrolArea;
    [SerializeField, Range(0.1f, 1f)] private float arriveDistance = 1;

    [SerializeField] private float waitTime = 0.5f;
    [SerializeField] private bool isWaiting = false;

    [SerializeField] private Vector3 currentPatrolTarget;
    private Vector3 shipCurrentPosition;

    private void Start()
    {
        patrolArea = GetComponentInChildren<PatrolArea>();

        currentPatrolTarget = patrolArea.GetCurrentTargetPosition();
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        if (patrolArea == null) return; 
        if (isWaiting) return;

        Vector3 shipPosition = shipController.transform.position;
        shipCurrentPosition = shipPosition;
        Vector3 patrolTargetPosition = patrolArea.GetCurrentTargetPosition(); 

        currentPatrolTarget = patrolTargetPosition;

        shipPosition.y = 0f;
        patrolTargetPosition.y = 0f;

        if (Vector3.Distance(shipPosition, patrolTargetPosition) <= arriveDistance)
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
        patrolArea.OnReachedPoint();
        var nextPathPoint = patrolArea.GetCurrentTargetPosition();
        currentPatrolTarget = nextPathPoint;
        isWaiting = false;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(shipCurrentPosition, currentPatrolTarget);
    }
}