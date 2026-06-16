using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MultiplayerLobby : MonoBehaviour
{
    private static readonly MatchMode[] SupportedModes =
    {
        MatchMode.Deathmatch,
        MatchMode.KingOfTheHill
    };

    private const int SecondsStep = 5;
    private const int MinutesStep = 60;

    [Header("Runtime")]
    [SerializeField] private MultiplayerRuntimeBootstrap runtimeBootstrap;

    [Header("Default Config Assets")]
    [SerializeField] private MatchDefaults matchDefaults;
    [SerializeField] private MatchRules matchRules;
    [SerializeField] private TrailColorPalette trailColorPalette;
    [SerializeField] private VehiclePrefabSet networkVehiclePrefabSet;

    [Header("Panels")]
    [SerializeField] private GameObject connectionTypePanel;
    [SerializeField] private GameObject hostSettingsPanel;

    [Header("Client")]
    [SerializeField] private string defaultClientAddress = "localhost";

    [Header("Optional Buttons")]
    [SerializeField] private Button startMatchButton;

    [Header("Host Settings UI")]
    [SerializeField] private TMP_Text minutes;
    [SerializeField] private TMP_Text seconds;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Toggle suddenDeathToggle;

    [Header("Trail Length")]
    [SerializeField] private TMP_Text trailLengthText;

    [Header("Player Color")]
    [SerializeField] private Color playerTrailColor = Color.white;

    [Header("Vehicle Previews")]
    [SerializeField] private int currentModel;
    [SerializeField] private GameObject[] motorPreview;

    private bool _isHost;
    private bool _hostRequested;
    private bool _joinRequested;
    private Coroutine _hostReadyRoutine;

    private int min;
    private int sec;
    private int timeInSecs;
    private int trailLength;
    private int trailColor;
    private MatchMode selectedMatchMode = MatchMode.Deathmatch;
    private bool suddenDeath;

    private void Awake()
    {
        if (runtimeBootstrap == null)
            runtimeBootstrap = MultiplayerRuntimeBootstrap.Instance;

        ResolvePanelsIfNeeded();
        ResolveHostSettingsUiIfNeeded();

        if (matchRules == null)
            Debug.LogError("MultiplayerLobby needs MatchRules assigned to apply lobby UI bounds. Assign MatchRules.asset in the MultiplayerLobby inspector.");

        MatchSettings defaultSettings = matchDefaults != null ? matchDefaults.CreateSettings() : null;

        if (defaultSettings != null)
        {
            selectedMatchMode = IsSupportedMode(defaultSettings.MatchMode)
                ? defaultSettings.MatchMode
                : MatchMode.Deathmatch;

            suddenDeath = defaultSettings.SuddenDeathEnabled;
            trailLength = ClampTrailLength(defaultSettings.TrailLength);
            timeInSecs = NormalizeMatchDurationSeconds(defaultSettings.MatchDurationSeconds);
        }

        trailColor = 0;
        playerTrailColor = GetLobbySelectedColorOrFallback();

        SyncTimeFieldsFromSeconds();
        RefreshTimeUI();
        InitializeModeDropdown();
        RefreshTrailLengthUI();

        if (suddenDeathToggle != null)
            suddenDeathToggle.SetIsOnWithoutNotify(suddenDeath);

        currentModel = Mathf.Clamp(currentModel, 0, motorPreview != null && motorPreview.Length > 0 ? motorPreview.Length - 1 : 0);
        SetPlayerModel(currentModel);

        ShowConnectionTypePanel();
    }

    public void HostGame()
    {
        ResolvePanelsIfNeeded();
        ResolveHostSettingsUiIfNeeded();

        if (_hostRequested)
        {
            ShowHostSettingsPanel();
            SetStartMatchAvailable(runtimeBootstrap != null && runtimeBootstrap.IsHostReady);
            if (runtimeBootstrap != null && !runtimeBootstrap.IsHostReady && _hostReadyRoutine == null)
                _hostReadyRoutine = StartCoroutine(WaitForHostReadyRoutine(runtimeBootstrap));
            return;
        }

        if (!TryGetRuntime(out MultiplayerRuntimeBootstrap runtime))
            return;

        bool accepted = runtime.RequestHostGame();
        if (!accepted)
        {
            Debug.LogError("[MultiplayerLobby] Host start request failed.");
            _hostRequested = false;
            _isHost = false;
            SetStartMatchAvailable(false);
            return;
        }

        _hostRequested = true;
        _joinRequested = false;
        _isHost = true;

        ShowHostSettingsPanel();
        SetStartMatchAvailable(false);

        if (_hostReadyRoutine != null)
            StopCoroutine(_hostReadyRoutine);

        _hostReadyRoutine = StartCoroutine(WaitForHostReadyRoutine(runtime));
    }

    public void JoinGame()
    {
        ResolvePanelsIfNeeded();
        ResolveHostSettingsUiIfNeeded();

        if (_joinRequested)
            return;

        if (!TryGetRuntime(out MultiplayerRuntimeBootstrap runtime))
            return;

        _joinRequested = true;
        _hostRequested = false;
        _isHost = false;

        if (_hostReadyRoutine != null)
        {
            StopCoroutine(_hostReadyRoutine);
            _hostReadyRoutine = null;
        }

        SetActive(connectionTypePanel, false);
        SetActive(hostSettingsPanel, false);
        SetStartMatchAvailable(false);

        runtime.JoinGame(defaultClientAddress);
    }

    public void StartMatch()
    {
        ResolveHostSettingsUiIfNeeded();

        if (!_isHost)
        {
            Debug.LogWarning("Only the host can start a multiplayer match.");
            return;
        }

        if (!TryGetRuntime(out MultiplayerRuntimeBootstrap runtime))
            return;

        if (!runtime.IsHostReady)
        {
            Debug.LogWarning("[MultiplayerLobby] Cannot start match yet because host is still starting.");
            SetStartMatchAvailable(false);

            if (_hostReadyRoutine == null)
                _hostReadyRoutine = StartCoroutine(WaitForHostReadyRoutine(runtime));

            return;
        }

        if (!TryCreateSettings(out MatchSettings settings))
            return;

        runtime.StartMatch(settings, matchRules, networkVehiclePrefabSet, trailColorPalette);
    }

    public void BackToMainMenu()
    {
        if (!TryGetRuntime(out MultiplayerRuntimeBootstrap runtime))
            return;

        runtime.BackToMainMenu();
    }

    public void ShowConnectionTypePanel()
    {
        SetActive(connectionTypePanel, true);
        SetActive(hostSettingsPanel, false);
        SetStartMatchAvailable(false);
    }

    public void GetSettingsFromUI(TMP_Dropdown dropdown, Toggle suddenDeathToggle)
    {
        TMP_Dropdown modeDropdown = dropdown != null ? dropdown : this.dropdown;
        if (modeDropdown != null)
            selectedMatchMode = GetModeForDropdownValue(modeDropdown.value);

        Toggle suddenDeathSource = suddenDeathToggle != null ? suddenDeathToggle : this.suddenDeathToggle;
        if (suddenDeathSource != null)
            suddenDeath = suddenDeathSource.isOn;

        Debug.Log("Wybrany tryb gry: " + GetDisplayName(selectedMatchMode));
        Debug.Log("Tryb Sudden Death: " + suddenDeath);
    }

    public void SetPlayerTrailColor(Color color)
    {
        playerTrailColor = TrailColorPalette.SanitizeColor(color, Color.white);
        if (playerTrailColor.a <= 0.01f)
            playerTrailColor.a = 1f;
    }

    public void SetPlayerTrailColorFromPaletteIndex(int index)
    {
        if (!TryGetPaletteColor(index, out Color color))
        {
            Debug.LogError("MultiplayerLobby cannot select a trail color because TrailColorPalette is not assigned or has no colors.");
            return;
        }

        trailColor = Mathf.Clamp(index, 0, trailColorPalette.Colors.Count - 1);
        SetPlayerTrailColor(color);
    }

    public void SetTrailColor(int value)
    {
        trailColor = value;
        SetPlayerTrailColorFromPaletteIndex(value);
    }

    public void ShowGameTime()
    {
        RefreshTimeUI();
    }

    public void IncreaseMin()
    {
        ResolveHostSettingsUiIfNeeded();
        AdjustMatchDurationSeconds(MinutesStep);
        Debug.Log($"[MultiplayerLobby] IncreaseMin -> {timeInSecs}s");
    }

    public void DecreaseMin()
    {
        ResolveHostSettingsUiIfNeeded();
        AdjustMatchDurationSeconds(-MinutesStep);
        Debug.Log($"[MultiplayerLobby] DecreaseMin -> {timeInSecs}s");
    }

    public void IncreaseSec()
    {
        ResolveHostSettingsUiIfNeeded();
        AdjustMatchDurationSeconds(SecondsStep);
        Debug.Log($"[MultiplayerLobby] IncreaseSec -> {timeInSecs}s");
    }

    public void DecreaseSec()
    {
        ResolveHostSettingsUiIfNeeded();
        AdjustMatchDurationSeconds(-SecondsStep);
        Debug.Log($"[MultiplayerLobby] DecreaseSec -> {timeInSecs}s");
    }

    public void ChangeTrailLength()
    {
        ResolveHostSettingsUiIfNeeded();

        if (matchRules == null)
        {
            Debug.LogError("[MultiplayerLobby] Cannot change trail length because MatchRules is not assigned.");
            return;
        }

        trailLength++;
        if (trailLength > matchRules.MaxTrailLength)
            trailLength = matchRules.MinTrailLength;

        trailLength = ClampTrailLength(trailLength);
        RefreshTrailLengthUI();

        Debug.Log($"[MultiplayerLobby] ChangeTrailLength -> {trailLength}");
    }

    public void ChangePlayerModelUp()
    {
        ResolveHostSettingsUiIfNeeded();

        if (motorPreview == null || motorPreview.Length == 0)
            return;

        currentModel++;
        if (currentModel >= motorPreview.Length)
            currentModel = 0;

        SetPlayerModel(currentModel);
    }

    public void ChangePlayerModelDown()
    {
        ResolveHostSettingsUiIfNeeded();

        if (motorPreview == null || motorPreview.Length == 0)
            return;

        currentModel--;
        if (currentModel < 0)
            currentModel = motorPreview.Length - 1;

        SetPlayerModel(currentModel);
    }

    public void SetPlayerModel(int selectedMotor)
    {
        ResolveHostSettingsUiIfNeeded();

        if (motorPreview == null || motorPreview.Length == 0)
        {
            Debug.LogWarning("[MultiplayerLobby] Cannot switch scooter preview because Motor Preview array is empty.");
            return;
        }

        currentModel = Mathf.Clamp(selectedMotor, 0, motorPreview.Length - 1);

        for (int i = 0; i < motorPreview.Length; i++)
        {
            if (motorPreview[i] != null)
                motorPreview[i].SetActive(i == currentModel);
        }

        Debug.Log($"[MultiplayerLobby] Scooter preview selected index={currentModel}.");
    }

    public void isSuddenDeath()
    {
        suddenDeath = !suddenDeath;
        if (suddenDeathToggle != null)
            suddenDeathToggle.SetIsOnWithoutNotify(suddenDeath);
    }

    public void tempLog()
    {
        Debug.Log("Tryb Sudden death: " + suddenDeath);
        Debug.Log("Czas trwania meczu w sekundach: " + timeInSecs);
        Debug.Log("Trail length: " + trailLength);
        Debug.Log("Trail color: " + trailColor);
    }

    private void ShowHostSettingsPanel()
    {
        SetActive(connectionTypePanel, false);
        SetActive(hostSettingsPanel, true);
    }

    private IEnumerator WaitForHostReadyRoutine(MultiplayerRuntimeBootstrap runtime)
    {
        float timeoutAt = Time.realtimeSinceStartup + 5f;

        while (Time.realtimeSinceStartup < timeoutAt)
        {
            if (runtime != null && runtime.IsHostReady)
            {
                Debug.Log("[MultiplayerLobby] Host is ready. Start Match enabled.");
                SetStartMatchAvailable(true);
                _hostReadyRoutine = null;
                yield break;
            }

            yield return null;
        }

        Debug.LogError("[MultiplayerLobby] Host did not become ready before timeout.");
        SetStartMatchAvailable(false);
        _hostReadyRoutine = null;
    }

    private void ResolvePanelsIfNeeded()
    {
        if (connectionTypePanel == null)
        {
            Transform found = FindDeepChild(transform, "ConnectionType");
            if (found != null)
                connectionTypePanel = found.gameObject;
        }

        if (hostSettingsPanel == null)
        {
            Transform found = FindDeepChild(transform, "Panel");
            if (found != null)
                hostSettingsPanel = found.gameObject;
        }

        if (connectionTypePanel == null)
            Debug.LogWarning("[MultiplayerLobby] Could not find ConnectionType panel.");

        if (hostSettingsPanel == null)
            Debug.LogWarning("[MultiplayerLobby] Could not find host settings Panel.");
    }

    private void ResolveHostSettingsUiIfNeeded()
    {
        if (minutes == null)
        {
            Transform found = FindDeepChild(transform, "TimePreviewMin");
            if (found != null)
                minutes = found.GetComponent<TMP_Text>();
        }

        if (seconds == null)
        {
            Transform found = FindDeepChild(transform, "TimePreviewSec");
            if (found != null)
                seconds = found.GetComponent<TMP_Text>();
        }

        if (dropdown == null)
        {
            Transform found = FindDeepChild(transform, "GameModeSelector");
            if (found != null)
                dropdown = found.GetComponent<TMP_Dropdown>();
        }

        if (suddenDeathToggle == null)
        {
            Transform found = FindDeepChild(transform, "SuddenDeathTgl");
            if (found != null)
                suddenDeathToggle = found.GetComponent<Toggle>();
        }

        if (trailLengthText == null)
        {
            Transform found =
                FindDeepChild(transform, "TrailLengthValue") ??
                FindDeepChild(transform, "TrailLengthText") ??
                FindDeepChild(transform, "TrailLengthPreview");

            if (found == null)
            {
                Transform trailLengthButton = FindDeepChild(transform, "TrailLengthButton");
                if (trailLengthButton != null)
                    found = FindDeepChild(trailLengthButton, "Value");
            }

            if (found == null)
            {
                Transform trailLengthSetting = FindDeepChild(transform, "TrailLengthSetting");
                if (trailLengthSetting != null)
                    found = FindDeepChild(trailLengthSetting, "Value");
            }

            if (found != null)
                trailLengthText = found.GetComponent<TMP_Text>();
        }

        if ((motorPreview == null || motorPreview.Length == 0) && hostSettingsPanel != null)
        {
            Transform first = FindDeepChild(hostSettingsPanel.transform, "motorPreview");
            Transform second = FindDeepChild(hostSettingsPanel.transform, "motor2Preview");

            if (first != null || second != null)
            {
                motorPreview = second != null
                    ? new[] { first != null ? first.gameObject : null, second.gameObject }
                    : new[] { first.gameObject };
            }
        }
    }

    private bool TryCreateSettings(out MatchSettings settings)
    {
        settings = null;

        if (matchDefaults == null)
        {
            Debug.LogError("MultiplayerLobby cannot start match because MatchDefaults is not assigned. Assign multiplayer MatchDefaults.asset in the MultiplayerLobby inspector.");
            return false;
        }

        if (matchRules == null)
        {
            Debug.LogError("MultiplayerLobby cannot start match because MatchRules is not assigned. Assign MatchRules.asset in the MultiplayerLobby inspector.");
            return false;
        }

        if (trailColorPalette == null)
        {
            Debug.LogError("MultiplayerLobby cannot start match because TrailColorPalette is not assigned.");
            return false;
        }

        if (networkVehiclePrefabSet == null)
        {
            Debug.LogError("MultiplayerLobby cannot start match because Network VehiclePrefabSet is not assigned.");
            return false;
        }

        if (networkVehiclePrefabSet.PlayerVehiclePrefab == null)
        {
            Debug.LogError("MultiplayerLobby cannot start match because Network VehiclePrefabSet has no player vehicle prefab assigned.");
            return false;
        }

        settings = matchDefaults.CreateSettings();
        if (settings == null)
        {
            Debug.LogError("MultiplayerLobby cannot start match because MatchDefaults returned null settings.");
            return false;
        }

        GetSettingsFromUI(dropdown, suddenDeathToggle);
        ApplySelectedTrailColor(settings);

        settings.PlayerCount = 1; // placeholder; server driver replaces with connected humans.
        settings.BotCount = 0;
        settings.MatchMode = selectedMatchMode;
        settings.MatchDurationSeconds = NormalizeMatchDurationSeconds(timeInSecs);
        settings.SuddenDeathEnabled = suddenDeath;
        settings.TrailLength = ClampTrailLength(trailLength);
        settings.PlayerTrailColor = playerTrailColor;

        settings = matchRules.Validate(settings);

        // Multiplayer is currently human-only.
        settings.BotCount = 0;

        if (string.IsNullOrWhiteSpace(settings.ArenaSceneName))
        {
            Debug.LogError("MultiplayerLobby cannot start match because MatchDefaults has no multiplayer arena scene name.");
            return false;
        }

        return true;
    }

    private void ApplySelectedTrailColor(MatchSettings settings)
    {
        playerTrailColor = GetLobbySelectedColorOrFallback();
    }

    private Color GetLobbySelectedColorOrFallback()
    {
        if (TryGetPaletteColor(trailColor, out Color selected))
            return TrailColorPalette.SanitizeColor(selected, Color.white);

        return Color.white;
    }

    private void RefreshTimeUI()
    {
        if (minutes != null)
            minutes.text = min.ToString("00");

        if (seconds != null)
            seconds.text = sec.ToString("00");
    }

    private void SyncTimeFieldsFromSeconds()
    {
        min = timeInSecs / 60;
        sec = timeInSecs % 60;
    }

    private void AdjustMatchDurationSeconds(int deltaSeconds)
    {
        if (!HasDurationRules())
            return;

        SetMatchDurationSeconds(WrapMatchDurationSeconds(timeInSecs + deltaSeconds));
    }

    private void SetMatchDurationSeconds(int value)
    {
        if (!HasDurationRules())
            return;

        timeInSecs = NormalizeMatchDurationSeconds(value);
        SyncTimeFieldsFromSeconds();
        RefreshTimeUI();
    }

    private int WrapMatchDurationSeconds(int value)
    {
        if (!HasDurationRules())
            return value;

        if (value > matchRules.MaxMatchDurationSeconds)
            return matchRules.MinMatchDurationSeconds;

        if (value < matchRules.MinMatchDurationSeconds)
            return matchRules.MaxMatchDurationSeconds;

        return NormalizeMatchDurationSeconds(value);
    }

    private int NormalizeMatchDurationSeconds(int value)
    {
        if (!HasDurationRules())
            return value;

        int clampedValue = Mathf.Clamp(value, matchRules.MinMatchDurationSeconds, matchRules.MaxMatchDurationSeconds);
        int offset = clampedValue - matchRules.MinMatchDurationSeconds;
        int snappedOffset = Mathf.RoundToInt(offset / (float)SecondsStep) * SecondsStep;
        return Mathf.Clamp(matchRules.MinMatchDurationSeconds + snappedOffset, matchRules.MinMatchDurationSeconds, matchRules.MaxMatchDurationSeconds);
    }

    private bool HasDurationRules()
    {
        if (matchRules != null)
            return true;

        Debug.LogError("MultiplayerLobby cannot adjust match duration because MatchRules is not assigned.");
        return false;
    }

    private void RefreshTrailLengthUI()
    {
        if (trailLengthText == null)
            return;

        switch (trailLength)
        {
            case 1:
                trailLengthText.text = "Short";
                break;
            case 2:
                trailLengthText.text = "Medium";
                break;
            case 3:
                trailLengthText.text = "Long";
                break;
            case 4:
                trailLengthText.text = "Permanent";
                break;
            default:
                trailLengthText.text = trailLength.ToString();
                break;
        }
    }

    private int ClampTrailLength(int value)
    {
        if (matchRules == null)
        {
            Debug.LogError("MultiplayerLobby cannot clamp trail length because MatchRules is not assigned.");
            return value;
        }

        return Mathf.Clamp(value, matchRules.MinTrailLength, matchRules.MaxTrailLength);
    }

    private bool TryGetPaletteColor(int index, out Color color)
    {
        color = Color.white;

        if (trailColorPalette == null || trailColorPalette.Colors == null || trailColorPalette.Colors.Count == 0)
            return false;

        index = Mathf.Clamp(index, 0, trailColorPalette.Colors.Count - 1);
        color = trailColorPalette.Colors[index];
        return true;
    }

    private void InitializeModeDropdown()
    {
        if (dropdown == null)
            return;

        List<string> options = new List<string>();
        for (int i = 0; i < SupportedModes.Length; i++)
            options.Add(GetDisplayName(SupportedModes[i]));

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(GetDropdownValueForMode(selectedMatchMode));
        dropdown.RefreshShownValue();
    }

    private static MatchMode GetModeForDropdownValue(int value)
    {
        if (value < 0 || value >= SupportedModes.Length)
            return MatchMode.Deathmatch;

        return SupportedModes[value];
    }

    private static int GetDropdownValueForMode(MatchMode mode)
    {
        for (int i = 0; i < SupportedModes.Length; i++)
        {
            if (SupportedModes[i] == mode)
                return i;
        }

        return 0;
    }

    private static bool IsSupportedMode(MatchMode mode)
    {
        for (int i = 0; i < SupportedModes.Length; i++)
        {
            if (SupportedModes[i] == mode)
                return true;
        }

        return false;
    }

    private static string GetDisplayName(MatchMode mode)
    {
        switch (mode)
        {
            case MatchMode.KingOfTheHill:
                return "King of the Hill";
            default:
                return "Deathmatch";
        }
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

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
