using UnityEngine;

public sealed class SingleplayerMatchStartFlow : IMatchStartFlow
{
    private readonly Lobby _lobby;

    public SingleplayerMatchStartFlow(Lobby lobby)
    {
        _lobby = lobby;
    }

    public bool CanStart(LobbyState state)
    {
        return state != null && state.BotCount > 0;
    }

    public void StartMatch(LobbyState state)
    {
        if (!CanStart(state))
        {
            Debug.LogWarning("Add at least one bot before starting a singleplayer match.");
            return;
        }

        if (!_lobby.PrepareRuntimeSession(state, LobbyMode.Singleplayer))
            return;

        SceneTransitionLoader.LoadScene(_lobby.ResolveSingleplayerArenaSceneName(state));
    }
}
