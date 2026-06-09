using UnityEngine;
using UnityEngine.UI;

public abstract class Lobby : MonoBehaviour
{
    [Header("Default Config Assets")]
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private BotsSettings botsSettings;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private MatchRules matchRules;

    [Header("Player Color")]
    [SerializeField] private Color playerTrailColor = Color.white;
    [SerializeField] private TrailColorButtonView[] trailColorButtons;

    [Header("Scene Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    [Header("UI Audio")]
    [SerializeField] private AudioClip uiClickSound;

    [Header("Vehicle Previews")]
    [SerializeField] private int currentModel;
    [SerializeField] private GameObject[] motorPreview;
    [SerializeField] private GameObject[] motorPlayable;

    [SerializeField] private string multiplayerArenaSceneName = "Neon City XL Multiplayer";

    private OpponentSlotView _opponentSlots;
    private ScooterSelectView _scooterSelect;
    private TrailColorSelectionView _trailColorSelection;
    private MatchSettingsView _matchSettings;
    private bool _componentsEnabled;
    private bool _componentsConfigured;
    private LobbyMode _configuredLobbyMode;
    private LobbyState _lobbyState;

    protected string ArenaSceneName { get; private set; } = "Neon City XL";

    public int BotCount => _opponentSlots != null ? _opponentSlots.BotCount : 0;

    internal GameSettings GameSettings => gameSettings;
    internal BotsSettings BotsSettings => botsSettings;
    internal PlayerLook PlayerLook => playerLook;
    internal TrailColorButtonView[] TrailColorButtons => trailColorButtons;
    internal GameObject[] MotorPreview => motorPreview;
    internal GameObject[] MotorPlayable => motorPlayable;
    internal int CurrentModel
    {
        get => currentModel;
        set => currentModel = value;
    }

    internal Color PlayerTrailColor
    {
        get => playerTrailColor;
        set => playerTrailColor = value;
    }

    internal OpponentSlotView Opponents => _opponentSlots;
    internal MatchSettingsView MatchSettings => _matchSettings;
    internal TrailColorSelectionView TrailColors => _trailColorSelection;

    protected abstract bool IsSingleplayerLobby { get; }
    protected abstract void ConfigureComponentsForCurrentRole();

    protected virtual void Awake()
    {
        ValidateSceneReferences();
        ResolveSceneButtons();

        if (gameSettings != null)
            ArenaSceneName = gameSettings.arenaSceneName;

        if (playerLook != null)
            playerTrailColor = playerLook.trailColor;

        InitializeLobbyStateMirror();
        EnsureComponentsForCurrentRole(true);
        _opponentSlots?.Validate(this);
        RefreshActiveComponents();
        RefreshStartButtonInteractivity();
        SyncLobbyStateFromCurrentSelections();
    }

    protected virtual void OnEnable()
    {
        EnsureComponentsForCurrentRole();
        EnableActiveComponents();
        RefreshActiveComponents();
        RefreshStartButtonInteractivity();
    }

    protected virtual void OnDisable()
    {
        DisableActiveComponents();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (startButton == null || backButton == null)
            ResolveSceneButtons();
    }
#endif

    protected virtual void Update()
    {
        EnsureComponentsForCurrentRole();
        _opponentSlots?.Tick();
        _scooterSelect?.Tick();
        _trailColorSelection?.Tick();
        _matchSettings?.Tick();
    }

    protected void UseComponents(
        OpponentSlotView opponentSlots,
        ScooterSelectView scooterSelect,
        TrailColorSelectionView trailColorSelection,
        MatchSettingsView matchSettings)
    {
        if (_opponentSlots == opponentSlots &&
            _scooterSelect == scooterSelect &&
            _trailColorSelection == trailColorSelection &&
            _matchSettings == matchSettings)
        {
            return;
        }

        bool reenable = _componentsEnabled;
        if (reenable)
            DisableActiveComponents();

        bool matchSettingsChanged = _matchSettings != matchSettings;

        if (matchSettingsChanged && _matchSettings != null)
            _matchSettings.Changed -= HandleMatchSettingsChanged;

        _opponentSlots = opponentSlots;
        _scooterSelect = scooterSelect;
        _trailColorSelection = trailColorSelection;
        _matchSettings = matchSettings;

        if (matchSettingsChanged && _matchSettings != null)
            _matchSettings.Changed += HandleMatchSettingsChanged;

        InitializeActiveComponents();

        if (reenable)
        {
            EnableActiveComponents();
            RefreshActiveComponents();
        }
    }

    public void RefreshLobby()
    {
        EnsureComponentsForCurrentRole();
        RefreshActiveComponents();
        RefreshStartButtonInteractivity();
    }

    private void RefreshActiveComponents()
    {
        _opponentSlots?.Refresh();
        _matchSettings?.Refresh();
        _scooterSelect?.Refresh();
        _trailColorSelection?.Refresh();
    }

    public void RefreshSlots()
    {
        EnsureComponentsForCurrentRole();
        _opponentSlots?.Refresh();
        RefreshStartButtonInteractivity();
    }

    public void LoadScene(string sceneName)
    {
        if (IsMainMenuScene(sceneName))
        {
            BackToMainMenu();
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            StartMatch();
            return;
        }

        ArenaSceneName = sceneName;
        LoadConfiguredScene(sceneName);
    }

    public void LoadScene()
    {
        StartMatch();
    }

    public void StartMatch()
    {
        string configuredSceneName = !string.IsNullOrWhiteSpace(ArenaSceneName)
            ? ArenaSceneName
            : gameSettings != null
                ? gameSettings.arenaSceneName
                : string.Empty;

        if (string.IsNullOrWhiteSpace(configuredSceneName) || IsNonMatchScene(configuredSceneName))
            configuredSceneName = "Neon City XL";

        ArenaSceneName = configuredSceneName;
        LoadConfiguredScene(configuredSceneName);
    }

    public void BackToMainMenu()
    {
        if (MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
        {
            if (MultiplayerRuntimeBootstrap.Instance != null)
            {
                MultiplayerRuntimeBootstrap.Instance.BackToMainMenu();
                return;
            }

            SceneTransitionLoader.LoadScene("MainMenu");
            return;
        }

        SceneTransitionLoader.LoadScene("MainMenu");
    }

    public void AddBot()
    {
        _opponentSlots?.AddBot();
    }

    public void RemoveBot()
    {
        _opponentSlots?.RemoveBot();
    }

    public void SetBotSlot(int slotIndex, bool occupied)
    {
        _opponentSlots?.SetBotSlot(slotIndex, occupied);
    }

    public void SetPlayerTrailColor(Color color)
    {
        _trailColorSelection?.SetPlayerTrailColor(color);
    }

    public void SetPlayerTrailColorFromPaletteIndex(int index)
    {
        _trailColorSelection?.SetTrailColorIndex(index);
    }

    public void SetTrailColor(int value)
    {
        SetPlayerTrailColorFromPaletteIndex(value);
    }

    public void ToggleSuddenDeath()
    {
        _matchSettings?.ToggleSuddenDeath();
    }

    public void ShowGameTime()
    {
        _matchSettings?.ShowGameTime();
    }

    public void IncreaseMin()
    {
        _matchSettings?.IncreaseMin();
    }

    public void DecreaseMin()
    {
        _matchSettings?.DecreaseMin();
    }

    public void IncreaseSec()
    {
        _matchSettings?.IncreaseSec();
    }

    public void DecreaseSec()
    {
        _matchSettings?.DecreaseSec();
    }

    public void ChangeTrailLength()
    {
        _matchSettings?.ChangeTrailLength();
    }

    public void ChangePlayerModelUp()
    {
        _scooterSelect?.ChangePlayerModelUp();
    }

    public void ChangePlayerModelDown()
    {
        _scooterSelect?.ChangePlayerModelDown();
    }

    public void SetPlayerModel(int selectedMotor)
    {
        _scooterSelect?.SetPlayerModel(selectedMotor);
    }

    public void LogLobbyState()
    {
        Debug.Log("Bots to add: " + BotCount);
        Debug.Log("Sudden death: " + (_matchSettings != null && _matchSettings.SuddenDeath));
        Debug.Log("Match duration seconds: " + (_matchSettings != null ? _matchSettings.MatchDuration : 0f));
        Debug.Log("Trail length: " + (_matchSettings != null ? _matchSettings.TrailLength : 0));
        Debug.Log("Trail color: " + (_trailColorSelection != null ? _trailColorSelection.SelectedColorIndex : 0));
    }

    internal void MarkLobbyStateDirty(bool syncCurrentSelections = true)
    {
        if (_lobbyState == null)
            InitializeLobbyStateMirror();

        _lobbyState.IsDirty = true;

        if (syncCurrentSelections)
            SyncLobbyStateFromCurrentSelections();

        RefreshStartButtonInteractivity();
    }

    internal bool IsLobbyStateDirty => _lobbyState != null && _lobbyState.IsDirty;

    internal void ClearLobbyStateDirty()
    {
        if (_lobbyState != null)
            _lobbyState.IsDirty = false;
    }

    internal void PlayUiClickSound()
    {
        if (!Application.isPlaying)
            return;

        ResolveUiClickSound();
        PersistentUiAudioPlayer.PlayOneShot(uiClickSound);
    }

    internal bool IsReadOnlyMultiplayerClient()
    {
        if (!MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return false;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && bootstrap.IsClientStarted && !bootstrap.IsServerStarted;
    }

    protected virtual bool CanStartMatch()
    {
        return IsSingleplayerLobby;
    }

    protected bool InitializeGame(bool singleplayer)
    {
        EnsureComponentsForCurrentRole();

        if (gameSettings == null)
            return false;

        _trailColorSelection?.ApplyCurrentSelectionToDefaults();

        int botCount = _opponentSlots != null ? _opponentSlots.BotCount : 0;
        SyncLobbyStateFromCurrentSelections(singleplayer);
        // Temporary bridge: runtime session construction still reads GameSettings.
        LobbyStateGameSettingsAdapter.CopyLobbyStateToGameSettings(_lobbyState, gameSettings);

        GameSessionRuntime session = GameSessionRuntime.FromDefaults(gameSettings, botsSettings, playerLook, botCount);
        session.isSingleplayer = singleplayer;
        GameSessionBootstrap.SetSession(session);
        return true;
    }

    private void InitializeLobbyStateMirror()
    {
        _lobbyState = LobbyStateGameSettingsAdapter.CreateLobbyStateFromGameSettings(gameSettings, ResolveLobbyMode());
        _lobbyState.ArenaSceneName = string.IsNullOrWhiteSpace(ArenaSceneName)
            ? _lobbyState.ArenaSceneName
            : ArenaSceneName;
        _lobbyState.SelectedTrailColor = playerLook != null ? playerLook.trailColor : playerTrailColor;
        _lobbyState.SelectedTrailColorIndex = ResolveTrailColorIndex(_lobbyState.SelectedTrailColor);
        _lobbyState.SelectedPlayerModelIndex = Mathf.Max(0, currentModel);
        _lobbyState.IsDirty = true;
    }

    private void SyncLobbyStateFromCurrentSelections(bool? singleplayerOverride = null)
    {
        if (_lobbyState == null)
            InitializeLobbyStateMirror();

        bool wasDirty = _lobbyState.IsDirty;
        LobbyMode lobbyMode = ResolveLobbyMode(singleplayerOverride);
        Color selectedTrailColor = playerLook != null ? playerLook.trailColor : playerTrailColor;

        _lobbyState.LobbyMode = lobbyMode;
        _lobbyState.ArenaSceneName = string.IsNullOrWhiteSpace(ArenaSceneName)
            ? _lobbyState.ArenaSceneName
            : ArenaSceneName;
        _lobbyState.HumanPlayerCount = ResolveHumanPlayerCount(lobbyMode, singleplayerOverride);
        _lobbyState.BotCount = _opponentSlots != null ? _opponentSlots.BotCount : 0;
        _lobbyState.SelectedTrailColor = selectedTrailColor;
        _lobbyState.SelectedTrailColorIndex = _trailColorSelection != null
            ? _trailColorSelection.SelectedColorIndex
            : ResolveTrailColorIndex(selectedTrailColor);
        _lobbyState.SelectedPlayerModelIndex = Mathf.Max(0, currentModel);

        if (_matchSettings != null)
        {
            _lobbyState.MatchMode = _matchSettings.SelectedMatchMode;
            _lobbyState.MatchDurationSeconds = _matchSettings.MatchDuration;
            _lobbyState.SuddenDeath = _matchSettings.SuddenDeath;
            _lobbyState.TrailLength = _matchSettings.TrailLength;
        }
        else if (gameSettings != null)
        {
            _lobbyState.MatchMode = LobbyStateGameSettingsAdapter.ToMatchMode(gameSettings.gameMode);
            _lobbyState.MatchDurationSeconds = gameSettings.matchDuration;
            _lobbyState.SuddenDeath = gameSettings.isSuddenDeath;
            _lobbyState.TrailLength = gameSettings.trailLength;
        }

        _lobbyState.IsDirty = wasDirty;
    }

    private LobbyMode ResolveLobbyMode(bool? singleplayerOverride = null)
    {
        if (singleplayerOverride.HasValue && singleplayerOverride.Value)
            return LobbyMode.Singleplayer;

        if (!singleplayerOverride.HasValue && IsSingleplayerLobby)
            return LobbyMode.Singleplayer;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && bootstrap.IsServerStarted
            ? LobbyMode.MultiplayerHost
            : LobbyMode.MultiplayerClient;
    }

    private int ResolveHumanPlayerCount(LobbyMode lobbyMode, bool? singleplayerOverride)
    {
        if (lobbyMode == LobbyMode.Singleplayer)
            return 1;

        if (singleplayerOverride.HasValue)
            return Mathf.Max(2, _opponentSlots != null ? _opponentSlots.GetHumanSlotCount() : 1);

        if (_opponentSlots != null)
            return Mathf.Max(0, _opponentSlots.GetHumanSlotCount());

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        if (bootstrap != null)
            return Mathf.Max(0, bootstrap.ConnectedPlayerCount);

        return lobbyMode == LobbyMode.MultiplayerClient ? 1 : 0;
    }

    private int ResolveTrailColorIndex(Color color)
    {
        if (gameSettings == null ||
            gameSettings.trailColorPalette == null ||
            gameSettings.trailColorPalette.Count == 0)
        {
            return 0;
        }

        for (int i = 0; i < gameSettings.trailColorPalette.Count; i++)
        {
            if (ApproximatelySameColor(color, gameSettings.trailColorPalette[i]))
                return i;
        }

        return 0;
    }

    private static bool ApproximatelySameColor(Color first, Color second)
    {
        const float tolerance = 0.001f;
        return Mathf.Abs(first.r - second.r) < tolerance &&
               Mathf.Abs(first.g - second.g) < tolerance &&
               Mathf.Abs(first.b - second.b) < tolerance &&
               Mathf.Abs(first.a - second.a) < tolerance;
    }

    private void LoadConfiguredScene(string sceneName)
    {
        if (IsMainMenuScene(sceneName))
        {
            BackToMainMenu();
            return;
        }

        ArenaSceneName = sceneName;
        bool isMultiplayerLobby = !IsSingleplayerLobby;

        EnsureComponentsForCurrentRole();
        _opponentSlots?.Refresh();
        RefreshStartButtonInteractivity();
        if (!isMultiplayerLobby && BotCount <= 0)
        {
            Debug.LogWarning("Add at least one bot before starting a singleplayer match.");
            return;
        }

        if (!InitializeGame(!isMultiplayerLobby))
            return;

        if (!isMultiplayerLobby)
        {
            SceneTransitionLoader.LoadScene(sceneName);
            return;
        }

        if (MultiplayerRuntimeBootstrap.Instance == null || !MultiplayerRuntimeBootstrap.Instance.IsServerStarted)
        {
            Debug.LogWarning("Only the host can start a multiplayer match.");
            return;
        }

        string networkSceneName = string.IsNullOrWhiteSpace(multiplayerArenaSceneName)
            ? sceneName
            : multiplayerArenaSceneName;

        MultiplayerRuntimeBootstrap.Instance.LoadMultiplayerMatchScene(networkSceneName);
    }

    private void InitializeActiveComponents()
    {
        _opponentSlots?.Initialize(this);
        _scooterSelect?.Initialize(this);
        _trailColorSelection?.Initialize(this);
        if (_matchSettings != null)
        {
            _matchSettings.Initialize(_lobbyState, matchRules, _matchSettings.WantsEditAccess);
            _matchSettings.Initialize(this);
        }
    }

    private void HandleMatchSettingsChanged()
    {
        MarkLobbyStateDirty(false);
    }

    private void EnableActiveComponents()
    {
        if (_componentsEnabled)
            return;

        _opponentSlots?.OnEnable();
        _scooterSelect?.OnEnable();
        _trailColorSelection?.OnEnable();
        _matchSettings?.OnEnable();
        _componentsEnabled = true;
    }

    private void DisableActiveComponents()
    {
        if (!_componentsEnabled)
            return;

        _matchSettings?.OnDisable();
        _trailColorSelection?.OnDisable();
        _scooterSelect?.OnDisable();
        _opponentSlots?.OnDisable();
        _componentsEnabled = false;
    }

    private void EnsureComponentsForCurrentRole(bool force = false)
    {
        LobbyMode lobbyMode = ResolveLobbyMode();
        if (!force && _componentsConfigured && _configuredLobbyMode == lobbyMode)
            return;

        _configuredLobbyMode = lobbyMode;
        _componentsConfigured = true;
        ConfigureComponentsForCurrentRole();
    }

    private void RefreshStartButtonInteractivity()
    {
        if (startButton == null)
            ResolveSceneButtons();

        if (startButton != null)
            startButton.interactable = CanStartMatch();
    }

    private void ResolveSceneButtons()
    {
        if (startButton == null)
            startButton = FindNamedButton("PlayButton");
        if (backButton == null)
            backButton = FindNamedButton("BackButton");
    }

    // Temporary migration fallback for old or missing serialized scene button references.
    private Button FindNamedButton(string objectName)
    {
        Transform match = FindDeepChild(transform.root, objectName);
        return match != null ? match.GetComponent<Button>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform match = FindDeepChild(parent.GetChild(i), childName);
            if (match != null)
                return match;
        }

        return null;
    }

    // Temporary migration fallback until UI audio is handled by MenuSelectable/audio presets.
    private void ResolveUiClickSound()
    {
        if (uiClickSound != null)
            return;

        ButtonFeedback[] localFeedback = GetComponentsInChildren<ButtonFeedback>(true);
        for (int i = 0; i < localFeedback.Length; i++)
        {
            if (localFeedback[i] != null && localFeedback[i].ClickSound != null)
            {
                uiClickSound = localFeedback[i].ClickSound;
                return;
            }
        }

        ButtonFeedback[] sceneFeedback = UnityEngine.Object.FindObjectsByType<ButtonFeedback>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < sceneFeedback.Length; i++)
        {
            if (sceneFeedback[i] != null && sceneFeedback[i].ClickSound != null)
            {
                uiClickSound = sceneFeedback[i].ClickSound;
                return;
            }
        }
    }

    private bool IsMainMenuScene(string sceneName)
    {
        return string.Equals(sceneName?.Trim(), "MainMenu", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsNonMatchScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return true;

        switch (sceneName.Trim().ToLowerInvariant())
        {
            case "mainmenu":
            case "singleplayerlobby":
            case "multiplayerlobby":
            case "multiplayerconnection":
            case "gameover":
                return true;
            default:
                return false;
        }
    }

    private void ValidateSceneReferences()
    {
        if (trailColorButtons == null || trailColorButtons.Length == 0)
            Debug.LogError($"{nameof(Lobby)} on {name} has no trail color buttons assigned.", this);
        else
        {
            for (int i = 0; i < trailColorButtons.Length; i++)
                trailColorButtons[i]?.Validate(this, i);
        }
    }
}

public abstract class LobbyComponent
{
    private bool _initialized;

    protected Lobby Lobby { get; private set; }

    public void Initialize(Lobby lobby)
    {
        Lobby = lobby;
        if (_initialized)
            return;

        _initialized = true;
        OnInitialize();
    }

    protected virtual void OnInitialize()
    {
    }

    public virtual void OnEnable()
    {
    }

    public virtual void OnDisable()
    {
    }

    public virtual void Refresh()
    {
    }

    public virtual void Tick()
    {
    }
}
