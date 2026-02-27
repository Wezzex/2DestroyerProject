using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button restartButton;

    [SerializeField] private GameObject gameOverUI;


    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        restartButton.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.GameScene);
        });
    }


    private void Start()
    {

        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        UpdateVisibility();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;
        }
    }

    private void Update()
    {
      
    }

    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool bShow = GameManager.Instance.CurrentState == GameManager.State.GameOver;
        gameOverUI.SetActive(bShow);
    }

}
