using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchLobbyController : MonoBehaviour
{
    [Header("Default Config Assets")]
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private BotsSettings botsSettings;
    [SerializeField] private PlayerLook playerLook;

    [Header("Bots")]
    [SerializeField] private TMP_Text botCountText;

    [Header("Player Color")]
    [SerializeField] private Color playerTrailColor = Color.white;
    [SerializeField] private TrailColorButtonView[] trailColorButtons;

    [Header("Player Slots")]
    [SerializeField] private TMP_Text playerHeading;
    [SerializeField] private LobbySlotView[] lobbySlots;

    [Header("Settings UI")]
    [SerializeField] private TMP_Text minutes;
    [SerializeField] private TMP_Text seconds;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Toggle suddenDeathToggle;

    [Header("UI Audio")]
    [SerializeField] private AudioClip uiClickSound;

    [Header("Trail Length")]
    [SerializeField] private TMP_Text trailLengthText;

    [Header("Vehicle Previews")]
    [SerializeField] private int currentModel;
    [SerializeField] private GameObject[] motorPreview;
    [SerializeField] private GameObject[] motorPlayable;

    [SerializeField] private string multiplayerArenaSceneName = "multi Neon City XL";

    private int _botCount;
    private int min, sec, maxmin, minmin;
    private float timeInSecs;
    private string arenaSceneName = "Neon City XL";
    private int trailLength;
    private int trailColor;
    private string gameMode;
    private bool suddenDeath;
    private bool[] _botSlots;
    private bool _slotButtonsBound;
    private bool _colorButtonsInitialized;
    private bool _settingsEventsBound;
    private bool _lobbyStateDirty = true;
    private int _lastBotMutationFrame = -1;

    public int BotCount => _botCount;

    private void Awake()
    {
        ValidateSceneReferences();

        if (gameSettings != null)
            arenaSceneName = gameSettings.arenaSceneName;

        if (playerLook != null)
            playerTrailColor = playerLook.trailColor;

        ApplyPlayerTrailColor();
        InitializeTrailColorButtons();

        _botSlots = new bool[lobbySlots != null ? lobbySlots.Length : 0];
        _botCount = 0;
        RefreshBotCountUI();
        RefreshPlayerHeading();
        BindLobbySlotButtons();
        RefreshSlots();

        min = 1;
        sec = 0;
        maxmin = 10;
        minmin = 1;
        timeInSecs = 60f;
        ShowGameTime();

        if (gameSettings != null)
        {
            trailLength = gameSettings.trailLength;
            ShowTrailLength();
        }

        currentModel = 0;
        if (motorPreview != null && motorPreview.Length > 0 && motorPreview[0] != null)
            motorPreview[0].SetActive(true);

        RefreshTrailColorSelection();
        PublishMultiplayerTrailColorSelection();
    }

    private void OnEnable()
    {
        MultiplayerSessionDriver.TrailColorSelectionsChanged += HandleTrailColorSelectionsChanged;
        MultiplayerSessionDriver.LobbyStateChanged += HandleLobbyStateChanged;
        BindLobbySlotButtons();
        BindSettingsEvents();
        RefreshLobbyInteractivity();
    }

    private void OnDisable()
    {
        MultiplayerSessionDriver.TrailColorSelectionsChanged -= HandleTrailColorSelectionsChanged;
        MultiplayerSessionDriver.LobbyStateChanged -= HandleLobbyStateChanged;
        UnbindSettingsEvents();
        UnbindLobbySlotButtons();
    }

    private void Update()
    {
        if (_lobbyStateDirty)
            PublishMultiplayerLobbyState();
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

        arenaSceneName = sceneName;
        LoadConfiguredScene(sceneName);
    }

    public void LoadScene()
    {
        StartMatch();
    }

    public void StartMatch()
    {
        string configuredSceneName = !string.IsNullOrWhiteSpace(arenaSceneName)
            ? arenaSceneName
            : gameSettings != null
                ? gameSettings.arenaSceneName
                : string.Empty;

        if (string.IsNullOrWhiteSpace(configuredSceneName) || IsNonMatchScene(configuredSceneName))
            configuredSceneName = "Neon City XL";

        arenaSceneName = configuredSceneName;
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

    private void LoadConfiguredScene(string sceneName)
    {
        if (IsMainMenuScene(sceneName))
        {
            BackToMainMenu();
            return;
        }

        arenaSceneName = sceneName;
        bool isMultiplayerLobby = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene();

        RefreshSlots();
        if (!isMultiplayerLobby && _botCount <= 0)
        {
            Debug.LogWarning("Add at least one bot before starting a singleplayer match.");
            return;
        }

        InitializeGame(!isMultiplayerLobby);

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

    private void ReadSettingsFromUi()
    {
        gameMode = dropdown != null && dropdown.options.Count > dropdown.value
            ? dropdown.options[dropdown.value].text
            : string.Empty;
        suddenDeath = suddenDeathToggle != null && suddenDeathToggle.isOn;
        Debug.Log("Wybrany tryb gry: " + gameMode);
        Debug.Log("Tryb Sudden Death: " + suddenDeath);
    }

    public void AddBot()
    {
        if (!CanEditMatchSettings() || !TryBeginBotMutation())
            return;

        AddBotToFirstAvailableSlot();
    }

    public void RemoveBot()
    {
        if (!CanEditMatchSettings() || !TryBeginBotMutation())
            return;

        RemoveBotFromLastOccupiedSlot();
    }

    public void SetPlayerTrailColor(Color color)
    {
        playerTrailColor = color;
        ApplyPlayerTrailColor();
    }

    public void SetPlayerTrailColorFromPaletteIndex(int index)
    {
        if (gameSettings == null || gameSettings.trailColorPalette == null || gameSettings.trailColorPalette.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, gameSettings.trailColorPalette.Count - 1);
        if (MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() &&
            MultiplayerSessionDriver.IsTrailColorTakenByOtherLocalPlayer(index))
        {
            RefreshTrailColorSelection();
            return;
        }

        trailColor = index;
        SetPlayerTrailColor(gameSettings.trailColorPalette[index]);
        PublishMultiplayerTrailColorSelection();
        RefreshTrailColorSelection();
    }

    private void InitializeGame(bool singleplayer)
    {
        if (gameSettings == null)
            return;

        _botCount = Mathf.Clamp(_botCount, 0, GetMaxBotCount());

        int humanSlots = singleplayer ? 1 : Mathf.Max(2, GetHumanSlotCount());
        gameSettings.maxPlayers = humanSlots + _botCount;
        gameSettings.matchDuration = timeInSecs;
        ReadSettingsFromUi();
        gameSettings.isSuddenDeath = suddenDeath;
        gameSettings.arenaSceneName = arenaSceneName;

        SetPlayerTrailColorFromPaletteIndex(trailColor);

        Debug.Log($"Initializing game with scene '{arenaSceneName}'");
        switch (gameMode)
        {
            case "Deathmatch":
                gameSettings.gameMode = 1;
                break;
            case "Battle Royale":
                gameSettings.gameMode = 0;
                break;
        }

        GameSessionRuntime session = GameSessionRuntime.FromDefaults(gameSettings, botsSettings, playerLook, _botCount);
        session.isSingleplayer = singleplayer;
        GameSessionBootstrap.SetSession(session);
    }

    private int GetMaxBotCount()
    {
        int maxPlayers = lobbySlots != null && lobbySlots.Length > 0 ? lobbySlots.Length : 6;
        bool isMultiplayerLobby = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene();
        int reservedHumanSlots = isMultiplayerLobby ? Mathf.Max(1, GetHumanSlotCount()) : 0;
        return Mathf.Max(0, maxPlayers - reservedHumanSlots);
    }

    private void RefreshBotCountUI()
    {
        if (botCountText != null)
            botCountText.text = _botCount.ToString();
    }

    public void RefreshSlots()
    {
        EnsureBotSlotBuffer();
        BindLobbySlotButtons();
        RefreshLobbyInteractivity();

        if (lobbySlots == null || lobbySlots.Length == 0)
            return;

        if (TryApplySyncedLobbyStateToSlots())
            return;

        int humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        SyncBotCountFromSlots(humanPlayers);

        int maxBotCount = GetMaxBotCount();
        if (_botCount > maxBotCount)
        {
            TrimBotsToMax(maxBotCount, humanPlayers);
            RefreshBotCountUI();
        }

        humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        bool canEditBots = CanEditMatchSettings();
        for (int i = 0; i < lobbySlots.Length; i++)
        {
            bool isHuman = i < humanPlayers;
            bool isBot = !isHuman && IsBotSlotOccupied(i);
            string text = GetSlotLabel(i, isHuman, isBot);
            lobbySlots[i]?.ApplyState(text, isHuman, isBot, canEditBots);
        }

        MarkLobbyStateDirty();
    }

    private void RefreshPlayerHeading()
    {
        if (playerHeading != null)
            playerHeading.text = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? "Players" : "Bots";
    }

    private int GetHumanSlotCount()
    {
        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        if (bootstrap != null && bootstrap.IsServerStarted)
            return Mathf.Max(1, bootstrap.ConnectedPlayerCount);

        return 0;
    }

    private void BindLobbySlotButtons()
    {
        if (_slotButtonsBound || lobbySlots == null)
            return;

        for (int i = 0; i < lobbySlots.Length; i++)
            lobbySlots[i]?.Bind(this, i);

        _slotButtonsBound = true;
    }

    private void UnbindLobbySlotButtons()
    {
        if (!_slotButtonsBound || lobbySlots == null)
            return;

        for (int i = 0; i < lobbySlots.Length; i++)
            lobbySlots[i]?.Unbind();

        _slotButtonsBound = false;
    }

    public void SetBotSlot(int slotIndex, bool occupied)
    {
        if (!CanEditMatchSettings() || !TryBeginBotMutation())
            return;

        SetBotSlotInternal(slotIndex, occupied);
    }

    private void SetBotSlotInternal(int slotIndex, bool occupied)
    {
        int humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        if (slotIndex < humanPlayers || _botSlots == null || slotIndex < 0 || slotIndex >= _botSlots.Length)
            return;

        if (_botSlots[slotIndex] == occupied)
            return;

        _botSlots[slotIndex] = occupied;
        SyncBotCountFromSlots(humanPlayers);
        RefreshBotCountUI();
        RefreshSlots();
    }

    private bool IsBotSlotOccupied(int slotIndex)
    {
        return _botSlots != null && slotIndex >= 0 && slotIndex < _botSlots.Length && _botSlots[slotIndex];
    }

    private void SyncBotCountFromSlots(int humanPlayers)
    {
        if (_botSlots == null)
        {
            _botCount = 0;
            return;
        }

        _botCount = 0;
        for (int i = humanPlayers; i < _botSlots.Length; i++)
        {
            if (_botSlots[i])
                _botCount++;
        }
    }

    private void AddBotToFirstAvailableSlot()
    {
        EnsureBotSlotBuffer();
        int humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        if (_botSlots == null)
            return;

        for (int i = humanPlayers; i < _botSlots.Length; i++)
        {
            if (_botSlots[i])
                continue;

            SetBotSlotInternal(i, true);
            return;
        }
    }

    private void RemoveBotFromLastOccupiedSlot()
    {
        EnsureBotSlotBuffer();
        int humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        if (_botSlots == null)
            return;

        for (int i = _botSlots.Length - 1; i >= humanPlayers; i--)
        {
            if (!_botSlots[i])
                continue;

            SetBotSlotInternal(i, false);
            return;
        }
    }

    private void TrimBotsToMax(int maxBotCount, int humanPlayers)
    {
        if (_botSlots == null)
            return;

        int activeBots = 0;
        for (int i = humanPlayers; i < _botSlots.Length; i++)
        {
            if (!_botSlots[i])
                continue;

            activeBots++;
            if (activeBots > maxBotCount)
                _botSlots[i] = false;
        }

        SyncBotCountFromSlots(humanPlayers);
    }

    private void EnsureBotSlotBuffer()
    {
        int slotCount = lobbySlots != null ? lobbySlots.Length : 0;
        if (_botSlots != null && _botSlots.Length == slotCount)
            return;

        _botSlots = new bool[slotCount];
        _slotButtonsBound = false;
    }

    private void ApplyPlayerTrailColor()
    {
        if (playerLook != null)
            playerLook.trailColor = playerTrailColor;
    }

    private void PublishMultiplayerTrailColorSelection()
    {
        if (!MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return;

        int paletteColorCount = gameSettings != null && gameSettings.trailColorPalette != null
            ? gameSettings.trailColorPalette.Count
            : 0;
        MultiplayerSessionDriver.RequestLocalTrailColor(trailColor, paletteColorCount);
    }

    private void HandleTrailColorSelectionsChanged()
    {
        if (!MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return;

        if (gameSettings != null &&
            gameSettings.trailColorPalette != null &&
            gameSettings.trailColorPalette.Count > 0 &&
            MultiplayerSessionDriver.TryGetLocalTrailColorIndex(out int assignedColorIndex))
        {
            assignedColorIndex = Mathf.Clamp(assignedColorIndex, 0, gameSettings.trailColorPalette.Count - 1);
            if (assignedColorIndex != trailColor)
            {
                trailColor = assignedColorIndex;
                SetPlayerTrailColor(gameSettings.trailColorPalette[trailColor]);
            }
        }

        RefreshTrailColorSelection();
    }

    public void ToggleSuddenDeath()
    {
        if (!CanEditMatchSettings())
            return;

        suddenDeath = suddenDeathToggle != null ? suddenDeathToggle.isOn : !suddenDeath;
        MarkLobbyStateDirty();
    }

    public void LogLobbyState()
    {
        Debug.Log("Bots to add: " + _botCount);
        Debug.Log("Sudden death: " + suddenDeath);
        Debug.Log("Match duration seconds: " + timeInSecs);
        Debug.Log("Trail length: " + trailLength);
        Debug.Log("Trail color: " + trailColor);
    }

    public void ShowGameTime()
    {
        if (minutes != null)
            minutes.text = min.ToString("00");
        if (seconds != null)
            seconds.text = sec.ToString("00");
    }

    private void UpdateTimeInSeconds()
    {
        timeInSecs = min * 60f + sec;
    }

    public void IncreaseMin()
    {
        if (!CanEditMatchSettings())
            return;

        SetMatchTime(min + 1, sec);
    }

    public void DecreaseMin()
    {
        if (!CanEditMatchSettings())
            return;

        SetMatchTime(min - 1, sec);
    }

    public void IncreaseSec()
    {
        if (!CanEditMatchSettings())
            return;

        SetMatchTime(min, sec + 5);
    }

    public void DecreaseSec()
    {
        if (!CanEditMatchSettings())
            return;

        SetMatchTime(min, sec - 5);
    }

    public void ChangeTrailLength()
    {
        if (gameSettings == null || !CanEditMatchSettings())
            return;

        trailLength++;
        if (trailLength > gameSettings.GetMaxTrailLength())
            trailLength = gameSettings.GetMinTrailLength();

        gameSettings.trailLength = trailLength;
        ShowTrailLength();
        MarkLobbyStateDirty();
    }

    public void SetTrailColor(int value)
    {
        SetPlayerTrailColorFromPaletteIndex(value);
    }

    public void ChangePlayerModelUp()
    {
        if (motorPreview == null || motorPreview.Length == 0)
            return;

        currentModel++;
        if (currentModel >= motorPreview.Length)
            currentModel = 0;

        SetPlayerModel(currentModel);
    }

    public void ChangePlayerModelDown()
    {
        if (motorPreview == null || motorPreview.Length == 0)
            return;

        currentModel--;
        if (currentModel < 0)
            currentModel = motorPreview.Length - 1;

        SetPlayerModel(currentModel);
    }

    public void SetPlayerModel(int selectedMotor)
    {
        if (motorPreview == null)
            return;

        for (int i = 0; i < motorPreview.Length; i++)
        {
            if (motorPreview[i] != null)
                motorPreview[i].SetActive(i == selectedMotor);
        }
    }

    private void SetMatchTime(int newMin, int newSec)
    {
        int minTotalSeconds = minmin * 60;
        int maxTotalSeconds = maxmin * 60;
        int totalSeconds = newMin * 60 + newSec;

        if (totalSeconds > maxTotalSeconds)
            totalSeconds = minTotalSeconds;
        else if (totalSeconds < minTotalSeconds)
            totalSeconds = maxTotalSeconds;

        min = totalSeconds / 60;
        sec = totalSeconds % 60;
        UpdateTimeInSeconds();
        ShowGameTime();
        MarkLobbyStateDirty();
    }

    private void ShowTrailLength()
    {
        if (trailLengthText == null)
            return;

        switch (trailLength)
        {
            case 0:
                trailLengthText.text = "Short";
                break;
            case 1:
                trailLengthText.text = "Medium";
                break;
            case 2:
                trailLengthText.text = "Long";
                break;
            case 3:
                trailLengthText.text = "Permanent";
                break;
        }
    }

    private void InitializeTrailColorButtons()
    {
        if (_colorButtonsInitialized)
            return;

        if (trailColorButtons == null)
            return;

        for (int i = 0; i < trailColorButtons.Length; i++)
            trailColorButtons[i]?.Initialize();

        _colorButtonsInitialized = true;
    }

    private void RefreshTrailColorSelection()
    {
        InitializeTrailColorButtons();

        if (trailColorButtons == null)
            return;

        for (int i = 0; i < trailColorButtons.Length; i++)
        {
            bool takenByOther = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() &&
                MultiplayerSessionDriver.IsTrailColorTakenByOtherLocalPlayer(i);

            trailColorButtons[i]?.ApplyState(i == trailColor, takenByOther, CanSelectTrailColor());
        }
    }

    private void HandleLobbyStateChanged()
    {
        if (!IsReadOnlyMultiplayerClient())
            return;

        ApplySyncedLobbySettings();
        RefreshSlots();
    }

    private bool TryApplySyncedLobbyStateToSlots()
    {
        if (!IsReadOnlyMultiplayerClient() || !MultiplayerSessionDriver.TryGetLobbyState(out MultiplayerSessionDriver.LobbyStateSnapshot snapshot))
            return false;

        ApplySyncedLobbySettings(snapshot);
        _botCount = CountBots(snapshot);
        RefreshBotCountUI();

        for (int i = 0; i < lobbySlots.Length; i++)
        {
            bool isHuman = i < snapshot.HumanPlayers;
            bool isBot = !isHuman && snapshot.IsBotSlotOccupied(i);
            string text = GetSlotLabel(i, isHuman, isBot);
            lobbySlots[i]?.ApplyState(text, isHuman, isBot, false);
        }

        _lobbyStateDirty = false;
        return true;
    }

    private void ApplySyncedLobbySettings()
    {
        if (MultiplayerSessionDriver.TryGetLobbyState(out MultiplayerSessionDriver.LobbyStateSnapshot snapshot))
            ApplySyncedLobbySettings(snapshot);
    }

    private void ApplySyncedLobbySettings(MultiplayerSessionDriver.LobbyStateSnapshot snapshot)
    {
        int totalSeconds = Mathf.RoundToInt(snapshot.MatchDuration);
        min = Mathf.Max(0, totalSeconds / 60);
        sec = Mathf.Max(0, totalSeconds % 60);
        timeInSecs = Mathf.Max(0f, snapshot.MatchDuration);
        ShowGameTime();

        trailLength = snapshot.TrailLength;
        ShowTrailLength();

        suddenDeath = snapshot.SuddenDeath;
        if (suddenDeathToggle != null)
            suddenDeathToggle.isOn = suddenDeath;

        if (dropdown != null && dropdown.options.Count > 0)
        {
            dropdown.SetValueWithoutNotify(Mathf.Clamp(snapshot.GameModeIndex, 0, dropdown.options.Count - 1));
            gameMode = dropdown.options[dropdown.value].text;
        }
    }

    private void RefreshLobbyInteractivity()
    {
        bool canEdit = CanEditMatchSettings();

        if (dropdown != null)
            dropdown.interactable = canEdit;
        if (suddenDeathToggle != null)
            suddenDeathToggle.interactable = canEdit;
    }

    private void BindSettingsEvents()
    {
        if (_settingsEventsBound)
            return;

        if (dropdown != null)
            dropdown.onValueChanged.AddListener(HandleGameModeChanged);
        if (suddenDeathToggle != null)
            suddenDeathToggle.onValueChanged.AddListener(HandleSuddenDeathChanged);

        _settingsEventsBound = true;
    }

    private void UnbindSettingsEvents()
    {
        if (!_settingsEventsBound)
            return;

        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(HandleGameModeChanged);
        if (suddenDeathToggle != null)
            suddenDeathToggle.onValueChanged.RemoveListener(HandleSuddenDeathChanged);

        _settingsEventsBound = false;
    }

    private void HandleGameModeChanged(int _)
    {
        if (!CanEditMatchSettings())
            return;

        gameMode = dropdown != null && dropdown.options.Count > dropdown.value
            ? dropdown.options[dropdown.value].text
            : string.Empty;
        PlayUiClickSound();
        MarkLobbyStateDirty();
    }

    private void HandleSuddenDeathChanged(bool value)
    {
        if (!CanEditMatchSettings())
            return;

        suddenDeath = value;
        MarkLobbyStateDirty();
    }

    private bool CanEditMatchSettings()
    {
        if (!MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return true;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && bootstrap.IsServerStarted;
    }

    private bool CanSelectTrailColor()
    {
        if (!MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return true;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && (bootstrap.IsServerStarted || bootstrap.IsClientStarted);
    }

    private bool IsReadOnlyMultiplayerClient()
    {
        if (!MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return false;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && bootstrap.IsClientStarted && !bootstrap.IsServerStarted;
    }

    private bool TryBeginBotMutation()
    {
        if (_lastBotMutationFrame == Time.frameCount)
            return false;

        _lastBotMutationFrame = Time.frameCount;
        return true;
    }

    private void PlayUiClickSound()
    {
        if (!Application.isPlaying)
            return;

        ResolveUiClickSound();
        PersistentUiAudioPlayer.PlayOneShot(uiClickSound);
    }

    private void ResolveUiClickSound()
    {
        if (uiClickSound != null)
            return;

        ButtonScript[] localFeedback = GetComponentsInChildren<ButtonScript>(true);
        for (int i = 0; i < localFeedback.Length; i++)
        {
            if (localFeedback[i] != null && localFeedback[i].ClickSound != null)
            {
                uiClickSound = localFeedback[i].ClickSound;
                return;
            }
        }

        ButtonScript[] sceneFeedback = UnityEngine.Object.FindObjectsByType<ButtonScript>(
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

    private void MarkLobbyStateDirty()
    {
        if (CanEditMatchSettings())
            _lobbyStateDirty = true;
    }

    private void PublishMultiplayerLobbyState()
    {
        if (!MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
        {
            _lobbyStateDirty = false;
            return;
        }

        if (!CanEditMatchSettings())
        {
            _lobbyStateDirty = false;
            return;
        }

        if (MultiplayerSessionDriver.Instance == null)
            return;

        MultiplayerSessionDriver.PublishHostLobbyState(
            GetHumanSlotCount(),
            lobbySlots != null ? lobbySlots.Length : 0,
            GetBotSlotMask(),
            trailLength,
            timeInSecs,
            GetGameModeIndex(),
            suddenDeathToggle != null ? suddenDeathToggle.isOn : suddenDeath);
        _lobbyStateDirty = false;
    }

    private int GetBotSlotMask()
    {
        if (_botSlots == null)
            return 0;

        int mask = 0;
        int limit = Mathf.Min(_botSlots.Length, 31);
        for (int i = 0; i < limit; i++)
        {
            if (_botSlots[i])
                mask |= 1 << i;
        }

        return mask;
    }

    private int GetGameModeIndex()
    {
        return dropdown != null ? dropdown.value : 0;
    }

    private int CountBots(MultiplayerSessionDriver.LobbyStateSnapshot snapshot)
    {
        int count = 0;
        int limit = Mathf.Min(snapshot.SlotCount, lobbySlots != null ? lobbySlots.Length : snapshot.SlotCount);
        for (int i = snapshot.HumanPlayers; i < limit; i++)
        {
            if (snapshot.IsBotSlotOccupied(i))
                count++;
        }

        return count;
    }

    private string GetSlotLabel(int slotIndex, bool isHuman, bool isBot)
    {
        if (isHuman)
            return slotIndex == 0 ? "HOST" : $"PLAYER {slotIndex + 1}";

        return isBot ? "BOT" : string.Empty;
    }

    private void ValidateSceneReferences()
    {
        ValidateReference(playerHeading, nameof(playerHeading));

        if (trailColorButtons == null || trailColorButtons.Length == 0)
            Debug.LogError($"{nameof(MatchLobbyController)} on {name} has no trail color buttons assigned.", this);
        else
        {
            for (int i = 0; i < trailColorButtons.Length; i++)
                trailColorButtons[i]?.Validate(this, i);
        }

        if (lobbySlots == null || lobbySlots.Length == 0)
            Debug.LogError($"{nameof(MatchLobbyController)} on {name} has no lobby slots assigned.", this);
        else
        {
            for (int i = 0; i < lobbySlots.Length; i++)
                lobbySlots[i]?.Validate(this, i);
        }
    }

    private void ValidateReference(Object reference, string fieldName)
    {
        if (reference == null)
            Debug.LogError($"{nameof(MatchLobbyController)} on {name} is missing scene reference '{fieldName}'.", this);
    }
}
