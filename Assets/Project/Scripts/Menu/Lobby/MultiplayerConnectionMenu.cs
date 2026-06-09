using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MultiplayerConnectionMenu : MonoBehaviour
{
    public static MultiplayerConnectionMenu ActiveInstance { get; private set; }

    [Header("Scene Panels")]
    [FormerlySerializedAs("ConnectionTypePanel")]
    [SerializeField] private GameObject connectionTypePanel;
    [FormerlySerializedAs("HostPanel")]
    [FormerlySerializedAs("hostPanel")]
    [SerializeField] private GameObject lobbyPanel;
    [FormerlySerializedAs("JoinPanel")]
    [SerializeField] private GameObject joinPanel;
    [FormerlySerializedAs("PopupPanel")]
    [SerializeField] private GameObject popupPanel;

    [Header("Scene Text")]
    [FormerlySerializedAs("JoinAddressInput")]
    [SerializeField] private TMP_InputField joinAddressInput;
    [FormerlySerializedAs("StatusText")]
    [SerializeField] private TMP_Text statusText;
    [FormerlySerializedAs("PopupText")]
    [SerializeField] private TMP_Text popupText;

    [Header("Scene Buttons")]
    [SerializeField] private Button confirmJoinButton;
    [SerializeField] private Button backJoinButton;
    [SerializeField] private Button popupOkButton;

    [Header("Join")]
    [SerializeField] private string defaultJoinAddress = "127.0.0.1";
    [SerializeField] private string lobbySceneName = "MultiplayerLobby";

    private bool _subscribedToBootstrap;
    private bool _buttonsBound;
    [SerializeField] private Lobby matchLobby;

    private void Awake()
    {
        ActiveInstance = this;
        EnsureRenderingCamera();
        ValidateSceneReferences();
        ApplyInitialViewState();
        RefreshSlots();
    }

    private void OnEnable()
    {
        BindSceneEvents();
        SubscribeToBootstrap();
    }

    private void OnDisable()
    {
        UnbindSceneEvents();
        UnsubscribeFromBootstrap();
    }

    private void OnDestroy()
    {
        if (ActiveInstance == this)
            ActiveInstance = null;
    }

    public void HostGame()
    {
        ShowLobbyView();

        MultiplayerRuntimeBootstrap.Instance?.HostGame();
        RefreshLobbyStatus();
        LoadLobbySceneIfNeeded();
    }

    public void JoinGame()
    {
        if (connectionTypePanel != null)
            connectionTypePanel.SetActive(false);
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);
        SetJoinPanelVisible(true);

        if (joinAddressInput != null && string.IsNullOrWhiteSpace(joinAddressInput.text))
            joinAddressInput.text = defaultJoinAddress;

        SetStatus("Enter host address");
    }

    public void ConfirmJoinGame()
    {
        bool started = MultiplayerRuntimeBootstrap.Instance != null &&
            MultiplayerRuntimeBootstrap.Instance.JoinGame(NormalizeAddress(joinAddressInput != null ? joinAddressInput.text : defaultJoinAddress));

        if (started)
        {
            SetStatus("Looking for host");
            return;
        }

        ShowPopup("No game found at given address");
    }

    public void BackToConnectionType()
    {
        SetJoinPanelVisible(false);
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);
        if (connectionTypePanel != null)
            connectionTypePanel.SetActive(true);
        SetStatus(string.Empty);
    }

    public void BackToMainMenu()
    {
        if (MultiplayerRuntimeBootstrap.Instance != null)
        {
            MultiplayerRuntimeBootstrap.Instance.BackToMainMenu();
            return;
        }

        SceneTransitionLoader.LoadScene("MainMenu");
    }

    public void RefreshSlots()
    {
        matchLobby?.RefreshSlots();
    }

    private void BindSceneEvents()
    {
        if (_buttonsBound)
            return;

        BindButton(confirmJoinButton, ConfirmJoinGame);
        BindButton(backJoinButton, BackToConnectionType);
        BindButton(popupOkButton, HidePopup);

        if (joinAddressInput != null)
            joinAddressInput.onSubmit.AddListener(HandleJoinAddressSubmit);

        _buttonsBound = true;
    }

    private void UnbindSceneEvents()
    {
        if (!_buttonsBound)
            return;

        UnbindButton(confirmJoinButton, ConfirmJoinGame);
        UnbindButton(backJoinButton, BackToConnectionType);
        UnbindButton(popupOkButton, HidePopup);

        if (joinAddressInput != null)
            joinAddressInput.onSubmit.RemoveListener(HandleJoinAddressSubmit);

        _buttonsBound = false;
    }

    private void BindButton(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void UnbindButton(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private void HandleJoinAddressSubmit(string _)
    {
        ConfirmJoinGame();
    }

    private void SetJoinPanelVisible(bool visible)
    {
        if (joinPanel != null)
            joinPanel.SetActive(visible);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private string NormalizeAddress(string address)
    {
        return string.IsNullOrWhiteSpace(address) ? defaultJoinAddress : address.Trim();
    }

    private void SubscribeToBootstrap()
    {
        if (_subscribedToBootstrap || MultiplayerRuntimeBootstrap.Instance == null)
            return;

        MultiplayerRuntimeBootstrap.Instance.StatusChanged += HandleNetworkStatusChanged;
        MultiplayerRuntimeBootstrap.Instance.PlayerCountChanged += HandlePlayerCountChanged;
        _subscribedToBootstrap = true;
    }

    private void UnsubscribeFromBootstrap()
    {
        if (!_subscribedToBootstrap || MultiplayerRuntimeBootstrap.Instance == null)
            return;

        MultiplayerRuntimeBootstrap.Instance.StatusChanged -= HandleNetworkStatusChanged;
        MultiplayerRuntimeBootstrap.Instance.PlayerCountChanged -= HandlePlayerCountChanged;
        _subscribedToBootstrap = false;
    }

    private void HandleNetworkStatusChanged(string message)
    {
        SetStatus(message);

        if (message == "Waiting for host to start the game")
        {
            ShowLobbyView();
            LoadLobbySceneIfNeeded();
        }
        else if (message == "No player has joined")
        {
            SetStatus(string.Empty);
            ShowPopup(message);
        }
        else if (message == "No game found at given address")
        {
            SetStatus(string.Empty);
            ShowPopup(message);
        }
    }

    private void HandlePlayerCountChanged(int connectedPlayers, int requiredPlayers)
    {
        RefreshSlots();
        SetStatus(connectedPlayers < requiredPlayers
            ? $"Waiting for players ({connectedPlayers}/{requiredPlayers})"
            : $"Players joined ({connectedPlayers}/{requiredPlayers})");
    }

    private void RefreshLobbyStatus()
    {
        SubscribeToBootstrap();

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        if (bootstrap == null)
            return;

        int connectedPlayers = bootstrap.ConnectedPlayerCount;
        int requiredPlayers = bootstrap.MinimumHumanPlayers;
        SetStatus(connectedPlayers < requiredPlayers
            ? $"Waiting for players ({connectedPlayers}/{requiredPlayers})"
            : $"Players joined ({connectedPlayers}/{requiredPlayers})");
        RefreshSlots();
    }

    private void ShowPopup(string message)
    {
        if (popupText != null)
            popupText.text = message;

        SetPopupVisible(true);
    }

    private void HidePopup()
    {
        SetPopupVisible(false);
    }

    private void SetPopupVisible(bool visible)
    {
        if (popupPanel != null)
            popupPanel.SetActive(visible);
    }

    private void ApplyInitialViewState()
    {
        SetJoinPanelVisible(false);
        SetPopupVisible(false);

        if (matchLobby != null)
        {
            ShowLobbyView();
            return;
        }

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        if (bootstrap != null && (bootstrap.IsServerStarted || bootstrap.IsClientStarted))
        {
            ShowLobbyView();
            return;
        }

        if (connectionTypePanel != null)
            connectionTypePanel.SetActive(true);
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);
    }

    private void ShowLobbyView()
    {
        if (connectionTypePanel != null)
            connectionTypePanel.SetActive(false);
        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);
        SetJoinPanelVisible(false);
    }

    private void LoadLobbySceneIfNeeded()
    {
        if (matchLobby != null || string.IsNullOrWhiteSpace(lobbySceneName))
            return;

        if (SceneManager.GetActiveScene().name == lobbySceneName)
            return;

        SceneTransitionLoader.LoadScene(lobbySceneName);
    }

    private static void EnsureRenderingCamera()
    {
        if (Camera.allCamerasCount > 0)
            return;

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.depth = -100f;

        if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
            cameraObject.AddComponent<AudioListener>();
    }

    private void ValidateSceneReferences()
    {
        ValidateReference(joinPanel, nameof(joinPanel));
        ValidateReference(joinAddressInput, nameof(joinAddressInput));
        ValidateReference(statusText, nameof(statusText));
        ValidateReference(popupPanel, nameof(popupPanel));
        ValidateReference(popupText, nameof(popupText));
        ValidateReference(confirmJoinButton, nameof(confirmJoinButton));
        ValidateReference(backJoinButton, nameof(backJoinButton));
        ValidateReference(popupOkButton, nameof(popupOkButton));
    }

    private void ValidateReference(Object reference, string fieldName)
    {
        if (reference == null)
            Debug.LogError($"{nameof(MultiplayerConnectionMenu)} on {name} is missing scene reference '{fieldName}'.", this);
    }
}
