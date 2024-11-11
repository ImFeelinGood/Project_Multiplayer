using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using game.Events;
using System;

namespace game
{
public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startButton;

    private void OnEnable()
    {
        readyButton.onClick.AddListener(OnReady);

        if(GameLobbyManager.instance.isHost)
        {
            startButton.onClick.AddListener(StartGame);
            Events.LobbyEvents.OnLobbyReady += OnLobbyReady;
        }
    }

    private void OnDisable()
    {
        Events.LobbyEvents.OnLobbyReady -= OnLobbyReady;
        
        readyButton.onClick.RemoveAllListeners();
        startButton.onClick.RemoveAllListeners();
    }
    void Start()
    {
        lobbyCodeText.text = $"Lobby code: {GameLobbyManager.instance.GetLobbyCode()}";
    }

    private async void OnReady()
    {
        bool succeeded = await GameLobbyManager.instance.SetPlayerReady();

        if(succeeded)
        {
            readyButton.gameObject.SetActive(false);
        }
    }

    private void OnLobbyReady()
    {
        startButton.gameObject.SetActive(true);
    }

    private async void StartGame()
    {
        await GameLobbyManager.instance.StartGame();
    }
}
}