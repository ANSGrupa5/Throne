using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MultiplayerMenuButtons : MonoBehaviour
{
    public static MultiplayerMenuButtons ActiveInstance { get; private set; }

    [Header("Optional: assign scene UI panels to avoid GameObject.Find")]
    public GameObject ConnectionTypePanel;
    public GameObject HostPanel;
    public GameObject JoinPanel;
    public TMP_InputField JoinAddressInput;
    public TMP_Text StatusText;
    public GameObject PopupPanel;
    public TMP_Text PopupText;

    [Header("Join")]
    [SerializeField] private string defaultJoinAddress = "127.0.0.1";

    private const float ButtonWidth = 220f;
    private const float ButtonHeight = 56f;
    private const float FieldWidth = 260f;
    private const float FieldHeight = 42f;
    private bool _subscribedToBootstrap;
    private SingleplayerLobby _lobby;
    private Transform[] _playerSlots;
    private TMP_Text[] _slotLabels;
    private bool _slotButtonsBound;

    private void Awake()
    {
        ActiveInstance = this;
        _lobby = FindFirstObjectByType<SingleplayerLobby>(FindObjectsInactive.Include);
        EnsureJoinPanel();
        EnsurePopup();
        EnsureButtonAudio();
        SetJoinPanelVisible(false);
        SetPopupVisible(false);
        RefreshSlots();
    }

    private void OnEnable()
    {
        SubscribeToBootstrap();
    }

    private void OnDisable()
    {
        UnsubscribeFromBootstrap();
    }

    private void OnDestroy()
    {
        if (ActiveInstance == this)
            ActiveInstance = null;
    }

    public void HostGame()
    {
        if (ConnectionTypePanel != null)
            ConnectionTypePanel.SetActive(false);
        if (HostPanel != null)
            HostPanel.SetActive(true);
        SetJoinPanelVisible(false);

        MultiplayerRuntimeBootstrap.Instance?.HostGame();
        RefreshLobbyStatus();
    }

    public void JoinGame()
    {
        if (ConnectionTypePanel != null)
            ConnectionTypePanel.SetActive(false);
        if (HostPanel != null)
            HostPanel.SetActive(false);
        EnsureJoinPanel();
        SetJoinPanelVisible(true);

        if (JoinAddressInput != null && string.IsNullOrWhiteSpace(JoinAddressInput.text))
            JoinAddressInput.text = defaultJoinAddress;

        SetStatus("Enter host address");
    }

    public void ConfirmJoinGame()
    {
        string address = JoinAddressInput != null ? JoinAddressInput.text : defaultJoinAddress;
        bool started = MultiplayerRuntimeBootstrap.Instance != null &&
            MultiplayerRuntimeBootstrap.Instance.JoinGame(address);

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
        if (HostPanel != null)
            HostPanel.SetActive(false);
        if (ConnectionTypePanel != null)
            ConnectionTypePanel.SetActive(true);
        SetStatus(string.Empty);
    }

    public void BackToMainMenu()
    {
        MultiplayerRuntimeBootstrap.Instance?.BackToMainMenu();
    }

    private void EnsureJoinPanel()
    {
        if (JoinPanel != null && JoinAddressInput != null)
            return;

        Transform canvasTransform = GetComponentInParent<Canvas>()?.transform;
        if (canvasTransform == null)
            return;

        ButtonScript styleSource = FindBestButtonStyleSource();
        FontStyles labelStyle = FontStyles.UpperCase;

        JoinPanel = CreateUiObject("JoinPanel", canvasTransform, new Vector2(0f, 0f), new Vector2(320f, 220f));

        TMP_Text title = CreateText("JoinTitle", JoinPanel.transform, "JOIN GAME", new Vector2(0f, 74f), new Vector2(300f, 40f), 30f, TextAlignmentOptions.Center);
        title.fontStyle = labelStyle;

        JoinAddressInput = CreateInput("AddressInput", JoinPanel.transform, defaultJoinAddress, new Vector2(0f, 20f));
        JoinAddressInput.onSubmit.AddListener(_ => ConfirmJoinGame());

        CreateButton("ConfirmJoinButton", JoinPanel.transform, "JOIN", new Vector2(0f, -44f), ConfirmJoinGame, styleSource);
        CreateButton("BackJoinButton", JoinPanel.transform, "BACK", new Vector2(0f, -108f), BackToConnectionType, styleSource);

        if (StatusText == null)
            StatusText = CreateText("NetworkStatus", canvasTransform, string.Empty, new Vector2(0f, -250f), new Vector2(420f, 34f), 20f, TextAlignmentOptions.Center);
    }

    private void EnsurePopup()
    {
        if (PopupPanel != null && PopupText != null)
            return;

        Transform canvasTransform = GetComponentInParent<Canvas>()?.transform;
        if (canvasTransform == null)
            return;

        PopupPanel = CreateUiObject("JoinPopup", canvasTransform, Vector2.zero, new Vector2(420f, 170f));
        Image background = PopupPanel.AddComponent<Image>();
        background.color = new Color(0f, 0.06f, 0.08f, 0.92f);

        PopupText = CreateText("PopupText", PopupPanel.transform, string.Empty, new Vector2(0f, 34f), new Vector2(380f, 62f), 22f, TextAlignmentOptions.Center);
        CreateButton("PopupOkButton", PopupPanel.transform, "OK", new Vector2(0f, -48f), () => SetPopupVisible(false), FindBestButtonStyleSource());
    }

    private GameObject CreateUiObject(string objectName, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private TMP_InputField CreateInput(string objectName, Transform parent, string text, Vector2 position)
    {
        GameObject go = CreateUiObject(objectName, parent, position, new Vector2(FieldWidth, FieldHeight));
        Image image = go.AddComponent<Image>();
        image.color = new Color(0f, 0.996f, 0.925f, 0.22f);

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        TMP_Text textComponent = CreateText("Text", go.transform, text, Vector2.zero, new Vector2(FieldWidth - 24f, FieldHeight), 22f, TextAlignmentOptions.Center);
        TMP_Text placeholder = CreateText("Placeholder", go.transform, "HOST ADDRESS", Vector2.zero, new Vector2(FieldWidth - 24f, FieldHeight), 18f, TextAlignmentOptions.Center);
        placeholder.color = new Color(1f, 1f, 1f, 0.42f);

        input.textComponent = textComponent;
        input.placeholder = placeholder;
        input.text = text;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0f, 0.996f, 0.925f, 0.35f);
        return input;
    }

    private Button CreateButton(string objectName, Transform parent, string text, Vector2 position, UnityAction action, ButtonScript styleSource)
    {
        GameObject go = CreateUiObject(objectName, parent, position, new Vector2(ButtonWidth, ButtonHeight));
        Image image = go.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>("_Project/_UI/Main Menu/button");

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        ButtonScript buttonScript = go.AddComponent<ButtonScript>();
        Image sourceImage = styleSource != null ? styleSource.GetComponent<Image>() : null;
        if (styleSource != null)
        {
            buttonScript.hoverSound = styleSource.hoverSound;
            buttonScript.clickSound = styleSource.clickSound;
            buttonScript.hoverCooldownSeconds = styleSource.hoverCooldownSeconds;
            buttonScript.audioFadeDuration = styleSource.audioFadeDuration;
            buttonScript.normalColor = styleSource.normalColor;
            buttonScript.hoverColor = styleSource.hoverColor;
            buttonScript.colorFadeDuration = styleSource.colorFadeDuration;

            if (sourceImage != null)
            {
                image.sprite = sourceImage.sprite;
                image.type = sourceImage.type;
                image.material = sourceImage.material;
                image.preserveAspect = sourceImage.preserveAspect;
                image.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
            }
        }
        else
        {
            buttonScript.normalColor = new Color(0f, 0.996f, 0.925f, 1f);
            buttonScript.hoverColor = new Color(0.03f, 0.68f, 0.64f, 1f);
        }

        image.color = buttonScript.normalColor;

        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        EventTrigger trigger = go.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerEnter, buttonScript.OnHoverEnter);
        AddTrigger(trigger, EventTriggerType.PointerExit, buttonScript.OnHoverExit);
        AddTrigger(trigger, EventTriggerType.PointerClick, buttonScript.OnClick);

        TMP_Text label = CreateText("Label", go.transform, text, Vector2.zero, new Vector2(ButtonWidth, ButtonHeight), 24f, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.UpperCase;
        return button;
    }

    private TMP_Text CreateText(string objectName, Transform parent, string text, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUiObject(objectName, parent, position, size);
        TMP_Text label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction action)
    {
        EventTrigger.Entry entry = new();
        entry.eventID = type;
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private void SetJoinPanelVisible(bool visible)
    {
        if (JoinPanel != null)
            JoinPanel.SetActive(visible);
    }

    private void SetStatus(string message)
    {
        if (StatusText != null)
            StatusText.text = message;
    }

    private string NormalizeAddress(string address)
    {
        return string.IsNullOrWhiteSpace(address) ? defaultJoinAddress : address.Trim();
    }

    private ButtonScript FindBestButtonStyleSource()
    {
        ButtonScript[] candidates = FindObjectsByType<ButtonScript>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        ButtonScript fallback = null;

        for (int i = 0; i < candidates.Length; i++)
        {
            ButtonScript candidate = candidates[i];
            if (candidate == null || candidate.GetComponent<Image>() == null)
                continue;

            fallback ??= candidate;
            Color color = candidate.normalColor;
            if (color.g > 0.7f && color.b > 0.7f && color.r < 0.2f)
                return candidate;
        }

        return fallback;
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
            SetJoinPanelVisible(false);
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
        if (MultiplayerRuntimeBootstrap.Instance == null || !MultiplayerRuntimeBootstrap.Instance.IsServerStarted)
            return;

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

    public void RefreshSlots()
    {
        if (_lobby == null)
            _lobby = FindFirstObjectByType<SingleplayerLobby>(FindObjectsInactive.Include);

        if (_lobby != null)
        {
            _lobby.RefreshSlots();
            return;
        }

        CachePlayerSlots();
        BindSlotButtons();

        if (_playerSlots == null || _slotLabels == null)
            return;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        int humanPlayers = bootstrap != null && bootstrap.IsServerStarted
            ? Mathf.Max(1, bootstrap.ConnectedPlayerCount)
            : 1;
        int botCount = _lobby != null ? _lobby.BotCount : 0;

        for (int i = 0; i < _playerSlots.Length; i++)
        {
            Transform slot = _playerSlots[i];
            TMP_Text label = _slotLabels[i];
            if (slot == null || label == null)
                continue;

            bool isHuman = i < humanPlayers;
            bool isBot = !isHuman && i < humanPlayers + botCount;
            string text = isHuman ? (i == 0 ? "HOST" : $"PLAYER {i + 1}") : (isBot ? $"BOT {i - humanPlayers + 1}" : "EMPTY");

            label.text = text;
            label.gameObject.SetActive(true);
            SetSlotButtons(slot, isHuman, isBot);
        }
    }

    private void SetSlotButtons(Transform slot, bool isHuman, bool isBot)
    {
        Transform addButton = slot.Find("AddBotButton");
        Transform removeButton = slot.Find("RemoveBotButton");

        if (isHuman)
        {
            if (addButton != null)
                addButton.gameObject.SetActive(false);
            if (removeButton != null)
                removeButton.gameObject.SetActive(false);
            return;
        }

        if (isBot)
        {
            if (addButton != null)
                addButton.gameObject.SetActive(false);
            if (removeButton != null)
                removeButton.gameObject.SetActive(true);
            return;
        }

        if (addButton != null)
            addButton.gameObject.SetActive(true);
        if (removeButton != null)
            removeButton.gameObject.SetActive(false);
    }

    private void BindSlotButtons()
    {
        if (_slotButtonsBound || _playerSlots == null)
            return;

        for (int i = 0; i < _playerSlots.Length; i++)
        {
            Transform slot = _playerSlots[i];
            BindSlotButton(slot.Find("AddBotButton"), true);
            BindSlotButton(slot.Find("RemoveBotButton"), false);
            MakeSlotButtonInvisible(slot.Find("AddBotButton"));
            MakeSlotButtonInvisible(slot.Find("RemoveBotButton"));
        }

        _slotButtonsBound = true;
    }

    private void BindSlotButton(Transform buttonTransform, bool addBot)
    {
        if (buttonTransform == null || !buttonTransform.TryGetComponent(out Button button))
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (_lobby == null)
                _lobby = FindFirstObjectByType<SingleplayerLobby>(FindObjectsInactive.Include);

            if (_lobby == null)
                return;

            if (addBot)
                _lobby.AddBot();
            else
                _lobby.RemoveBot();

            RefreshSlots();
        });
    }

    private void MakeSlotButtonInvisible(Transform buttonTransform)
    {
        if (buttonTransform == null || !buttonTransform.TryGetComponent(out Image image))
            return;

        Color color = image.color;
        color.a = 0f;
        image.color = color;
    }

    private void CachePlayerSlots()
    {
        if (_playerSlots != null && _playerSlots.Length > 0)
            return;

        _playerSlots = FindObjectsByType<SwitchState>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(switchState => switchState.transform)
            .Where(transform => transform.name.StartsWith("PlayerSlot"))
            .OrderBy(GetSlotSortIndex)
            .ToArray();

        _slotLabels = new TMP_Text[_playerSlots.Length];
        for (int i = 0; i < _playerSlots.Length; i++)
            _slotLabels[i] = GetOrCreateSlotLabel(_playerSlots[i]);
    }

    private int GetSlotSortIndex(Transform slot)
    {
        if (slot == null)
            return int.MaxValue;

        string name = slot.name;
        if (name == "PlayerSlot")
            return 0;

        int underscore = name.LastIndexOf('_');
        if (underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out int index))
            return index;

        return int.MaxValue;
    }

    private TMP_Text GetOrCreateSlotLabel(Transform slot)
    {
        Transform existing = slot.Find("SlotStatusText");
        if (existing != null && existing.TryGetComponent(out TMP_Text existingLabel))
            return existingLabel;

        TMP_Text label = CreateText("SlotStatusText", slot, string.Empty, Vector2.zero, new Vector2(92f, 42f), 13f, TextAlignmentOptions.Center);
        label.color = new Color(0f, 0.996f, 0.925f, 1f);
        label.fontStyle = FontStyles.UpperCase;
        return label;
    }

    private void ShowPopup(string message)
    {
        EnsurePopup();
        if (PopupText != null)
            PopupText.text = message;

        SetPopupVisible(true);
    }

    private void SetPopupVisible(bool visible)
    {
        if (PopupPanel != null)
            PopupPanel.SetActive(visible);
    }

    private void EnsureButtonAudio()
    {
        ButtonScript styleSource = FindBestButtonStyleSource();
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            GameObject buttonObject = button.gameObject;
            if (!buttonObject.TryGetComponent(out AudioSource audioSource))
                audioSource = buttonObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            ButtonScript buttonScript = buttonObject.GetComponent<ButtonScript>();
            bool addedScript = buttonScript == null;
            if (addedScript)
                buttonScript = buttonObject.AddComponent<ButtonScript>();

            if (styleSource != null && buttonScript != styleSource)
            {
                buttonScript.hoverSound = styleSource.hoverSound;
                buttonScript.clickSound = styleSource.clickSound;
                buttonScript.hoverCooldownSeconds = styleSource.hoverCooldownSeconds;
                buttonScript.audioFadeDuration = styleSource.audioFadeDuration;
                buttonScript.colorFadeDuration = styleSource.colorFadeDuration;

                if (addedScript && buttonObject.TryGetComponent(out Image image) && image.color.a <= 0.01f)
                {
                    buttonScript.normalColor = new Color(image.color.r, image.color.g, image.color.b, 0f);
                    buttonScript.hoverColor = new Color(image.color.r, image.color.g, image.color.b, 0f);
                }
                else if (addedScript)
                {
                    buttonScript.normalColor = styleSource.normalColor;
                    buttonScript.hoverColor = styleSource.hoverColor;
                }
            }

            EventTrigger trigger = buttonObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = buttonObject.AddComponent<EventTrigger>();

            AddTriggerIfMissing(trigger, EventTriggerType.PointerEnter, buttonScript.OnHoverEnter);
            AddTriggerIfMissing(trigger, EventTriggerType.PointerExit, buttonScript.OnHoverExit);
            AddTriggerIfMissing(trigger, EventTriggerType.PointerClick, buttonScript.OnClick);
        }
    }

    private void AddTriggerIfMissing(EventTrigger trigger, EventTriggerType type, UnityAction action)
    {
        if (trigger.triggers.Any(entry => entry.eventID == type))
            return;

        AddTrigger(trigger, type, action);
    }
}
