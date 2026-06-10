using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public abstract class MatchSettingsView : LobbyComponent
{
    private const int TimeStepSeconds = 5;
    private const float FallbackMinMatchDurationSeconds = 10f;
    private const float FallbackMaxMatchDurationSeconds = 600f;
    private const int FallbackMinTrailLength = 0;
    private const int FallbackMaxTrailLength = 3;

    [Header("Time")]
    [SerializeField] private TMP_Text minutesText;
    [SerializeField] private TMP_Text secondsText;
    [SerializeField] private Button minDownButton;
    [SerializeField] private Button minUpButton;
    [SerializeField] private Button secDownButton;
    [SerializeField] private Button secUpButton;

    [Header("Mode")]
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private Toggle suddenDeathToggle;

    [Header("Trail Length")]
    [SerializeField] private TMP_Text trailLengthText;
    [SerializeField] private Button trailLengthButton;

    private LobbyState _state;
    private MatchRules _rules;
    private bool _settingsEventsBound;
    private bool _canEdit;

    public event Action Changed;

    public float MatchDuration => _state != null ? _state.MatchDurationSeconds : FallbackMinMatchDurationSeconds;
    public bool SuddenDeath => _state != null && _state.SuddenDeath;
    public int TrailLength => _state != null ? _state.TrailLength : FallbackMinTrailLength;
    // Assumes dropdown option index matches the serialized MatchMode enum value.
    public int GameModeIndex => (int)SelectedMatchMode;
    public MatchMode SelectedMatchMode => _state != null ? _state.MatchMode : MatchMode.KingOfTheHill;

    internal bool WantsEditAccess => WantsEditAccessByDefault;

    protected bool CanEdit => _canEdit;
    protected virtual bool WantsEditAccessByDefault => false;

    public void Initialize(LobbyState state, MatchRules rules, bool canEdit)
    {
        bool rebind = _settingsEventsBound;
        if (rebind)
            UnbindSettingsEvents();

        _state = state;
        _rules = rules;
        _canEdit = canEdit;
        NormalizeState();
        Refresh();

        if (rebind)
            BindSettingsEvents();
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
        Render();
    }

    private void Render()
    {
        RefreshInteractivity();
        ShowGameTime();
        ShowTrailLength();
        ShowModeSettings();
    }

    public void IncreaseMin()
    {
        if (!CanEdit)
            return;

        SetMatchTime(CurrentMinutes + 1, CurrentSeconds);
    }

    public void DecreaseMin()
    {
        if (!CanEdit)
            return;

        SetMatchTime(CurrentMinutes - 1, CurrentSeconds);
    }

    public void IncreaseSec()
    {
        if (!CanEdit)
            return;

        SetMatchTime(CurrentMinutes, CurrentSeconds + TimeStepSeconds);
    }

    public void DecreaseSec()
    {
        if (!CanEdit)
            return;

        SetMatchTime(CurrentMinutes, CurrentSeconds - TimeStepSeconds);
    }

    public void ChangeTrailLength()
    {
        if (_state == null || !CanEdit)
            return;

        int minTrailLength = MinTrailLength;
        int maxTrailLength = MaxTrailLength;
        int nextTrailLength = _state.TrailLength + 1;
        if (nextTrailLength > maxTrailLength)
            nextTrailLength = minTrailLength;

        _state.TrailLength = ClampTrailLength(nextTrailLength);
        ShowTrailLength();
        NotifyChanged();
    }

    public void ToggleSuddenDeath()
    {
        if (!CanEdit)
            return;

        SetSuddenDeath(suddenDeathToggle != null ? suddenDeathToggle.isOn : !SuddenDeath);
    }

    public void ShowGameTime()
    {
        int totalSeconds = Mathf.RoundToInt(MatchDuration);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        if (minutesText != null)
            minutesText.text = minutes.ToString("00");
        if (secondsText != null)
            secondsText.text = seconds.ToString("00");
    }

    protected void ApplySyncedLobbySettings(LobbyStateSnapshot snapshot)
    {
        if (_state == null)
            return;

        Lobby?.ApplySyncedLobbyStateSnapshot(snapshot);
        _state.MatchDurationSeconds = ClampMatchDuration(snapshot.MatchDurationSeconds);
        _state.TrailLength = ClampTrailLength(snapshot.TrailLength);
        _state.SuddenDeath = snapshot.SuddenDeath;
        _state.MatchMode = snapshot.MatchMode;
        _state.IsDirty = false;

        Render();
    }

    protected void PublishHostLobbyState()
    {
        if (_state == null || !_state.IsDirty)
            return;

        if (Lobby != null && Lobby.PublishCurrentHostLobbyState(_state))
            _state.IsDirty = false;
    }

    protected void RefreshInteractivity()
    {
        bool canEdit = CanEdit;

        if (gameModeDropdown != null)
            gameModeDropdown.interactable = canEdit;
        if (suddenDeathToggle != null)
            suddenDeathToggle.interactable = canEdit;

        SetButtonInteractivity(minDownButton, canEdit);
        SetButtonInteractivity(minUpButton, canEdit);
        SetButtonInteractivity(secDownButton, canEdit);
        SetButtonInteractivity(secUpButton, canEdit);
        SetButtonInteractivity(trailLengthButton, canEdit);
    }

    private void SetMatchTime(int newMin, int newSec)
    {
        if (_state == null)
            return;

        int minTotalSeconds = Mathf.RoundToInt(MinMatchDurationSeconds);
        int maxTotalSeconds = Mathf.RoundToInt(MaxMatchDurationSeconds);
        int totalSeconds = newMin * 60 + newSec;

        if (totalSeconds > maxTotalSeconds)
            totalSeconds = minTotalSeconds;
        else if (totalSeconds < minTotalSeconds)
            totalSeconds = maxTotalSeconds;

        _state.MatchDurationSeconds = ClampMatchDuration(totalSeconds);
        ShowGameTime();
        NotifyChanged();
    }

    private void ShowTrailLength()
    {
        if (trailLengthText == null)
            return;

        switch (TrailLength)
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
            default:
                trailLengthText.text = TrailLength.ToString();
                break;
        }
    }

    private void BindSettingsEvents()
    {
        if (_settingsEventsBound)
            return;

        if (minDownButton != null)
            minDownButton.onClick.AddListener(DecreaseMin);
        if (minUpButton != null)
            minUpButton.onClick.AddListener(IncreaseMin);
        if (secDownButton != null)
            secDownButton.onClick.AddListener(DecreaseSec);
        if (secUpButton != null)
            secUpButton.onClick.AddListener(IncreaseSec);
        if (trailLengthButton != null)
            trailLengthButton.onClick.AddListener(ChangeTrailLength);
        if (gameModeDropdown != null)
            gameModeDropdown.onValueChanged.AddListener(HandleGameModeChanged);
        if (suddenDeathToggle != null)
            suddenDeathToggle.onValueChanged.AddListener(HandleSuddenDeathChanged);

        _settingsEventsBound = true;
    }

    private void UnbindSettingsEvents()
    {
        if (!_settingsEventsBound)
            return;

        if (minDownButton != null)
            minDownButton.onClick.RemoveListener(DecreaseMin);
        if (minUpButton != null)
            minUpButton.onClick.RemoveListener(IncreaseMin);
        if (secDownButton != null)
            secDownButton.onClick.RemoveListener(DecreaseSec);
        if (secUpButton != null)
            secUpButton.onClick.RemoveListener(IncreaseSec);
        if (trailLengthButton != null)
            trailLengthButton.onClick.RemoveListener(ChangeTrailLength);
        if (gameModeDropdown != null)
            gameModeDropdown.onValueChanged.RemoveListener(HandleGameModeChanged);
        if (suddenDeathToggle != null)
            suddenDeathToggle.onValueChanged.RemoveListener(HandleSuddenDeathChanged);

        _settingsEventsBound = false;
    }

    private void HandleGameModeChanged(int _)
    {
        if (!CanEdit)
            return;

        if (_state == null)
            return;

        _state.MatchMode = LobbyStateGameSettingsAdapter.ToMatchMode(gameModeDropdown != null ? gameModeDropdown.value : 0);
        NotifyChanged();
    }

    private void HandleSuddenDeathChanged(bool value)
    {
        if (!CanEdit)
            return;

        SetSuddenDeath(value);
    }

    private void SetSuddenDeath(bool value)
    {
        if (_state == null)
            return;

        _state.SuddenDeath = value;
        if (suddenDeathToggle != null)
            suddenDeathToggle.SetIsOnWithoutNotify(value);
        NotifyChanged();
    }

    private void NormalizeState()
    {
        if (_state == null)
            return;

        _state.MatchDurationSeconds = ClampMatchDuration(_state.MatchDurationSeconds);
        _state.TrailLength = ClampTrailLength(_state.TrailLength);
    }

    private float ClampMatchDuration(float duration)
    {
        return _rules != null
            ? _rules.ClampMatchDuration(duration)
            : Mathf.Clamp(duration, FallbackMinMatchDurationSeconds, FallbackMaxMatchDurationSeconds);
    }

    private int ClampTrailLength(int trailLength)
    {
        return _rules != null
            ? _rules.ClampTrailLength(trailLength)
            : Mathf.Clamp(trailLength, FallbackMinTrailLength, FallbackMaxTrailLength);
    }

    private float MinMatchDurationSeconds => _rules != null ? _rules.MinMatchDurationSeconds : FallbackMinMatchDurationSeconds;
    private float MaxMatchDurationSeconds => _rules != null ? _rules.MaxMatchDurationSeconds : FallbackMaxMatchDurationSeconds;
    private int MinTrailLength => _rules != null ? _rules.MinTrailLength : FallbackMinTrailLength;
    private int MaxTrailLength => _rules != null ? _rules.MaxTrailLength : FallbackMaxTrailLength;

    private void ShowModeSettings()
    {
        if (suddenDeathToggle != null)
            suddenDeathToggle.SetIsOnWithoutNotify(SuddenDeath);

        if (gameModeDropdown == null || gameModeDropdown.options.Count <= 0)
            return;

        int value = Mathf.Clamp(GameModeIndex, 0, gameModeDropdown.options.Count - 1);
        gameModeDropdown.SetValueWithoutNotify(value);
    }

    private void SetButtonInteractivity(Button button, bool canEdit)
    {
        if (button != null)
            button.interactable = canEdit;
    }

    private void NotifyChanged()
    {
        if (_state != null)
            _state.IsDirty = true;

        Changed?.Invoke();
    }

    private int CurrentMinutes
    {
        get
        {
            int totalSeconds = Mathf.RoundToInt(MatchDuration);
            return totalSeconds / 60;
        }
    }

    private int CurrentSeconds
    {
        get
        {
            int totalSeconds = Mathf.RoundToInt(MatchDuration);
            return totalSeconds % 60;
        }
    }
}

[Serializable]
public class EditableMatchSettingsView : MatchSettingsView
{
    protected override bool WantsEditAccessByDefault => true;
}

[Serializable]
public sealed class MultiplayerHostMatchSettingsView : EditableMatchSettingsView
{
    public override void Tick()
    {
        PublishHostLobbyState();
    }
}

[Serializable]
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
        if (MultiplayerSessionDriver.TryGetLobbyState(out LobbyStateSnapshot snapshot))
            ApplySyncedLobbySettings(snapshot);
    }
}
