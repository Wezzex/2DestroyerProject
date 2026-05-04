using UnityEngine;

public class AIHoldPositionBehaviour : AIBehavior
{
    public override string Name => "HoldPosition";

    public override void PerformAction(ShipController shipController, AIDetector aIDetector)
    {

        Utility.LogAI("HoldPosition Action is called", shipController);

        shipController.HandleMoveShip(Vector2.zero);
    }
}
