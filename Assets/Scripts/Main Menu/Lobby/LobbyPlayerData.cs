using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace game
{
public class LobbyPlayerData
{
    private string id;
    private string gamertag;
    private bool isReady;

    public string Id => id;
    public string Gamertag => gamertag;

    public bool IsReady
    {
        get => isReady;
        set => isReady = value;
    }

    public void Initialize(string _id, string _gamertag)
    {
        id = _id;
        gamertag = _gamertag;
    }

    public void Initialize(Dictionary<string, PlayerDataObject> playerData)
    {
        UpdateState(playerData);
    }

    public void UpdateState(Dictionary<string, PlayerDataObject> playerData)
    {

        if(playerData.ContainsKey("Id"))
        {
            id = playerData["Id"].Value;
        }

        if(playerData.ContainsKey("Gamertag"))
        {
            gamertag = playerData["Gamertag"].Value;
        }

        if(playerData.ContainsKey("IsReady"))
        {
            isReady = playerData["IsReady"].Value == "True";
        }
    }

    public Dictionary<string,string> Serialize()
    {
        return new Dictionary<string, string>()
        {
            {"Id", id},
            {"Gamertag", gamertag},
            {"IsReady", isReady.ToString()}
        };
    }
}
}