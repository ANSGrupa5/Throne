using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Obsolete("Use MultiplayerMenuButtons as the multiplayer scene UI owner. This component is transitional only.")]
public sealed class MultiplayerLobby : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private MultiplayerRuntimeBootstrap runtimeBootstrap;

    [Header("Join UI")]
    [SerializeField] private TMP_InputField addressInput;

    [Header("Optional Buttons")]
    [SerializeField] private Button startMatchButton;

    private bool _isHost;

    private void Awake()
    {
        if (runtimeBootstrap == null)
            runtimeBootstrap = MultiplayerRuntimeBootstrap.Instance;

        SetStartMatchAvailable(false);
    }

    public void HostGame()
    {
        if (!TryGetRuntime(out MultiplayerRuntimeBootstrap runtime))
            return;

        _isHost = true;
        SetStartMatchAvailable(true);
        runtime.HostGame();
    }

    public void JoinGame()
    {
        if (!TryGetRuntime(out MultiplayerRuntimeBootstrap runtime))
            return;

        _isHost = false;
        SetStartMatchAvailable(false);

        string address = addressInput != null ? addressInput.text : string.Empty;
        runtime.JoinGame(address);
    }

    public void StartMatch()
    {
        if (!_isHost)
        {
            Debug.LogWarning("Only the host can start a multiplayer match.");
            return;
        }

        if (!TryGetRuntime(out MultiplayerRuntimeBootstrap runtime))
            return;

        runtime.StartMatch();
    }

    public void BackToMainMenu()
    {
        if (!TryGetRuntime(out MultiplayerRuntimeBootstrap runtime))
            return;

        runtime.BackToMainMenu();
    }

    private bool TryGetRuntime(out MultiplayerRuntimeBootstrap runtime)
    {
        if (runtimeBootstrap == null)
            runtimeBootstrap = MultiplayerRuntimeBootstrap.Instance;

        runtime = runtimeBootstrap;

        if (runtime == null)
        {
            Debug.LogError("MultiplayerLobby cannot continue because MultiplayerRuntimeBootstrap is missing.");
            return false;
        }

        return true;
    }

    private void SetStartMatchAvailable(bool available)
    {
        if (startMatchButton != null)
            startMatchButton.interactable = available;
    }
}
