using UnityEngine;

public class DefaultEnemyAI : MonoBehaviour
{

    [SerializeField] private AIBehavior shootBehaviour, patrolBehaviour;

    [SerializeField] private ShipController shipController;
    [SerializeField] private AIDetector aIDetector;

    private void Awake()
    {
        aIDetector = GetComponentInChildren<AIDetector>();
        shipController = GetComponentInChildren<ShipController>();
    }

    private void Update()
    {
        if (aIDetector.TargetVisible)
        {
            shootBehaviour.PerformAction(shipController, aIDetector);
        }
        else
        {
            patrolBehaviour.PerformAction(shipController, aIDetector);
        }
    }
}
