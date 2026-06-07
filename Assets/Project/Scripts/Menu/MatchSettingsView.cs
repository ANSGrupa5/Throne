using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class MatchSettingsView : LobbyComponent
{
    private const int TimeStepSeconds = 5;

    private bool _settingsEventsBound;
    private Button[] _settingButtons;

    private int _minutes = 1;
    private int _seconds;
    private float _matchDuration = 60f;
    private string _gameMode = string.Empty;
    private bool _suddenDeath;
    private int _trailLength = 1;

    public float MatchDuration => _matchDuration;
    public bool SuddenDeath => _suddenDeath;
    public int TrailLength => _trailLength;
    public int GameModeIndex => Lobby.GameModeDropdown != null ? Lobby.GameModeDropdown.value : 0;

    protected virtual bool CanEdit => false;

    protected override void OnInitialize()
    {
        LoadFromGameSettings();
        ShowGameTime();
        ShowTrailLength();
    }

    public override void OnEnable()
    {
        BindSettingsEvents();
    }

    public override void OnDisable()
    {
        UnbindSettingsEvents();
    }

    public override void Refresh()
    {
        RefreshInteractivity();
        ShowGameTime();
        ShowTrailLength();
    }

    public void IncreaseMin()
    {
        if (!CanEdit)
            return;

        SetMatchTime(_minutes + 1, _seconds);
    }

    public void DecreaseMin()
    {
        if (!CanEdit)
            return;

        SetMatchTime(_minutes - 1, _seconds);
    }

    public void IncreaseSec()
    {
        if (!CanEdit)
            return;

        SetMatchTime(_minutes, _seconds + TimeStepSeconds);
    }

    public void DecreaseSec()
    {
        if (!CanEdit)
            return;

        SetMatchTime(_minutes, _seconds - TimeStepSeconds);
    }

    public void ChangeTrailLength()
    {
        if (Lobby.GameSettings == null || !CanEdit)
            return;

        _trailLength++;
        if (_trailLength > Lobby.GameSettings.GetMaxTrailLength())
            _trailLength = Lobby.GameSettings.GetMinTrailLength();

        ApplyToGameSettings(Lobby.GameSettings);
        ShowTrailLength();
        Lobby.MarkLobbyStateDirty();
    }

    public void ToggleSuddenDeath()
    {
        if (!CanEdit)
            return;

        _suddenDeath = Lobby.SuddenDeathToggle != null ? Lobby.SuddenDeathToggle.isOn : !_suddenDeath;
        ApplyToGameSettings(Lobby.GameSettings);
        Lobby.MarkLobbyStateDirty();
    }

    public void ShowGameTime()
    {
        if (Lobby.MinutesText != null)
            Lobby.MinutesText.text = _minutes.ToString("00");
        if (Lobby.SecondsText != null)
            Lobby.SecondsText.text = _seconds.ToString("00");
    }

    public virtual void ApplyToGameSettings(GameSettings gameSettings)
    {
        if (gameSettings == null)
            return;

        ReadSettingsFromUi();
        WriteCurrentSettings(gameSettings);
    }

    protected void ApplySyncedLobbySettings(MultiplayerSessionDriver.LobbyStateSnapshot snapshot)
    {
        SetDisplayedMatchDuration(snapshot.MatchDuration);
        ShowGameTime();

        _trailLength = ClampTrailLength(snapshot.TrailLength);
        ShowTrailLength();

        _suddenDeath = snapshot.SuddenDeath;
        if (Lobby.SuddenDeathToggle != null)
            Lobby.SuddenDeathToggle.SetIsOnWithoutNotify(_suddenDeath);

        TMP_Dropdown dropdown = Lobby.GameModeDropdown;
        if (dropdown != null && dropdown.options.Count > 0)
        {
            dropdown.SetValueWithoutNotify(Mathf.Clamp(snapshot.GameModeIndex, 0, dropdown.options.Count - 1));
            _gameMode = dropdown.options[dropdown.value].text;
        }

        WriteCurrentSettings(Lobby.GameSettings);
    }

    protected void PublishHostLobbyState()
    {
        if (!Lobby.IsLobbyStateDirty)
            return;

        if (MultiplayerSessionDriver.Instance == null)
            return;

        ApplyToGameSettings(Lobby.GameSettings);

        MultiplayerSessionDriver.PublishHostLobbyState(
            Lobby.Opponents != null ? Lobby.Opponents.GetHumanSlotCount() : 0,
            Lobby.OpponentSlots != null ? Lobby.OpponentSlots.Length : 0,
            Lobby.Opponents != null ? Lobby.Opponents.GetBotSlotMask() : 0,
            _trailLength,
            _matchDuration,
            GameModeIndex,
            Lobby.SuddenDeathToggle != null ? Lobby.SuddenDeathToggle.isOn : _suddenDeath);
        Lobby.ClearLobbyStateDirty();
    }

    protected void RefreshInteractivity()
    {
        bool canEdit = CanEdit;

        if (Lobby.GameModeDropdown != null)
            Lobby.GameModeDropdown.interactable = canEdit;
        if (Lobby.SuddenDeathToggle != null)
            Lobby.SuddenDeathToggle.interactable = canEdit;

        Button[] settingButtons = GetSettingButtons();
        for (int i = 0; i < settingButtons.Length; i++)
        {
            if (settingButtons[i] != null)
                settingButtons[i].interactable = canEdit;
        }
    }

    private void SetMatchTime(int newMin, int newSec)
    {
        int minTotalSeconds = Mathf.RoundToInt(GameSettings.MinMatchDuration);
        int maxTotalSeconds = Mathf.RoundToInt(GameSettings.MaxMatchDuration);
        int totalSeconds = newMin * 60 + newSec;

        if (totalSeconds > maxTotalSeconds)
            totalSeconds = minTotalSeconds;
        else if (totalSeconds < minTotalSeconds)
            totalSeconds = maxTotalSeconds;

        _minutes = totalSeconds / 60;
        _seconds = totalSeconds % 60;
        UpdateTimeInSeconds();
        ApplyToGameSettings(Lobby.GameSettings);
        ShowGameTime();
        Lobby.MarkLobbyStateDirty();
    }

    private void UpdateTimeInSeconds()
    {
        _matchDuration = _minutes * 60f + _seconds;
    }

    private void ShowTrailLength()
    {
        if (Lobby.TrailLengthText == null)
            return;

        switch (_trailLength)
        {
            case 0:
                Lobby.TrailLengthText.text = "Short";
                break;
            case 1:
                Lobby.TrailLengthText.text = "Medium";
                break;
            case 2:
                Lobby.TrailLengthText.text = "Long";
                break;
            case 3:
                Lobby.TrailLengthText.text = "Permanent";
                break;
        }
    }

    private void BindSettingsEvents()
    {
        if (_settingsEventsBound)
            return;

        if (Lobby.GameModeDropdown != null)
            Lobby.GameModeDropdown.onValueChanged.AddListener(HandleGameModeChanged);
        if (Lobby.SuddenDeathToggle != null)
            Lobby.SuddenDeathToggle.onValueChanged.AddListener(HandleSuddenDeathChanged);

        _settingsEventsBound = true;
    }

    private void UnbindSettingsEvents()
    {
        if (!_settingsEventsBound)
            return;

        if (Lobby.GameModeDropdown != null)
            Lobby.GameModeDropdown.onValueChanged.RemoveListener(HandleGameModeChanged);
        if (Lobby.SuddenDeathToggle != null)
            Lobby.SuddenDeathToggle.onValueChanged.RemoveListener(HandleSuddenDeathChanged);

        _settingsEventsBound = false;
    }

    private void HandleGameModeChanged(int _)
    {
        if (!CanEdit)
            return;

        ReadSettingsFromUi();
        ApplyToGameSettings(Lobby.GameSettings);
        Lobby.PlayUiClickSound();
        Lobby.MarkLobbyStateDirty();
    }

    private void HandleSuddenDeathChanged(bool value)
    {
        if (!CanEdit)
            return;

        _suddenDeath = value;
        ApplyToGameSettings(Lobby.GameSettings);
        Lobby.MarkLobbyStateDirty();
    }

    private void ReadSettingsFromUi()
    {
        TMP_Dropdown dropdown = Lobby.GameModeDropdown;
        _gameMode = dropdown != null && dropdown.options.Count > dropdown.value
            ? dropdown.options[dropdown.value].text
            : string.Empty;
        _suddenDeath = Lobby.SuddenDeathToggle != null && Lobby.SuddenDeathToggle.isOn;
    }

    private void LoadFromGameSettings()
    {
        GameSettings gameSettings = Lobby.GameSettings;
        if (gameSettings == null)
        {
            SetDisplayedMatchDuration(GameSettings.MinMatchDuration);
            _trailLength = 1;
            ReadSettingsFromUi();
            return;
        }

        SetDisplayedMatchDuration(gameSettings.matchDuration);
        _trailLength = ClampTrailLength(gameSettings.trailLength);
        _suddenDeath = gameSettings.isSuddenDeath;

        if (Lobby.SuddenDeathToggle != null)
            Lobby.SuddenDeathToggle.SetIsOnWithoutNotify(_suddenDeath);

        TMP_Dropdown dropdown = Lobby.GameModeDropdown;
        if (dropdown != null && dropdown.options.Count > 0)
        {
            dropdown.SetValueWithoutNotify(Mathf.Clamp(gameSettings.gameMode, 0, dropdown.options.Count - 1));
            _gameMode = dropdown.options[dropdown.value].text;
        }
        else
        {
            _gameMode = string.Empty;
        }

        WriteCurrentSettings(gameSettings);
    }

    private void SetDisplayedMatchDuration(float duration)
    {
        int totalSeconds = Mathf.RoundToInt(Mathf.Clamp(
            duration,
            GameSettings.MinMatchDuration,
            GameSettings.MaxMatchDuration));

        _minutes = totalSeconds / 60;
        _seconds = totalSeconds % 60;
        _matchDuration = totalSeconds;
    }

    private void WriteCurrentSettings(GameSettings gameSettings)
    {
        if (gameSettings == null)
            return;

        gameSettings.matchDuration = Mathf.Clamp(
            _matchDuration,
            GameSettings.MinMatchDuration,
            GameSettings.MaxMatchDuration);
        gameSettings.isSuddenDeath = _suddenDeath;
        gameSettings.trailLength = ClampTrailLength(_trailLength);
        gameSettings.gameMode = ResolveGameModeIndex();
    }

    private int ClampTrailLength(int trailLength)
    {
        GameSettings gameSettings = Lobby.GameSettings;
        if (gameSettings == null)
            return Mathf.Clamp(trailLength, 0, 3);

        return Mathf.Clamp(
            trailLength,
            gameSettings.GetMinTrailLength(),
            gameSettings.GetMaxTrailLength());
    }

    private int ResolveGameModeIndex()
    {
        switch (_gameMode)
        {
            case "Deathmatch":
                return 1;
            case "Battle Royale":
                return 0;
            default:
                return GameModeIndex;
        }
    }

    private Button[] GetSettingButtons()
    {
        if (_settingButtons != null)
            return _settingButtons;

        Button[] buttons = Lobby.GetComponentsInChildren<Button>(true);
        System.Collections.Generic.List<Button> matches = new();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            switch (button.name)
            {
                case "MinDownButton":
                case "MinUpButton":
                case "SecDownButton":
                case "SecUpButton":
                case "TrailLengthButton":
                    matches.Add(button);
                    break;
            }
        }

        _settingButtons = matches.ToArray();
        return _settingButtons;
    }
}

public class EditableMatchSettingsView : MatchSettingsView
{
    protected override bool CanEdit => true;
}

public sealed class MultiplayerHostMatchSettingsView : EditableMatchSettingsView
{
    public override void Tick()
    {
        PublishHostLobbyState();
    }
}

public sealed class MultiplayerClientMatchSettingsView : MatchSettingsView
{
    public override void OnEnable()
    {
        base.OnEnable();
        MultiplayerSessionDriver.LobbyStateChanged += HandleLobbyStateChanged;
        ApplySnapshotIfAvailable();
    }

    public override void OnDisable()
    {
        MultiplayerSessionDriver.LobbyStateChanged -= HandleLobbyStateChanged;
        base.OnDisable();
    }

    public override void Refresh()
    {
        ApplySnapshotIfAvailable();
        base.Refresh();
    }

    private void HandleLobbyStateChanged()
    {
        ApplySnapshotIfAvailable();
    }

    private void ApplySnapshotIfAvailable()
    {
        if (MultiplayerSessionDriver.TryGetLobbyState(out MultiplayerSessionDriver.LobbyStateSnapshot snapshot))
            ApplySyncedLobbySettings(snapshot);
    }
}
