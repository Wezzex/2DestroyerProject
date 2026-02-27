using Actions;
using UnityEngine;

public class GameOverAction : ActionStack.Action
{
    private readonly GameManager gameManager;

    public GameOverAction(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public override void OnBegin(bool bFirstTime)
    {
        gameManager.SetState(GameManager.State.GameOver);
    }

    public override bool IsDone() => false;
}
