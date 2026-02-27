using UnityEngine;

public class AIPatrolStaticBehaviour : AIBehavior
{
    [SerializeField] private float patrolDelay = 4;

    [SerializeField] private Vector2 randomDirection = Vector2.zero;
    [SerializeField] private float currentPatrolDelay;

    private void Awake()
    {
        randomDirection = Random.insideUnitCircle;
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        float angle = Vector2.Angle(shipController.aimTurrets[0].transform.right, randomDirection);
        if (currentPatrolDelay <= 0 && (angle < 2))
        {
            randomDirection = Random.insideUnitCircle;
            currentPatrolDelay = patrolDelay;
        }
        else
        {
            if (currentPatrolDelay > 0)
            {
                currentPatrolDelay -= Time.deltaTime;
            }
            else
            {
                shipController.HandleTurretMovement((Vector2)shipController.aimTurrets[0].transform.position + randomDirection);
                shipController.SetShootingState(false);
            }
        }
    }
}
