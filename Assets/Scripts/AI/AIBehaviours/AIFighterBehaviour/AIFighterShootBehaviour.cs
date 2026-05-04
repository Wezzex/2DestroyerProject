using UnityEngine;

public class AIFighterShootBehaviour : AIBehavior
{
    [SerializeField] private float fieldOfVisionForShooting = 45;

    public override string Name => "FighterShoot";

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {

        Utility.LogAI("FighterShoot Action is called", shipController);

            shipController.SetShootingState(true);
            shipController.HandleShoot();
        

    }
}
