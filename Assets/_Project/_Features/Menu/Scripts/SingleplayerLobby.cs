using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SingleplayerLobby : MonoBehaviour
{
    private const float LobbySlotSizeMultiplier = 1.12f;
    private static readonly Vector2 SlotLabelSize = new Vector2(112f, 52f);

    private static readonly string[] ColorButtonNames =
    {
        "RedColorButton",
        "BlueColorButton",
        "GreenColorButton",
        "YellowColorButton",
        "MagentaColorButton",
        "CyanColorButton"
    };

    private static readonly string[] SingleClickAudioButtonNames =
    {
        "RedColorButton",
        "BlueColorButton",
        "GreenColorButton",
        "YellowColorButton",
        "MagentaColorButton",
        "CyanColorButton",
        "MinDownButton",
        "MinUpButton",
        "SecDownButton",
        "SecUpButton"
    };

    [Header("Default Config Assets")]
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private BotsSettings botsSettings;
    [SerializeField] private PlayerLook playerLook;

    [Header("Bots")]
    [SerializeField] private TMP_Text botCountText;
    [Header("Player Color")]
    [SerializeField] private Color playerTrailColor = Color.white;

    [Header("Settings UI")]
    [SerializeField] private TMP_Text minutes;
    [SerializeField] private TMP_Text seconds;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Toggle suddenDeathToggle;


    [Header("Trail Length")]
    [SerializeField] private TMP_Text trailLengthText;

    [Header("Vechicle Previews")]
    [SerializeField] private int currentModel;
    [SerializeField] private GameObject[] motorPreview;
    [SerializeField] private GameObject[] motorPlayable;

    private int _botCount;
    private int min, sec, maxmin, minmin;
    private float timeInSecs;
    private string arenaSceneName = "Neon City XL"; // Default arena, can be overridden by GameSettings
    [SerializeField] private string multiplayerArenaSceneName = "multi Neon City XL";
    private int trailLength;
    private int trailColor;
    private string gameMode;
    private bool suddenDeath;
    private Image[] _colorButtonImages;
    private Button[] _colorButtons;
    private Outline[] _colorButtonOutlines;
    private Transform[] _lobbySlots;
    private TMP_Text[] _slotLabels;
    private bool[] _botSlots;
    private bool _slotButtonsBound;
    private bool _lobbySlotsSized;
    //private int trailLength; // ToDo: make UI for that
    public int BotCount => _botCount;

    private void Awake()
    {
        if (gameSettings != null)
            arenaSceneName = gameSettings.arenaSceneName;

        if (playerLook != null)
            playerTrailColor = playerLook.trailColor;

        ApplyPlayerTrailColor();
        CacheColorButtons();

        // Initialize bot count to 0, allowing the player to add them from scratch.
        // The previous implementation loaded a default value from an asset, which was confusing.
        _botCount = 0;
        RefreshBotCountUI(); // This can be modified later to update your visual "plus" icons.
        RefreshPlayerHeading();
        CacheLobbySlots();
        BindLobbySlotButtons();
        RefreshSlots();

        min = 1; //curently selected minutes
        sec = 0; //currently selected seconds
        maxmin = 10; //max selectable minutes
        minmin = 1; //min selectable minutes
        timeInSecs = 60f;
        ShowGameTime();

        trailLength = gameSettings.trailLength;
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

        currentModel = 0;
        motorPreview[0].SetActive(true);
        RefreshTrailColorSelection();
        PublishMultiplayerTrailColorSelection();
    }

    private void OnEnable()
    {
        MultiplayerSessionDriver.TrailColorSelectionsChanged += HandleTrailColorSelectionsChanged;
    }

    private void OnDisable()
    {
        MultiplayerSessionDriver.TrailColorSelectionsChanged -= HandleTrailColorSelectionsChanged;
    }

    private void Start()
    {
        ConfigureSingleClickAudioButtons();
    }

    public void LoadScene(string sceneName)
    {
        arenaSceneName = sceneName;
        LoadConfiguredScene(sceneName);
    }
    public void LoadScene()
    {
        LoadConfiguredScene(gameSettings.arenaSceneName);
    }

    private void LoadConfiguredScene(string sceneName)
    {
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
            SceneManager.LoadScene(sceneName);
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

    public void GetSettingsFromUI(TMP_Dropdown dropdown, Toggle suddenDeathToggle)
    {
        gameMode = dropdown.options[dropdown.value].text;
        suddenDeath = suddenDeathToggle.isOn;
        Debug.Log("Wybrany tryb gry: " + gameMode);
        Debug.Log("Tryb Sudden Death: " + suddenDeath);
    }

    public void AddBot()
    {
        AddBotToFirstAvailableSlot();
    }

    public void RemoveBot()
    {
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
        _botCount = Mathf.Clamp(_botCount, 0, GetMaxBotCount());
        
        int humanSlots = singleplayer ? 1 : Mathf.Max(2, GetHumanSlotCount());
        gameSettings.maxPlayers = humanSlots + _botCount;
        gameSettings.matchDuration = timeInSecs;
        GetSettingsFromUI(dropdown, suddenDeathToggle);
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
        var session = GameSessionRuntime.FromDefaults(gameSettings, botsSettings, playerLook, _botCount);
        session.isSingleplayer = singleplayer;
        GameSessionBootstrap.SetSession(session);
    }

    private void SetBotCount(int value)
    {
        SetBotCountByFillingSlots(value);
        RefreshBotCountUI();
        RefreshSlots();
    }

    private int GetDefaultBotCount()
    {
        if (botsSettings == null || botsSettings.bots == null)
            return 0;
        
        // Sum the counts directly from the settings asset.
        return botsSettings.bots.Sum(bot => bot?.count ?? 0);
    }

    private int GetMaxBotCount()
    {
        int maxPlayers = 6; // Absolute max limit from GameSettings
        bool hasPlayerPrefab = playerLook != null && playerLook.playerPrefab != null;
        bool isMultiplayerLobby = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene();
        int reservedHumanSlots = isMultiplayerLobby ? Mathf.Max(1, GetHumanSlotCount()) : (hasPlayerPrefab ? 1 : 0);
        return Mathf.Max(0, maxPlayers - reservedHumanSlots);
    }

    private void RefreshBotCountUI()
    {
        if (botCountText != null)
            botCountText.text = _botCount.ToString();
    }

    public void RefreshSlots()
    {
        CacheLobbySlots();
        BindLobbySlotButtons();

        if (_lobbySlots == null || _slotLabels == null)
            return;

        int maxBotCount = GetMaxBotCount();
        int humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        SyncBotCountFromSlots(humanPlayers);

        if (_botCount > maxBotCount)
        {
            TrimBotsToMax(maxBotCount, humanPlayers);
            RefreshBotCountUI();
        }

        bool isMultiplayerLobby = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene();
        humanPlayers = isMultiplayerLobby ? GetHumanSlotCount() : 0;

        for (int i = 0; i < _lobbySlots.Length; i++)
        {
            Transform slot = _lobbySlots[i];
            TMP_Text label = _slotLabels[i];
            if (slot == null || label == null)
                continue;

            bool isHuman = i < humanPlayers;
            bool isBot = !isHuman && IsBotSlotOccupied(i);
            string text = isHuman ? (i == 0 ? "HOST" : $"PLAYER {i + 1}") : (isBot ? "BOT" : "EMPTY\n+");

            label.text = text;
            label.gameObject.SetActive(true);
            label.transform.SetAsLastSibling();
            SetSlotButtons(slot, isHuman, isBot);
            UpdateSlotFrame(slot, isHuman, isBot);
        }
    }

    private void RefreshPlayerHeading()
    {
        GameObject headingObject = GameObject.Find("PlayersText (TMP)");
        if (headingObject == null || !headingObject.TryGetComponent(out TMP_Text heading))
            return;

        heading.text = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? "Players" : "Bots";
    }

    private int GetHumanSlotCount()
    {
        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        if (bootstrap != null && bootstrap.IsServerStarted)
            return Mathf.Max(1, bootstrap.ConnectedPlayerCount);

        return 0;
    }

    private void CacheLobbySlots()
    {
        if (_lobbySlots != null && _lobbySlots.Length > 0)
            return;

        _lobbySlots = FindObjectsByType<SwitchState>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(switchState => switchState.transform)
            .Where(transform => transform.name.StartsWith("PlayerSlot"))
            .OrderBy(GetSlotSortIndex)
            .ToArray();

        _slotLabels = new TMP_Text[_lobbySlots.Length];
        _botSlots = new bool[_lobbySlots.Length];
        for (int i = 0; i < _lobbySlots.Length; i++)
        {
            ResizeLobbySlot(_lobbySlots[i]);
            EnsureSlotFrame(_lobbySlots[i]);
            _slotLabels[i] = GetOrCreateSlotLabel(_lobbySlots[i]);
        }

        _lobbySlotsSized = true;
    }

    private void ResizeLobbySlot(Transform slot)
    {
        if (_lobbySlotsSized || slot == null)
            return;

        if (slot is RectTransform rect)
        {
            Vector2 size = rect.sizeDelta;
            if (size.x > 0f && size.y > 0f)
                rect.sizeDelta = size * LobbySlotSizeMultiplier;
            else
                rect.localScale = new Vector3(LobbySlotSizeMultiplier, LobbySlotSizeMultiplier, 1f);
        }
        else
        {
            slot.localScale = new Vector3(
                slot.localScale.x * LobbySlotSizeMultiplier,
                slot.localScale.y * LobbySlotSizeMultiplier,
                slot.localScale.z);
        }
    }

    private void BindLobbySlotButtons()
    {
        if (_slotButtonsBound || _lobbySlots == null)
            return;

        for (int i = 0; i < _lobbySlots.Length; i++)
        {
            int slotIndex = i;
            Transform slot = _lobbySlots[i];
            DisableOriginalSlotButton(slot.Find("AddBotButton"));
            DisableOriginalSlotButton(slot.Find("RemoveBotButton"));
            BindSlotClickTarget(slot, slotIndex);
        }

        _slotButtonsBound = true;
    }

    private void BindSlotClickTarget(Transform slot, int slotIndex)
    {
        Button button = GetOrCreateSlotClickTarget(slot);
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => HandleSlotClicked(slotIndex));
        EnsureSlotButtonAudio(button.gameObject);
    }

    private void HandleSlotClicked(int slotIndex)
    {
        int humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        if (slotIndex < humanPlayers)
            return;

        if (_botSlots == null || slotIndex < 0 || slotIndex >= _botSlots.Length)
            return;

        _botSlots[slotIndex] = !_botSlots[slotIndex];
        SyncBotCountFromSlots(humanPlayers);
        RefreshBotCountUI();
        RefreshSlots();
    }

    private void SetSlotButtons(Transform slot, bool isHuman, bool isBot)
    {
        Transform addButton = slot.Find("AddBotButton");
        Transform removeButton = slot.Find("RemoveBotButton");

        if (addButton != null)
            addButton.gameObject.SetActive(false);
        if (removeButton != null)
            removeButton.gameObject.SetActive(false);

        Transform clickTarget = slot.Find("SlotClickTarget");
        if (clickTarget != null && clickTarget.TryGetComponent(out Button button))
            button.interactable = !isHuman;
    }

    private void DisableOriginalSlotButton(Transform buttonTransform)
    {
        if (buttonTransform == null)
            return;

        if (buttonTransform.TryGetComponent(out Button button))
            button.enabled = false;

        if (buttonTransform.TryGetComponent(out Image image))
        {
            Color color = image.color;
            color.a = 0f;
            image.color = color;
            image.raycastTarget = false;
        }

        EventTrigger trigger = buttonTransform.GetComponent<EventTrigger>();
        if (trigger != null)
            trigger.enabled = false;
    }

    private void EnsureSlotFrame(Transform slot)
    {
        if (slot == null)
            return;

        Transform existing = slot.Find("SlotFrame");
        if (existing != null)
        {
            ConfigureSlotFrame(existing);
            return;
        }

        GameObject frameObject = new("SlotFrame", typeof(RectTransform));
        frameObject.transform.SetParent(slot, false);
        frameObject.transform.SetAsFirstSibling();
        ConfigureSlotFrame(frameObject.transform);
    }

    private void ConfigureSlotFrame(Transform frame)
    {
        if (frame == null)
            return;

        frame.SetAsFirstSibling();
        RectTransform rect = frame.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(3f, 3f);
        rect.offsetMax = new Vector2(-3f, -3f);

        Image image = frame.GetComponent<Image>();
        if (image == null)
            image = frame.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0.996f, 0.925f, 0.08f);
        image.raycastTarget = false;

        Outline outline = frame.GetComponent<Outline>();
        if (outline == null)
            outline = frame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.996f, 0.925f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private void UpdateSlotFrame(Transform slot, bool isHuman, bool isBot)
    {
        Transform frame = slot != null ? slot.Find("SlotFrame") : null;
        if (frame == null)
            return;

        Image image = frame.GetComponent<Image>();
        Outline outline = frame.GetComponent<Outline>();

        Color fill = isHuman
            ? new Color(0.18f, 0.52f, 1f, 0.22f)
            : isBot
                ? new Color(0.55f, 1f, 0.24f, 0.24f)
                : new Color(0f, 0.996f, 0.925f, 0.07f);
        Color border = isHuman
            ? new Color(0.25f, 0.62f, 1f, 0.95f)
            : isBot
                ? new Color(0.78f, 1f, 0.25f, 0.95f)
                : new Color(0f, 0.996f, 0.925f, 0.65f);

        if (image != null)
            image.color = fill;
        if (outline != null)
            outline.effectColor = border;
    }

    private Button GetOrCreateSlotClickTarget(Transform slot)
    {
        if (slot == null)
            return null;

        Transform existing = slot.Find("SlotClickTarget");
        if (existing != null && existing.TryGetComponent(out Button existingButton))
            return existingButton;

        GameObject targetObject = new("SlotClickTarget", typeof(RectTransform));
        targetObject.transform.SetParent(slot, false);
        targetObject.transform.SetAsLastSibling();

        RectTransform rect = targetObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = targetObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        Button button = targetObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0f);
        button.colors = colors;
        return button;
    }

    private int GetSlotSortIndex(Transform slot)
    {
        if (slot == null)
            return int.MaxValue;

        string slotName = slot.name;
        if (slotName == "PlayerSlot")
            return 0;

        int underscore = slotName.LastIndexOf('_');
        if (underscore >= 0 && int.TryParse(slotName.Substring(underscore + 1), out int index))
            return index;

        return int.MaxValue;
    }

    private TMP_Text GetOrCreateSlotLabel(Transform slot)
    {
        Transform existing = slot.Find("SlotStatusText");
        if (existing != null && existing.TryGetComponent(out TMP_Text existingLabel))
        {
            ConfigureSlotLabel(existingLabel);
            return existingLabel;
        }

        GameObject labelObject = new("SlotStatusText", typeof(RectTransform));
        labelObject.transform.SetParent(slot, false);

        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        ConfigureSlotLabel(label);
        return label;
    }

    private void ConfigureSlotLabel(TMP_Text label)
    {
        if (label == null)
            return;

        RectTransform rect = label.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = SlotLabelSize;

        label.text = string.Empty;
        label.fontSize = 14f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0f, 0.996f, 0.925f, 1f);
        label.fontStyle = FontStyles.UpperCase;
        label.raycastTarget = false;
    }

    private void EnsureSlotButtonAudio(GameObject buttonObject)
    {
        if (buttonObject == null)
            return;

        if (!buttonObject.TryGetComponent(out AudioSource audioSource))
            audioSource = buttonObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        ButtonScript buttonScript = buttonObject.GetComponent<ButtonScript>();
        if (buttonScript == null)
            buttonScript = buttonObject.AddComponent<ButtonScript>();

        ButtonScript styleSource = FindObjectsByType<ButtonScript>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && candidate != buttonScript && candidate.clickSound != null);
        if (styleSource != null)
        {
            buttonScript.hoverSound = styleSource.hoverSound;
            buttonScript.clickSound = styleSource.clickSound;
            buttonScript.hoverCooldownSeconds = styleSource.hoverCooldownSeconds;
            buttonScript.audioFadeDuration = styleSource.audioFadeDuration;
            buttonScript.colorFadeDuration = styleSource.colorFadeDuration;
        }

        buttonScript.normalColor = new Color(1f, 1f, 1f, 0f);
        buttonScript.hoverColor = new Color(1f, 1f, 1f, 0f);

        EventTrigger trigger = buttonObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = buttonObject.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, buttonScript.OnHoverEnter);
            AddTrigger(trigger, EventTriggerType.PointerExit, buttonScript.OnHoverExit);
            AddTrigger(trigger, EventTriggerType.PointerClick, buttonScript.OnClick);
        }
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new();
        entry.eventID = type;
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private void ConfigureSingleClickAudioButtons()
    {
        for (int i = 0; i < SingleClickAudioButtonNames.Length; i++)
        {
            GameObject buttonObject = GameObject.Find(SingleClickAudioButtonNames[i]);
            if (buttonObject == null ||
                !buttonObject.TryGetComponent(out Button button) ||
                !buttonObject.TryGetComponent(out ButtonScript buttonScript))
            {
                continue;
            }

            EventTrigger trigger = buttonObject.GetComponent<EventTrigger>();
            if (trigger != null)
                trigger.triggers.RemoveAll(entry => entry.eventID == EventTriggerType.PointerClick);

            button.onClick.RemoveListener(buttonScript.OnClick);
        }
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

    private void SetBotCountByFillingSlots(int value)
    {
        CacheLobbySlots();
        if (_botSlots == null)
        {
            _botCount = Mathf.Clamp(value, 0, GetMaxBotCount());
            return;
        }

        int humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        int target = Mathf.Clamp(value, 0, GetMaxBotCount());
        for (int i = humanPlayers; i < _botSlots.Length; i++)
            _botSlots[i] = false;

        int added = 0;
        for (int i = humanPlayers; i < _botSlots.Length && added < target; i++)
        {
            _botSlots[i] = true;
            added++;
        }

        _botCount = added;
    }

    private void AddBotToFirstAvailableSlot()
    {
        CacheLobbySlots();
        int humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        if (_botSlots == null)
            return;

        for (int i = humanPlayers; i < _botSlots.Length; i++)
        {
            if (_botSlots[i])
                continue;

            _botSlots[i] = true;
            SyncBotCountFromSlots(humanPlayers);
            RefreshBotCountUI();
            RefreshSlots();
            return;
        }
    }

    private void RemoveBotFromLastOccupiedSlot()
    {
        CacheLobbySlots();
        int humanPlayers = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() ? GetHumanSlotCount() : 0;
        if (_botSlots == null)
            return;

        for (int i = _botSlots.Length - 1; i >= humanPlayers; i--)
        {
            if (!_botSlots[i])
                continue;

            _botSlots[i] = false;
            SyncBotCountFromSlots(humanPlayers);
            RefreshBotCountUI();
            RefreshSlots();
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

    public void isSuddenDeath()
    {
        suddenDeath = !suddenDeath;
    }

    public void tempLog()
    {
        Debug.Log("Powinno dodać się " + _botCount + " botów");
        Debug.Log("Tryb Sudden death: " + suddenDeath);
        Debug.Log("Czas trwania meczu w sekundach: " + timeInSecs);
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
        timeInSecs = (float)(min * 60) + (float)sec;
    }

    public void IncreaseMin()
    {
        SetMatchTime(min + 1, sec);
    }

    public void DecreaseMin()
    {
        SetMatchTime(min - 1, sec);
    }

    public void IncreaseSec()
    {
        SetMatchTime(min, sec + 5);
    }

    public void DecreaseSec()
    {
        SetMatchTime(min, sec - 5);
    }

    public void ChangeTrailLength()
    {
        trailLength++;
        if (trailLength > gameSettings.GetMaxTrailLength())
            trailLength = gameSettings.GetMinTrailLength();

        gameSettings.trailLength = trailLength;

        switch(trailLength)
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

    public void SetTrailColor(int value)
    {
        SetPlayerTrailColorFromPaletteIndex(value);
    }

    public void ChangePlayerModelUp()
    {
        currentModel++;
        if(currentModel >= motorPreview.Length)
            currentModel = 0;

        SetPlayerModel(currentModel);
    }

    public void ChangePlayerModelDown()
    {
        currentModel--;
        if (currentModel < 0)
            currentModel = motorPreview.Length-1;

        SetPlayerModel(currentModel);
    }

    public void SetPlayerModel(int selectedMotor)
    {
        for (int i = 0; i < motorPreview.Length; i++)
        {
            if (i == selectedMotor)
                motorPreview[i].SetActive(true);
            else
                motorPreview[i].SetActive(false);
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
    }

    private void CacheColorButtons()
    {
        if (_colorButtonImages != null && _colorButtonImages.Length > 0)
            return;

        _colorButtonImages = new Image[ColorButtonNames.Length];
        _colorButtons = new Button[ColorButtonNames.Length];
        _colorButtonOutlines = new Outline[ColorButtonNames.Length];

        for (int i = 0; i < ColorButtonNames.Length; i++)
        {
            GameObject colorButton = GameObject.Find(ColorButtonNames[i]);
            if (colorButton == null)
                continue;

            _colorButtonImages[i] = colorButton.GetComponent<Image>();
            _colorButtons[i] = colorButton.GetComponent<Button>();
            _colorButtonOutlines[i] = colorButton.GetComponent<Outline>();
            if (_colorButtonOutlines[i] == null)
                _colorButtonOutlines[i] = colorButton.AddComponent<Outline>();

            _colorButtonOutlines[i].effectColor = Color.white;
            _colorButtonOutlines[i].effectDistance = new Vector2(2f, 2f);
        }
    }

    private void RefreshTrailColorSelection()
    {
        CacheColorButtons();

        if (_colorButtonOutlines == null)
            return;

        for (int i = 0; i < _colorButtonOutlines.Length; i++)
        {
            bool takenByOther = MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene() &&
                MultiplayerSessionDriver.IsTrailColorTakenByOtherLocalPlayer(i);

            if (_colorButtonOutlines[i] != null)
                _colorButtonOutlines[i].enabled = i == trailColor;

            if (_colorButtons != null && i < _colorButtons.Length && _colorButtons[i] != null)
                _colorButtons[i].interactable = !takenByOther;

            if (_colorButtonImages != null && i < _colorButtonImages.Length && _colorButtonImages[i] != null)
            {
                _colorButtonImages[i].transform.localScale = Vector3.one;
                Color color = _colorButtonImages[i].color;
                color.a = takenByOther ? 0.35f : 1f;
                _colorButtonImages[i].color = color;
            }
        }
    }
}
