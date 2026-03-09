using Actions;
using UnityEngine;

public class GamePlayingAction : ActionStack.Action
{
    private readonly GameManager gameManager;
    private bool bPushedNext;

    public GamePlayingAction(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public override void OnBegin(bool bFirstTime)
    {
        bPushedNext = false;
        gameManager.SetState(GameManager.State.GamePlaying);

        Time.timeScale = 1f;
        gameManager.bGameOverRequested = false;
    }

    public override void OnUpdate()
    {
        

        if (!bPushedNext && gameManager.bGameOverRequested)
        {
            bPushedNext = true;
            ActionStack.Main.PushAction(new GameOverAction(gameManager));
        }
    }

    public override bool IsDone() => gameManager.bGameOverRequested;

}
