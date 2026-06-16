using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SingleplayerLobby : MonoBehaviour
{
    private static readonly MatchMode[] SupportedModes =
    {
        MatchMode.Deathmatch,
        MatchMode.KingOfTheHill
    };

    private const int SecondsStep = 5;
    private const int MinutesStep = 60;

    [Header("Default Config Assets")]
    [SerializeField] private MatchDefaults matchDefaults;
    [SerializeField] private MatchRules matchRules;
    [SerializeField] private TrailColorPalette trailColorPalette;
    [SerializeField] private VehiclePrefabSet vehiclePrefabSet;

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

    private int _botCount;
    private int min;
    private int sec;
    private int timeInSecs;
    private int trailLength;
    private int trailColor;
    private MatchMode selectedMatchMode = MatchMode.Deathmatch;
    private bool suddenDeath;
    private bool hasSelectedTrailColor;

    private void Awake()
    {
        if (matchRules == null)
            Debug.LogError("SingleplayerLobby needs MatchRules assigned to apply lobby UI bounds. Assign MatchRules.asset in the SingleplayerLobby inspector.");

        MatchSettings defaultSettings = matchDefaults != null ? matchDefaults.CreateSettings() : null;

        if (defaultSettings != null)
        {
            _botCount = ClampBotCount(defaultSettings.BotCount);
            selectedMatchMode = IsSupportedMode(defaultSettings.MatchMode) ? defaultSettings.MatchMode : MatchMode.Deathmatch;
            suddenDeath = defaultSettings.SuddenDeathEnabled;
            trailLength = ClampTrailLength(defaultSettings.TrailLength);
            timeInSecs = NormalizeMatchDurationSeconds(defaultSettings.MatchDurationSeconds);
            playerTrailColor = trailColorPalette != null
                ? trailColorPalette.GetDefaultColor(defaultSettings.PlayerTrailColor)
                : defaultSettings.PlayerTrailColor;
        }

        SyncTimeFieldsFromSeconds();
        RefreshTimeUI();
        RefreshBotCountUI();
        InitializeModeDropdown();
        RefreshTrailLengthUI();

        if (suddenDeathToggle != null)
            suddenDeathToggle.SetIsOnWithoutNotify(suddenDeath);

        currentModel = 0;
        if (motorPreview != null && motorPreview.Length > 0 && motorPreview[0] != null)
            motorPreview[0].SetActive(true);
    }

    public void LoadScene(string sceneName)
    {
        LoadScene();
    }

    public void LoadScene()
    {
        if (!InitializeGame(out string arenaSceneName))
            return;

        SceneManager.LoadScene(arenaSceneName);
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

    public void AddBot()
    {
        SetBotCount(_botCount + 1);
    }

    public void RemoveBot()
    {
        SetBotCount(_botCount - 1);
    }

    public void SetPlayerTrailColor(Color color)
    {
        playerTrailColor = color;
        hasSelectedTrailColor = true;
    }

    public void SetPlayerTrailColorFromPaletteIndex(int index)
    {
        if (!TryGetPaletteColor(index, out Color color))
        {
            Debug.LogError("SingleplayerLobby cannot select a trail color because TrailColorPalette is not assigned or has no colors.");
            return;
        }

        SetPlayerTrailColor(color);
    }

    private bool InitializeGame(out string arenaSceneName)
    {
        arenaSceneName = string.Empty;

        if (!TryCreateSettings(out MatchSettings settings))
            return false;

        GameSessionRuntime session = GameSessionRuntime.FromSettings(
            settings,
            vehiclePrefabSet,
            trailColorPalette,
            isSingleplayer: true);

        if (session == null)
        {
            Debug.LogError("SingleplayerLobby cannot start match because GameSessionRuntime.FromSettings returned null.");
            return false;
        }

        GameSessionBootstrap.SetSession(session);
        arenaSceneName = settings.ArenaSceneName;
        Debug.Log($"Initializing singleplayer game with scene '{arenaSceneName}'");
        return true;
    }

    private bool TryCreateSettings(out MatchSettings settings)
    {
        settings = null;

        if (matchDefaults == null)
        {
            Debug.LogError("SingleplayerLobby cannot start match because MatchDefaults is not assigned. Assign MatchDefaults.asset in the SingleplayerLobby inspector.");
            return false;
        }

        if (matchRules == null)
        {
            Debug.LogError("SingleplayerLobby cannot start match because MatchRules is not assigned. Assign MatchRules.asset in the SingleplayerLobby inspector.");
            return false;
        }

        if (trailColorPalette == null)
        {
            Debug.LogError("SingleplayerLobby cannot start match because TrailColorPalette is not assigned. Assign TrailColorPalette.asset in the SingleplayerLobby inspector.");
            return false;
        }

        if (vehiclePrefabSet == null)
        {
            Debug.LogError("SingleplayerLobby cannot start match because VehiclePrefabSet is not assigned. Assign VehiclePrefabSet.asset in the SingleplayerLobby inspector.");
            return false;
        }

        settings = matchDefaults.CreateSettings();
        if (settings == null)
        {
            Debug.LogError("SingleplayerLobby cannot start match because MatchDefaults returned null settings.");
            return false;
        }

        GetSettingsFromUI(dropdown, suddenDeathToggle);
        ApplySelectedTrailColor(settings);

        settings.PlayerCount = 1;
        settings.BotCount = ClampBotCount(_botCount);
        settings.MatchMode = selectedMatchMode;
        settings.MatchDurationSeconds = NormalizeMatchDurationSeconds(timeInSecs);
        settings.SuddenDeathEnabled = suddenDeath;
        settings.TrailLength = ClampTrailLength(trailLength);
        settings.PlayerTrailColor = playerTrailColor;

        settings = matchRules.Validate(settings);

        if (string.IsNullOrWhiteSpace(settings.ArenaSceneName))
        {
            Debug.LogError("SingleplayerLobby cannot start match because MatchDefaults has no arena scene name. Assign an arena SceneReference in MatchDefaults.asset.");
            return false;
        }

        if (vehiclePrefabSet.PlayerVehiclePrefab == null)
        {
            Debug.LogError("SingleplayerLobby cannot start match because VehiclePrefabSet has no player vehicle prefab assigned.");
            return false;
        }

        if (settings.BotCount > 0 && vehiclePrefabSet.BotVehiclePrefab == null)
        {
            Debug.LogError("SingleplayerLobby cannot start match because VehiclePrefabSet has no bot vehicle prefab assigned.");
            return false;
        }

        return true;
    }

    private void ApplySelectedTrailColor(MatchSettings settings)
    {
        if (hasSelectedTrailColor)
            return;

        if (TryGetPaletteColor(trailColor, out Color selectedColor))
        {
            playerTrailColor = selectedColor;
            return;
        }

        playerTrailColor = trailColorPalette.GetDefaultColor(settings.PlayerTrailColor);
    }

    private void SetBotCount(int value)
    {
        _botCount = ClampBotCount(value);
        RefreshBotCountUI();
    }

    private int ClampBotCount(int value)
    {
        if (matchRules == null)
        {
            Debug.LogError("SingleplayerLobby cannot clamp bot count because MatchRules is not assigned.");
            return value;
        }

        return Mathf.Clamp(value, matchRules.MinBotCount, matchRules.MaxBotCount);
    }

    private void RefreshBotCountUI()
    {
        if (botCountText != null)
            botCountText.text = _botCount.ToString();
    }

    public void isSuddenDeath()
    {
        suddenDeath = !suddenDeath;
        if (suddenDeathToggle != null)
            suddenDeathToggle.SetIsOnWithoutNotify(suddenDeath);
    }

    public void tempLog()
    {
        Debug.Log("Powinno dodac sie " + _botCount + " botow");
        Debug.Log("Tryb Sudden death: " + suddenDeath);
        Debug.Log("Czas trwania meczu w sekundach: " + timeInSecs);
        Debug.Log("Trail length: " + trailLength);
        Debug.Log("Trail color: " + trailColor);
    }

    public void ShowGameTime()
    {
        RefreshTimeUI();
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

    public void IncreaseMin()
    {
        AdjustMatchDurationSeconds(MinutesStep);
    }

    public void DecreaseMin()
    {
        AdjustMatchDurationSeconds(-MinutesStep);
    }

    public void IncreaseSec()
    {
        AdjustMatchDurationSeconds(SecondsStep);
    }

    public void DecreaseSec()
    {
        AdjustMatchDurationSeconds(-SecondsStep);
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

        Debug.LogError("SingleplayerLobby cannot adjust match duration because MatchRules is not assigned.");
        return false;
    }

    public void ChangeTrailLength()
    {
        if (matchRules == null)
        {
            Debug.LogError("SingleplayerLobby cannot change trail length because MatchRules is not assigned.");
            return;
        }

        trailLength++;
        if (trailLength > matchRules.MaxTrailLength)
            trailLength = matchRules.MinTrailLength;

        trailLength = ClampTrailLength(trailLength);
        RefreshTrailLengthUI();
    }

    public void SetTrailColor(int value)
    {
        trailColor = value;
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
        if (motorPreview == null || motorPreview.Length == 0)
            return;

        currentModel = Mathf.Clamp(selectedMotor, 0, motorPreview.Length - 1);

        for (int i = 0; i < motorPreview.Length; i++)
        {
            if (motorPreview[i] != null)
                motorPreview[i].SetActive(i == currentModel);
        }
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
            Debug.LogError("SingleplayerLobby cannot clamp trail length because MatchRules is not assigned.");
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
}
