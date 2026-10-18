using UnityEngine;

// Fighters have fixed forward guns, so they only fire while making an attack run at the target:
// target ahead (inside the cone), within range and in line of sight. Never while breaking off to a strafe point.
public class AIFighterShootBehaviour : AIBehavior
{
    [Tooltip("Full cone angle in front of the fighter the target must be inside.")]
    [SerializeField] private float fieldOfVisionForShooting = 45;
    [SerializeField] private float fireRange = 50f;
    [SerializeField] private FighterStrafePoints strafe;
    [Tooltip("The moving body (Motor). Its forward is where the guns point.")]
    [SerializeField] private Transform shipCPosition;

    public override string Name => "FighterShoot";

    private void Start()
    {
        if (strafe == null) strafe = GetComponentInChildren<FighterStrafePoints>();
        if (shipCPosition == null) shipCPosition = UnitSetup.GetBody(gameObject);
    }

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {
        Utility.LogAI("FighterShoot Action is called", shipController);

        shipController.SetShootingState(IsApproachingTarget(aIDetector));
    }

    private bool IsApproachingTarget(AIDetector aIDetector)
    {
        if (aIDetector == null || aIDetector.Target == null || !aIDetector.TargetVisible) return false;
        if (strafe != null && !strafe.IsAttackingTarget) return false;

        Vector3 toTarget = aIDetector.Target.position - shipCPosition.position;
        toTarget.y = 0f;
        if (toTarget.magnitude > fireRange) return false;

        Vector3 forward = shipCPosition.forward;
        forward.y = 0f;
        return Vector3.Angle(forward, toTarget) < fieldOfVisionForShooting * 0.5f;
    }

    // Leaving the pursuit branch (patrol, return, docked) must not leave the guns firing.
    private void OnDisable()
    {
        ShipController shipController = GetComponent<ShipController>();
        if (shipController != null) shipController.SetShootingState(false);
    }
}
