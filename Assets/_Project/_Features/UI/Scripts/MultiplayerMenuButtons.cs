using UnityEngine;

public sealed class MultiplayerMenuButtons : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private GameObject connectionTypePanel;
    [SerializeField] private GameObject hostPanel;

    private bool _hostRequested;
    private bool _joinRequested;

    private void Awake()
    {
        ResolvePanelsIfNeeded();
        ShowConnectionType();
    }

    public void HostGame()
    {
        ResolvePanelsIfNeeded();

        if (_hostRequested)
        {
            ShowHostPanel();
            return;
        }

        MultiplayerRuntimeBootstrap runtime = MultiplayerRuntimeBootstrap.Instance;
        if (runtime == null)
        {
            Debug.LogError("Cannot host multiplayer game because MultiplayerRuntimeBootstrap is missing.");
            return;
        }

        _hostRequested = true;
        _joinRequested = false;

        // Switch UI first. FishNet startup may log errors or take time.
        ShowHostPanel();

        runtime.HostGame();
    }

    public void JoinGame()
    {
        ResolvePanelsIfNeeded();

        if (_joinRequested)
            return;

        MultiplayerRuntimeBootstrap runtime = MultiplayerRuntimeBootstrap.Instance;
        if (runtime == null)
        {
            Debug.LogError("Cannot join multiplayer game because MultiplayerRuntimeBootstrap is missing.");
            return;
        }

        _joinRequested = true;
        _hostRequested = false;

        ShowJoinState();

        runtime.JoinGame();
    }

    public void StartMatch()
    {
        MultiplayerRuntimeBootstrap runtime = MultiplayerRuntimeBootstrap.Instance;
        if (runtime == null)
        {
            Debug.LogError("Cannot start multiplayer match because MultiplayerRuntimeBootstrap is missing.");
            return;
        }

        runtime.StartMatch();
    }

    public void BackToMainMenu()
    {
        MultiplayerRuntimeBootstrap runtime = MultiplayerRuntimeBootstrap.Instance;
        if (runtime == null)
        {
            Debug.LogError("Cannot go back because MultiplayerRuntimeBootstrap is missing.");
            return;
        }

        runtime.BackToMainMenu();
    }

    private void ShowConnectionType()
    {
        SetActive(connectionTypePanel, true);
        SetActive(hostPanel, false);
    }

    private void ShowHostPanel()
    {
        SetActive(connectionTypePanel, false);
        SetActive(hostPanel, true);
    }

    private void ShowJoinState()
    {
        SetActive(connectionTypePanel, false);
        SetActive(hostPanel, false);
    }

    private void ResolvePanelsIfNeeded()
    {
        if (connectionTypePanel == null)
        {
            Transform found = transform.Find("ConnectionType");
            if (found != null)
                connectionTypePanel = found.gameObject;
        }

        if (hostPanel == null)
        {
            Transform found = transform.Find("Panel");
            if (found != null)
                hostPanel = found.gameObject;
        }

        if (connectionTypePanel == null)
            Debug.LogWarning("MultiplayerMenuButtons could not find ConnectionType child under Canvas.");

        if (hostPanel == null)
            Debug.LogWarning("MultiplayerMenuButtons could not find Panel child under Canvas.");
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }
}
