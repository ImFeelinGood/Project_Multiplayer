using System.Collections.Generic;
using game;
using UnityEngine;
using LobbyEvents = game.Events.LobbyEvents;

public class LobbySpawner : MonoBehaviour
{
    [SerializeField] private LobbyPlayer lobbyPlayer; 
        [SerializeField] private Transform playerContainer;
    private List<LobbyPlayer> players = new List<LobbyPlayer>(); 

    private void OnEnable()
    {
        LobbyEvents.OnLobbyUpdated += OnLobbyUpdated;
    }

    private void OnDisable()
    {
        LobbyEvents.OnLobbyUpdated -= OnLobbyUpdated;
    }

    private void OnLobbyUpdated()
    {
        List<LobbyPlayerData> playerDatas = GameLobbyManager.instance.GetPlayers();

        foreach (var player in players)
        {
            Destroy(player.gameObject);
        }

        players.Clear(); 

        for (int i = 0; i < playerDatas.Count; i++)
        {
            LobbyPlayerData data = playerDatas[i];

            LobbyPlayer newPlayer = Instantiate(lobbyPlayer, playerContainer); 
            newPlayer.SetData(data); 
            players.Add(newPlayer);
        }
    }
}
