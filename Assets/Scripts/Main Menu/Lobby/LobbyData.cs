using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public class LobbyData
{
    private string _relayJoinCode;
    private string _sceneName;

    public string SceneName = "MainGame";

    public string RelayJoinCode
    {
        get => _relayJoinCode;
        set => _relayJoinCode = value;
    }

    public void Initialize(Dictionary<string, DataObject> lobbyData)
    {
        UpdateState(lobbyData);
    }

    public void UpdateState(Dictionary<string, DataObject> lobbyData)
    {
        if (lobbyData.ContainsKey("RelayJoinCode"))
        {
            _relayJoinCode = lobbyData["RelayJoinCode"].Value;
        }

        if (lobbyData.ContainsKey("SceneName"))
        {
            _sceneName = lobbyData["SceneName"].Value;
        }
    }

    public Dictionary<string, string> Serialize()
    {
        return new Dictionary<string, string>()
            {

            {"RelayJoinCode", _relayJoinCode},
            {"SceneName", _sceneName}
            };
    }

    // public void SetRelayCode(string code)
    // {
    //     RelayJoinCode = code;
    // }
}
