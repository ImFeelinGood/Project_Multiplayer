using Unity.Services.Lobbies.Models;

namespace game
{
public class LobbyEvents
{
    public delegate void LobbyUpdated(Lobby lobby);
    public static LobbyUpdated OnLobbyUpdated;
}
}