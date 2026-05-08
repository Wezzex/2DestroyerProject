using System.Collections;
using UnityEngine;

public class AIFighterPursuitBehaviour : AIBehavior
{

    [SerializeField] private FighterStrafePoints strafe;
    [SerializeField] private FollowPlannedPath followPlannedPath;
    [SerializeField] private GlobalPathPlaner planer;

    [SerializeField, Range(0.1f, 10f)] private float arriveDistance = 5;
    [SerializeField] private Transform shipCPosition;

    public override string Name => "FighterPursuit";

    private void Start()
    {
        if (strafe == null) strafe = GetComponentInChildren<FighterStrafePoints>();
        if (planer == null) planer = GetComponentInChildren<GlobalPathPlaner>();
        if (followPlannedPath == null) followPlannedPath = GetComponentInChildren<FollowPlannedPath>();

        if (shipCPosition == null)
        {
            shipCPosition = transform;
        }
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        Utility.LogAI("FighterPursuit is called", shipController);
        if (shipController == null || aIDetector == null) return;
        if (aIDetector.Target == null) return;
        if (strafe == null || planer == null || followPlannedPath == null) return;

        followPlannedPath.StopMoving = false;


        if (!strafe.bIsInitilized || strafe.Target != aIDetector.Target)
        {
            strafe.InitilizeStrafePoints(aIDetector.Target, shipCPosition);
            followPlannedPath.ResetFollower();
        }

        Vector3 shipPosition = shipCPosition.position;
        shipPosition.y = 0f;

        Vector3 goal = strafe.GetCurrentTarget();
        goal.y = shipPosition.y;

        planer.SetDestination(goal);

        bool bIsStrafing = (strafe.CurrentIndex == 1);

        if (bIsStrafing)
        {

            if (Vector3.Distance(shipPosition, goal) <= arriveDistance)
            {
                strafe.OnReached();
                followPlannedPath.ResetFollower();

                Vector3 next = strafe.GetCurrentTarget();
                next.y = shipPosition.y;

                planer.SetDestination(next);
            }
        }
    }
}

