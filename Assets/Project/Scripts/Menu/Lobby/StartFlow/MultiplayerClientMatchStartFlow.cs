using UnityEngine;

public sealed class MultiplayerClientMatchStartFlow : IMatchStartFlow
{
    public bool CanStart(LobbyState state)
    {
        return false;
    }

    public void StartMatch(LobbyState state)
    {
        Debug.LogWarning("Multiplayer clients cannot start the match. Waiting for the host.");
    }
}
