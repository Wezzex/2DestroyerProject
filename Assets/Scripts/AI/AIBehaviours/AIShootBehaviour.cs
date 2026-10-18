using System;
using UnityEngine;

public class AIShootBehaviour : AIBehavior
{
    [SerializeField] private float fieldOfVisionForShooting = 60;

    public override string Name => "Shoot";

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {

        Utility.LogAI("Shoot Action is called", shipController);

        // Ships holding formation run this every frame, with or without a target in range.
        if (aIDetector == null || aIDetector.Target == null)
        {
            shipController.SetShootingState(false);
            return;
        }

        // Squad orders can hold a target beyond sensor range; don't fire at it from across the map.
        if (NavMeshUtility.FlatDistance(aIDetector.transform.position, aIDetector.Target.position) > aIDetector.ViewRadius)
        {
            shipController.SetShootingState(false);
            return;
        }

        Vector3 direction = aIDetector.Target.position - shipController.aimTurrets[0].transform.position;
        direction.y = 0.0f;

        Vector2 aimDirection = new Vector2(direction.x, direction.z);

        // Previously only ever switched on, so a ship kept firing after the target left its turrets' view.
        shipController.SetShootingState(TargetInFOV(shipController, aIDetector));

        shipController.HandleTurretMovement(aimDirection);
    }

    private bool TargetInFOV(ShipController shipController, AIDetector aIDetector)
    {
        Transform turret = shipController.aimTurrets[0].transform;

        Vector3 direction = aIDetector.Target.position - turret.position;
        direction.y = 0.0f;

        Vector3 turretForward = turret.forward;
        turretForward.y = 0.0f;

        float angle = Vector3.Angle(turretForward, direction);
        return angle < fieldOfVisionForShooting * 0.5f;
    }
}
