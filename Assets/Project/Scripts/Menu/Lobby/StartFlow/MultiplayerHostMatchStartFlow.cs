using UnityEngine;

public sealed class MultiplayerHostMatchStartFlow : IMatchStartFlow
{
    private readonly Lobby _lobby;

    public MultiplayerHostMatchStartFlow(Lobby lobby)
    {
        _lobby = lobby;
    }

    public bool CanStart(LobbyState state)
    {
        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return state != null &&
               bootstrap != null &&
               bootstrap.IsServerStarted &&
               bootstrap.ConnectedPlayerCount >= bootstrap.MinimumHumanPlayers;
    }

    public void StartMatch(LobbyState state)
    {
        if (!CanStart(state))
        {
            Debug.LogWarning("Only the host can start a multiplayer match after enough players have joined.");
            return;
        }

        if (!_lobby.PublishCurrentHostLobbyState(state))
            return;

        if (!_lobby.PrepareRuntimeSession(state, LobbyMode.MultiplayerHost))
            return;

        MultiplayerRuntimeBootstrap.Instance.LoadMultiplayerMatchScene(_lobby.ResolveMultiplayerArenaSceneName(state));
    }
}
