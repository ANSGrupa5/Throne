using UnityEngine;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [Header("Default Config Assets")]
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private BotsSettings botsSettings;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private MatchDefaults matchDefaults;
    [SerializeField] private MatchRules matchRules;

    [Header("Role")]
    [SerializeField] private LobbyMode configuredLobbyMode = LobbyMode.Singleplayer;

    [Header("Player Color")]
    [SerializeField] private Color playerTrailColor = Color.white;
    [SerializeField] private TrailColorButtonView[] trailColorButtons;
    [SerializeField] private TrailColorSelectionPanelBinding trailColorPanelBinding;
    [SerializeField] private MatchSettingsPanelBinding matchSettingsPanelBinding;

    [Header("Scene Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    [SerializeField] private string multiplayerArenaSceneName = "Neon City XL Multiplayer";

    [Header("Role Views")]
    [SerializeField] private SingleplayerOpponentSlotView opponents = new();
    [SerializeField] private MultiplayerHostOpponentSlotView hostOpponents = new();
    [SerializeField] private MultiplayerClientOpponentSlotView clientOpponents = new();
    [SerializeField] private ScooterSelectView scooters = new();
    [SerializeField] private EditableMatchSettingsView matchSettings = new();
    [SerializeField] private MultiplayerHostMatchSettingsView hostMatchSettings = new();
    [SerializeField] private MultiplayerClientMatchSettingsView clientMatchSettings = new();

    private OpponentSlotView _opponentSlots;
    private ScooterSelectView _scooterSelect;
    private TrailColorSelectionView _trailColorSelection;
    private MatchSettingsView _matchSettings;
    private readonly SingleplayerTrailColorSelectionView _singleplayerTrailColors = new();
    private readonly MultiplayerTrailColorSelectionView _multiplayerTrailColors = new();
    private bool _componentsEnabled;
    private bool _componentsConfigured;
    private LobbyMode _activeLobbyMode;
    private LobbyState _lobbyState;
    private IMatchStartFlow _startFlow;
    private readonly MatchSessionFactory _matchSessionFactory = new();
    private SingleplayerMatchStartFlow _singleplayerStartFlow;
    private MultiplayerHostMatchStartFlow _multiplayerHostStartFlow;
    private MultiplayerClientMatchStartFlow _multiplayerClientStartFlow;

    protected string ArenaSceneName { get; private set; } = "Neon City XL";

    public int BotCount => _opponentSlots != null ? _opponentSlots.BotCount : 0;

    internal GameSettings GameSettings => gameSettings;
    internal BotsSettings BotsSettings => botsSettings;
    internal PlayerLook PlayerLook => playerLook;
    internal TrailColorButtonView[] TrailColorButtons => trailColorButtons;

    internal Color PlayerTrailColor
    {
        get => playerTrailColor;
        set => playerTrailColor = value;
    }

    internal OpponentSlotView Opponents => _opponentSlots;
    internal MatchSettingsView MatchSettings => _matchSettings;
    internal TrailColorSelectionView TrailColors => _trailColorSelection;

    protected virtual void Awake()
    {
        configuredLobbyMode = LobbyLaunchContext.ConsumeMode(configuredLobbyMode);
        ResolvePrefabBindings();
        ValidateSceneReferences();

        if (gameSettings != null)
            ArenaSceneName = gameSettings.arenaSceneName;

        if (playerLook != null)
            playerTrailColor = playerLook.trailColor;

        InitializeLobbyStateMirror();
        EnsureComponentsForCurrentRole(true);
        _opponentSlots?.Validate(this);
        _scooterSelect?.Validate(this);
        SyncLobbyStateFromCurrentSelections();
        RefreshStartButtonInteractivity();
    }

    protected virtual void OnEnable()
    {
        EnsureComponentsForCurrentRole();
        EnableActiveComponents();
        RefreshActiveComponents();
        SyncLobbyStateFromCurrentSelections();
        RefreshStartButtonInteractivity();
    }

    protected virtual void OnDisable()
    {
        DisableActiveComponents();
    }

    protected virtual void Update()
    {
        if (EnsureComponentsForCurrentRole())
        {
            RefreshActiveComponents();
            SyncLobbyStateFromCurrentSelections();
            RefreshStartButtonInteractivity();
        }

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
        SyncLobbyStateFromCurrentSelections();
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
        SyncLobbyStateFromCurrentSelections();
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
        StartMatch();
    }

    public void LoadScene()
    {
        StartMatch();
    }

    public void StartMatch()
    {
        EnsureComponentsForCurrentRole();
        ArenaSceneName = ResolvePlayableArenaSceneName(ArenaSceneName);
        SyncLobbyStateFromCurrentSelections();
        ConfigureStartFlowForCurrentRole();

        if (_startFlow == null || !_startFlow.CanStart(_lobbyState))
        {
            RefreshStartButtonInteractivity();
            return;
        }

        _startFlow.StartMatch(_lobbyState);
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

    internal bool IsReadOnlyMultiplayerClient()
    {
        if (!MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return false;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && bootstrap.IsClientStarted && !bootstrap.IsServerStarted;
    }

    internal bool PrepareRuntimeSession(LobbyState state, LobbyMode runtimeMode)
    {
        if (state == null)
            return false;

        _trailColorSelection?.ApplyCurrentSelectionToDefaults();

        state.LobbyMode = runtimeMode;
        GameSessionRuntime session = _matchSessionFactory.Create(
            state,
            matchDefaults,
            gameSettings,
            botsSettings,
            playerLook);
        session.isSingleplayer = runtimeMode == LobbyMode.Singleplayer;
        GameSessionBootstrap.SetSession(session);
        return true;
    }

    internal bool PublishCurrentHostLobbyState(LobbyState state)
    {
        if (state == null)
            return false;

        if (ReferenceEquals(state, _lobbyState))
            SyncLobbyStateFromCurrentSelections();

        LobbyStateSnapshot snapshot = LobbyStateSnapshot.FromLobbyState(
            state,
            _opponentSlots != null ? _opponentSlots.SlotCount : state.ParticipantCount,
            _opponentSlots != null ? _opponentSlots.GetBotSlotMask() : 0);

        if (!MultiplayerSessionDriver.PublishHostLobbyState(snapshot))
            return false;

        ClearLobbyStateDirty();
        return true;
    }

    internal void ApplySyncedLobbyStateSnapshot(LobbyStateSnapshot snapshot)
    {
        if (_lobbyState == null)
            InitializeLobbyStateMirror();

        _lobbyState.LobbyMode = ResolveLobbyMode();
        _lobbyState.HumanPlayerCount = snapshot.HumanPlayers;
        _lobbyState.BotCount = snapshot.BotCount;
        _lobbyState.TrailLength = snapshot.TrailLength;
        _lobbyState.MatchDurationSeconds = snapshot.MatchDurationSeconds;
        _lobbyState.MatchMode = snapshot.MatchMode;
        _lobbyState.SuddenDeath = snapshot.SuddenDeath;
        _lobbyState.IsDirty = false;
    }

    internal string ResolveSingleplayerArenaSceneName(LobbyState state)
    {
        return ResolvePlayableArenaSceneName(state != null ? state.ArenaSceneName : ArenaSceneName);
    }

    internal string ResolveMultiplayerArenaSceneName(LobbyState state)
    {
        if (!string.IsNullOrWhiteSpace(multiplayerArenaSceneName))
            return multiplayerArenaSceneName;

        return ResolvePlayableArenaSceneName(state != null ? state.ArenaSceneName : ArenaSceneName);
    }

    private void InitializeLobbyStateMirror()
    {
        _lobbyState = LobbyStateGameSettingsAdapter.CreateLobbyStateFromGameSettings(gameSettings, ResolveLobbyMode());
        _lobbyState.ArenaSceneName = string.IsNullOrWhiteSpace(ArenaSceneName)
            ? _lobbyState.ArenaSceneName
            : ArenaSceneName;
        _lobbyState.SelectedTrailColor = playerLook != null ? playerLook.trailColor : playerTrailColor;
        _lobbyState.SelectedTrailColorIndex = ResolveTrailColorIndex(_lobbyState.SelectedTrailColor);
        _lobbyState.SelectedPlayerModelIndex = _scooterSelect != null
            ? _scooterSelect.SelectedModelIndex
            : 0;
        _lobbyState.IsDirty = true;
    }

    private void SyncLobbyStateFromCurrentSelections()
    {
        if (_lobbyState == null)
            InitializeLobbyStateMirror();

        bool wasDirty = _lobbyState.IsDirty;
        LobbyMode lobbyMode = ResolveLobbyMode();
        Color selectedTrailColor = playerLook != null ? playerLook.trailColor : playerTrailColor;

        _lobbyState.LobbyMode = lobbyMode;
        _lobbyState.ArenaSceneName = string.IsNullOrWhiteSpace(ArenaSceneName)
            ? _lobbyState.ArenaSceneName
            : ArenaSceneName;
        _lobbyState.HumanPlayerCount = ResolveHumanPlayerCount(lobbyMode);
        _lobbyState.BotCount = _opponentSlots != null ? _opponentSlots.BotCount : 0;
        _lobbyState.SelectedTrailColor = selectedTrailColor;
        _lobbyState.SelectedTrailColorIndex = _trailColorSelection != null
            ? _trailColorSelection.SelectedColorIndex
            : ResolveTrailColorIndex(selectedTrailColor);
        _lobbyState.SelectedPlayerModelIndex = _scooterSelect != null
            ? _scooterSelect.SelectedModelIndex
            : 0;

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

    private LobbyMode ResolveLobbyMode()
    {
        if (configuredLobbyMode == LobbyMode.Singleplayer)
            return LobbyMode.Singleplayer;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && bootstrap.IsServerStarted
            ? LobbyMode.MultiplayerHost
            : LobbyMode.MultiplayerClient;
    }

    private int ResolveHumanPlayerCount(LobbyMode lobbyMode)
    {
        switch (lobbyMode)
        {
            case LobbyMode.Singleplayer:
                return 1;
            case LobbyMode.MultiplayerHost:
            case LobbyMode.MultiplayerClient:
                if (_opponentSlots != null)
                    return Mathf.Max(0, _opponentSlots.GetHumanSlotCount());

                MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
                return bootstrap != null ? Mathf.Max(0, bootstrap.ConnectedPlayerCount) : 0;
            default:
                return 0;
        }
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

    private string ResolvePlayableArenaSceneName(string sceneName)
    {
        string configuredSceneName = !string.IsNullOrWhiteSpace(sceneName)
            ? sceneName
            : gameSettings != null
                ? gameSettings.arenaSceneName
                : string.Empty;

        return string.IsNullOrWhiteSpace(configuredSceneName) || IsNonMatchScene(configuredSceneName)
            ? "Neon City XL"
            : configuredSceneName;
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

    private bool EnsureComponentsForCurrentRole(bool force = false)
    {
        LobbyMode lobbyMode = ResolveLobbyMode();
        if (!force && _componentsConfigured && _activeLobbyMode == lobbyMode)
            return false;

        _activeLobbyMode = lobbyMode;
        _componentsConfigured = true;
        ConfigureComponentsForCurrentRole(lobbyMode);
        ConfigureStartFlow(lobbyMode);
        return true;
    }

    private void ConfigureComponentsForCurrentRole(LobbyMode lobbyMode)
    {
        switch (lobbyMode)
        {
            case LobbyMode.Singleplayer:
                UseComponents(opponents, scooters, _singleplayerTrailColors, matchSettings);
                break;
            case LobbyMode.MultiplayerHost:
                UseComponents(hostOpponents, scooters, _multiplayerTrailColors, hostMatchSettings);
                break;
            case LobbyMode.MultiplayerClient:
                UseComponents(clientOpponents, scooters, _multiplayerTrailColors, clientMatchSettings);
                break;
        }
    }

    private void ConfigureStartFlowForCurrentRole()
    {
        ConfigureStartFlow(ResolveLobbyMode());
    }

    private void ConfigureStartFlow(LobbyMode lobbyMode)
    {
        if (_singleplayerStartFlow == null)
            _singleplayerStartFlow = new SingleplayerMatchStartFlow(this);
        if (_multiplayerHostStartFlow == null)
            _multiplayerHostStartFlow = new MultiplayerHostMatchStartFlow(this);
        if (_multiplayerClientStartFlow == null)
            _multiplayerClientStartFlow = new MultiplayerClientMatchStartFlow();

        _startFlow = lobbyMode switch
        {
            LobbyMode.MultiplayerHost => _multiplayerHostStartFlow,
            LobbyMode.MultiplayerClient => _multiplayerClientStartFlow,
            _ => _singleplayerStartFlow
        };
    }

    private void RefreshStartButtonInteractivity()
    {
        ConfigureStartFlowForCurrentRole();

        if (startButton != null)
            startButton.interactable = _startFlow != null && _startFlow.CanStart(_lobbyState);
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
        ResolvePrefabBindings();

        if (trailColorButtons == null || trailColorButtons.Length == 0)
            Debug.LogError($"{nameof(LobbyController)} on {name} has no trail color buttons assigned.", this);
        else
        {
            for (int i = 0; i < trailColorButtons.Length; i++)
                trailColorButtons[i]?.Validate(this, i);
        }

        if (startButton == null)
            Debug.LogError($"{nameof(LobbyController)} on {name} has no start button assigned.", this);
        if (backButton == null)
            Debug.LogError($"{nameof(LobbyController)} on {name} has no back button assigned.", this);
    }

    private void ResolvePrefabBindings()
    {
        if ((trailColorButtons == null || trailColorButtons.Length == 0) && trailColorPanelBinding != null)
            trailColorButtons = trailColorPanelBinding.TrailColorButtons;

        if (matchSettingsPanelBinding == null)
            matchSettingsPanelBinding = GetComponentInChildren<MatchSettingsPanelBinding>(true);

        if (matchSettingsPanelBinding == null)
        {
            Transform bindingRoot = FindChildByName(transform, "MatchSettingsPanel");
            if (bindingRoot == null)
                bindingRoot = transform;

            matchSettingsPanelBinding = bindingRoot.GetComponent<MatchSettingsPanelBinding>();
            if (matchSettingsPanelBinding == null)
                matchSettingsPanelBinding = bindingRoot.gameObject.AddComponent<MatchSettingsPanelBinding>();
        }

        matchSettingsPanelBinding.ResolveReferences();

        if (matchSettings != null)
            matchSettings.BindReferences(matchSettingsPanelBinding);
        if (hostMatchSettings != null)
            hostMatchSettings.BindReferences(matchSettingsPanelBinding);
        if (clientMatchSettings != null)
            clientMatchSettings.BindReferences(matchSettingsPanelBinding);
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildByName(root.GetChild(i), childName);
            if (result != null)
                return result;
        }

        return null;
    }
}

public abstract class LobbyComponent
{
    private bool _initialized;

    protected LobbyController Lobby { get; private set; }

    public void Initialize(LobbyController lobby)
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
