using Actions;
using UnityEngine;

public class WaitToStartAction : ActionStack.Action
{
    private readonly GameManager gameManager;
    private float timer;
    private bool bPushedNext;

    public WaitToStartAction(GameManager gameManager, float timer)
    {
        this.gameManager = gameManager;
        this.timer = timer;
    }

    public override void OnBegin(bool bFirstTime)
    {
        bPushedNext = false;
        gameManager.SetState(GameManager.State.WaitingToStart);
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        Cursor.visible = false;
    }

    public override void OnUpdate()
    {
        timer -= Time.unscaledDeltaTime;
        if (!bPushedNext && timer <= 0.0f)
        {
            bPushedNext = true;
            ActionStack.Main.PushAction(new CountdownAction(gameManager, 3f));
        }
    }

    public override bool IsDone() => timer <= 0.0f;
}
