using Actions;
using UnityEngine;

public class CountdownAction : ActionStack.Action
{
    private readonly GameManager gameManager;
    private float timer;
    private bool bPushedNext;

    public CountdownAction(GameManager gameManager, float countdownTime)
    {
        this.gameManager = gameManager;
        this.timer = countdownTime;
    }

    public override void OnBegin(bool bFirstTime)
    {
        bPushedNext = false;
        gameManager.SetState(GameManager.State.CountdownToStart);

        Time.timeScale = 0f;
        gameManager.SetCountdown(timer);
    }

    public override void OnUpdate()
    {
        timer -= Time.unscaledDeltaTime;
        gameManager.SetCountdown(timer);

        if (!bPushedNext && timer <= 0.0f)
        {
            bPushedNext = true;
            ActionStack.Main.PushAction(new GamePlayingAction(gameManager));
        }
    }

    public override bool IsDone() => timer <= 0.0f;
}
