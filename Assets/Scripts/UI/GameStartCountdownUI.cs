using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameStartCountdownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;

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
        if (GameManager.Instance.CurrentState == GameManager.State.CountdownToStart)
        {
            countdownText.text = Mathf.Ceil(GameManager.Instance.CountdownRemaining).ToString();
        }

    }

    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        UpdateVisibility();
    }


    private void UpdateVisibility()
    {
        bool bShow = GameManager.Instance.CurrentState == GameManager.State.CountdownToStart;
        gameObject.SetActive(bShow);
    }
}
