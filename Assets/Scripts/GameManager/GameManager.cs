using System;
using UnityEngine;
using Actions;
using Unity.VisualScripting;


public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }

    public event EventHandler OnStateChanged;
    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;
    public enum State
    {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
        Paused
    }

    [Header("Timers")]

    [SerializeField] private float waitingToStartTime = 1.0f;
    [SerializeField] private float gamePlayingTimeMax = 300.0f;


    public State CurrentState { get; private set; } = State.WaitingToStart;
    private State stateBeforePausing;

    public float CountdownRemaining { get; private set; }
    public float GameRemaining { get; private set; }

    public enum GameOverReason
    {
        None,
        PlayerDied,
        AllStationsDestroyed
    }
    public GameOverReason Reason { get; private set; } = GameOverReason.None;
    public bool bGameOverRequested { get; set; }



    private bool bIsPaused;
    private PauseAction activePauseAction;
    public bool IsPaused => bIsPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ActionStack.Main.PushAction(new WaitToStartAction(this, waitingToStartTime));

    }

    private void OnEnable()
    {
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.OnPauseAction += PlayerInput_OnPauseAction;
        }
    }
    private void OnDisable()
    {
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.OnPauseAction -= PlayerInput_OnPauseAction;
        }
    }

    private void PlayerInput_OnPauseAction(object sender, EventArgs e)
    {
        if (!bIsPaused)
        {
            activePauseAction = new PauseAction(this);
            ActionStack.Main.PushAction(activePauseAction);
        }
        else
        {
            activePauseAction?.RequestUnpaused();
            activePauseAction = null;
        }
    }

    public void RequestGameOver(GameOverReason reason)
    {
        if (bGameOverRequested) return;
        bGameOverRequested = true;
        Reason = reason;
        ActionStack.Main.PushAction(new GameOverAction(this));
    }

    private void OnPlayerDied()
    {
        RequestGameOver(GameOverReason.PlayerDied);
    }


    public void SetState(State newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetCountdown(float remaining) => CountdownRemaining = remaining;
    public void SetGameTime(float remaining) => GameRemaining = remaining;

    public float GetGamePlayingTimerNormalized()
    {
        if (gamePlayingTimeMax <= 0.0001f) return 0.0f;
        return 1f - (GameRemaining / gamePlayingTimeMax);
    }

    public void TogglePause()
    {
        if (!bIsPaused)
        {
            bIsPaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ActionStack.Main.PushAction(new PauseAction(this));

        }
        else
        {

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    
    }

    public void ApplyPause(bool bPaused)
    {
        if (bPaused)
        {
            if (bIsPaused) return;

            bIsPaused = true;
            stateBeforePausing = CurrentState;
            Time.timeScale = 0f;
            SetState(State.Paused);
            OnGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else 
        {
            if (!bIsPaused) return;

            bIsPaused = false;
            Time.timeScale = 1f;
            SetState(stateBeforePausing);
            OnGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }


}
