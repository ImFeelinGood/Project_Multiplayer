using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace game
{
    public class GameLobbyManager : MonoBehaviour
    {
        private List<LobbyPlayerData> lobbyPlayerDatas = new List<LobbyPlayerData>();
        private LobbyPlayerData localLobbyPlayerData;

        private LobbyData lobbyData;
        public int maxNumberOfPlayers = 4;
        private int playerCount = 1;

        private bool inGame = false;
        public static GameLobbyManager instance;

        public bool isHost => localLobbyPlayerData.Id == LobbyManager.instance.GetHostId();

        void Awake()
        {
            instance = this;
        }

        private void OnEnable()
        {
            LobbyEvents.OnLobbyUpdated += onLobbyUpdated;
        }

        private void OnDisable()
        {
            LobbyEvents.OnLobbyUpdated -= onLobbyUpdated;
        }

        public async Task<bool> CreateLobby()
        {
            playerCount = 1;  // Host is the first player
            localLobbyPlayerData = new LobbyPlayerData();
            localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, $"Player {playerCount}");

            lobbyData = new LobbyData();

            bool succeeded = await LobbyManager.instance.CreateLobby(maxNumberOfPlayers, true, localLobbyPlayerData.Serialize(), lobbyData.Serialize());

            if (succeeded)
            {
                Debug.Log("Lobby created successfully.");
            }
            else
            {
                Debug.LogWarning("Failed to create the lobby.");
            }

            return succeeded;
        }

        public string GetLobbyCode()
        {
            return LobbyManager.instance.GetLobbyCode();
        }

        public async Task<bool> JoinLobby(string code)
        {
            playerCount++;  // Increment player count for each new join
            localLobbyPlayerData = new LobbyPlayerData();
            localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, $"Player {playerCount}");

            bool succeeded = await LobbyManager.instance.JoinLobby(code, localLobbyPlayerData.Serialize());
            return succeeded;
        }

        private async void onLobbyUpdated(Lobby lobby)
        {
            List<Dictionary<string, PlayerDataObject>> playerData = LobbyManager.instance.GetPlayerData();
            lobbyPlayerDatas.Clear();

            int numberOfPlayersReady = 0;

            foreach (Dictionary<string, PlayerDataObject> data in playerData)
            {
                LobbyPlayerData lobbyPlayerData = new LobbyPlayerData();
                lobbyPlayerData.Initialize(data);

                if (lobbyPlayerData.IsReady)
                {
                    numberOfPlayersReady++;
                }

                if (lobbyPlayerData.Id == AuthenticationService.Instance.PlayerId)
                {
                    localLobbyPlayerData = lobbyPlayerData;
                }

                lobbyPlayerDatas.Add(lobbyPlayerData);
            }

            lobbyData = new LobbyData();
            lobbyData.Initialize(lobby.Data);

            Events.LobbyEvents.OnLobbyUpdated?.Invoke();

            if (numberOfPlayersReady == lobby.Players.Count)
            {
                Events.LobbyEvents.OnLobbyReady?.Invoke();
            }

            if (lobbyData.RelayJoinCode != default && !inGame)
            {
                await JoinRelayServer(lobbyData.RelayJoinCode);
                SceneManager.LoadSceneAsync(lobbyData.SceneName);
            }
        }

        public List<LobbyPlayerData> GetPlayers()
        {
            return lobbyPlayerDatas;
        }

        public async Task<bool> SetPlayerReady()
        {
            localLobbyPlayerData.IsReady = true;
            return await LobbyManager.instance.UpdatePlayerData(localLobbyPlayerData.Id, localLobbyPlayerData.Serialize());
        }

        public async Task StartGame()
        {
            string relayJoinCode = await RelayManager.instance.CreateRelay(maxNumberOfPlayers);
            inGame = true;

            lobbyData.RelayJoinCode = relayJoinCode;
            await LobbyManager.instance.UpdateLobbyData(lobbyData.Serialize());

            string allocationId = RelayManager.instance.GetAllocationId();
            string connectionData = RelayManager.instance.GetConnectionData();
            await LobbyManager.instance.UpdatePlayerData(localLobbyPlayerData.Id, localLobbyPlayerData.Serialize(), allocationId, connectionData);

            SceneManager.LoadSceneAsync(lobbyData.SceneName);
        }

        private async Task<bool> JoinRelayServer(string relayJoinCode)
        {
            inGame = true;

            await RelayManager.instance.JoinRelay(relayJoinCode);

            string allocationId = RelayManager.instance.GetAllocationId();
            string connectionData = RelayManager.instance.GetConnectionData();
            await LobbyManager.instance.UpdatePlayerData(localLobbyPlayerData.Id, localLobbyPlayerData.Serialize(), allocationId, connectionData);

            return true;
        }
    }
}
