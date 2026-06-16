using System;
using UnityEngine;

[Obsolete("Use MultiplayerLobby instead. Remove MultiplayerMenuButtons from the MultiplayerLobby scene.")]
public sealed class MultiplayerMenuButtons : MonoBehaviour
{
    public void HostGame()
    {
        Debug.LogError("MultiplayerMenuButtons is obsolete. Rebind this button to MultiplayerLobby.HostGame.");
    }

    public void JoinGame()
    {
        Debug.LogError("MultiplayerMenuButtons is obsolete. Rebind this button to MultiplayerLobby.JoinGame.");
    }

    public void StartMatch()
    {
        Debug.LogError("MultiplayerMenuButtons is obsolete. Rebind this button to MultiplayerLobby.StartMatch.");
    }

    public void BackToMainMenu()
    {
        Debug.LogError("MultiplayerMenuButtons is obsolete. Rebind this button to MultiplayerLobby.BackToMainMenu.");
    }
}
