using Actions;
using UnityEngine;

public class PauseAction : ActionStack.Action
{
    private readonly GameManager gameManager;
    private bool bDone;

    public PauseAction(GameManager manager)
    {
        this.gameManager = manager;
    }

    public override void OnBegin(bool bFirstTime)
    {
        gameManager.ApplyPause(true);
        bDone = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void RequestUnpaused()
    {
        gameManager.ApplyPause(false);
        bDone = true;
    }

    public override void OnEnd()
    {
        gameManager.ApplyPause(false);
    }

    public override bool IsDone() => bDone;
}
