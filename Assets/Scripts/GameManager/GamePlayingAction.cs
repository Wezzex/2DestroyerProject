using Actions;
using UnityEngine;

public class GamePlayingAction : ActionStack.Action
{
    private readonly GameManager gameManager;
    private float timer;
    private bool bPushedNext;

    public GamePlayingAction(GameManager gameManager, float gameTime)
    {
        this.gameManager = gameManager;
        timer = gameTime;
    }

    public override void OnBegin(bool bFirstTime)
    {
        bPushedNext = false;
        gameManager.SetState(GameManager.State.GamePlaying);
        gameManager.SetGameTime(timer);
    }

    public override void OnUpdate()
    {
        timer -= Time.deltaTime;
        gameManager.SetGameTime(timer);

        if (!bPushedNext && timer <= 0.0f)
        {
            bPushedNext = true;
            ActionStack.Main.PushAction(new GameOverAction(gameManager));
        }
    }

    public override bool IsDone() => timer <= 0.0f;

}
