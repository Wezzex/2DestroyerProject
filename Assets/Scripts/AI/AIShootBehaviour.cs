using System;
using UnityEngine;

public class AIShootBehaviour : AIBehavior
{
    [SerializeField] private float fieldOfVisionForShooting = 60;
    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        Vector3 direction = aIDetector.Target.position - shipController.aimTurrets[0].transform.position;
        direction.y = 0.0f;

        Vector2 aimDirection = new Vector2(direction.x, direction.z);

        if (TargetInFOV(shipController, aIDetector))
        {
            shipController.HandleMoveShip(Vector2.zero);
            shipController.SetShootingState(true);
            shipController.HandleShoot();
        }

        
        shipController.HandleTurretMovement(aimDirection);
    }

    private bool TargetInFOV(ShipController shipController, AIDetector aIDetector)
    {
        Transform turret = shipController.aimTurrets[0].transform;

        Vector3 direction = aIDetector.Target.position - turret.position;
        direction.y = 0.0f;

        Vector3 turretForward = turret.forward;
        turretForward.y = 0.0f;

        if (direction.sqrMagnitude < 0.0001f) return true;

        float angle = Vector3.Angle(turretForward, direction);
        return angle < fieldOfVisionForShooting * 0.5f;
    }
}
