using System;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{

    

    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {

        resumeButton.onClick.AddListener(() =>
        {
            GameManager.Instance.TogglePause();
        });

        optionsButton.onClick.AddListener(() =>
        {
            OptionsUI.Instance.Show();
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.MainMenuScene);
        });
    }

    private void Start()
    {
        GameManager.Instance.OnGamePaused += GameManager_OnGamePaused;
        GameManager.Instance.OnGameUnpaused += GameManager_OnGameUnpaused;

        Hide();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;



    }

    private void GameManager_OnGamePaused(object sender, EventArgs e) => Show();
    private void GameManager_OnGameUnpaused(object sender, EventArgs e) => Hide();

    private void Show() => gameObject.SetActive(true);
    private void Hide() => gameObject.SetActive(false);

}
