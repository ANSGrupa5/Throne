using UnityEngine;

public abstract class TrailColorSelectionView : LobbyComponent
{
    public int SelectedColorIndex { get; protected set; }

    protected override void OnInitialize()
    {
        SelectedColorIndex = ResolveInitialColorIndex();
        ApplyCurrentSelectionToDefaults();
        InitializeButtons();
    }

    public override void Refresh()
    {
        InitializeButtons();

        TrailColorButtonView[] buttons = Lobby.TrailColorButtons;
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (Lobby.GameSettings != null &&
                Lobby.GameSettings.trailColorPalette != null &&
                i < Lobby.GameSettings.trailColorPalette.Count)
            {
                buttons[i]?.SetColor(Lobby.GameSettings.trailColorPalette[i]);
            }

            buttons[i]?.ApplyState(i == SelectedColorIndex, IsUnavailable(i), CanInteract());
        }
    }

    public virtual void SetTrailColorIndex(int index)
    {
        if (Lobby.GameSettings == null ||
            Lobby.GameSettings.trailColorPalette == null ||
            Lobby.GameSettings.trailColorPalette.Count == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, Lobby.GameSettings.trailColorPalette.Count - 1);
        if (IsUnavailable(index))
        {
            Refresh();
            return;
        }

        SelectedColorIndex = index;
        ApplyCurrentSelectionToDefaults();
        OnSelectedColorChanged();
        Refresh();
    }

    public void SetPlayerTrailColor(Color color)
    {
        Lobby.PlayerTrailColor = color;
        if (Lobby.PlayerLook != null)
            Lobby.PlayerLook.trailColor = color;
    }

    public virtual void ApplyCurrentSelectionToDefaults()
    {
        if (Lobby.GameSettings == null ||
            Lobby.GameSettings.trailColorPalette == null ||
            Lobby.GameSettings.trailColorPalette.Count == 0)
        {
            if (Lobby.PlayerLook != null)
                Lobby.PlayerLook.trailColor = Lobby.PlayerTrailColor;
            return;
        }

        SelectedColorIndex = Mathf.Clamp(SelectedColorIndex, 0, Lobby.GameSettings.trailColorPalette.Count - 1);
        SetPlayerTrailColor(Lobby.GameSettings.trailColorPalette[SelectedColorIndex]);
    }

    protected virtual bool CanInteract()
    {
        return true;
    }

    protected virtual bool IsUnavailable(int colorIndex)
    {
        return false;
    }

    protected virtual void OnSelectedColorChanged()
    {
    }

    private void InitializeButtons()
    {
        TrailColorButtonView[] buttons = Lobby.TrailColorButtons;
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
            buttons[i]?.Initialize();
    }

    private int ResolveInitialColorIndex()
    {
        if (Lobby.GameSettings == null ||
            Lobby.GameSettings.trailColorPalette == null ||
            Lobby.GameSettings.trailColorPalette.Count == 0)
        {
            return 0;
        }

        Color current = Lobby.PlayerLook != null ? Lobby.PlayerLook.trailColor : Lobby.PlayerTrailColor;
        for (int i = 0; i < Lobby.GameSettings.trailColorPalette.Count; i++)
        {
            if (ApproximatelySameColor(current, Lobby.GameSettings.trailColorPalette[i]))
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
}

public sealed class SingleplayerTrailColorSelectionView : TrailColorSelectionView
{
}

public sealed class MultiplayerTrailColorSelectionView : TrailColorSelectionView
{
    public override void OnEnable()
    {
        MultiplayerSessionDriver.TrailColorSelectionsChanged += HandleTrailColorSelectionsChanged;
        PublishSelection();
    }

    public override void OnDisable()
    {
        MultiplayerSessionDriver.TrailColorSelectionsChanged -= HandleTrailColorSelectionsChanged;
    }

    public override void SetTrailColorIndex(int index)
    {
        base.SetTrailColorIndex(index);
        PublishSelection();
    }

    public override void ApplyCurrentSelectionToDefaults()
    {
        base.ApplyCurrentSelectionToDefaults();
        PublishSelection();
    }

    protected override bool CanInteract()
    {
        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && (bootstrap.IsServerStarted || bootstrap.IsClientStarted);
    }

    protected override bool IsUnavailable(int colorIndex)
    {
        return MultiplayerSessionDriver.IsTrailColorTakenByOtherLocalPlayer(colorIndex);
    }

    protected override void OnSelectedColorChanged()
    {
        PublishSelection();
    }

    private void HandleTrailColorSelectionsChanged()
    {
        if (Lobby.GameSettings != null &&
            Lobby.GameSettings.trailColorPalette != null &&
            Lobby.GameSettings.trailColorPalette.Count > 0 &&
            MultiplayerSessionDriver.TryGetLocalTrailColorIndex(out int assignedColorIndex))
        {
            assignedColorIndex = Mathf.Clamp(assignedColorIndex, 0, Lobby.GameSettings.trailColorPalette.Count - 1);
            if (assignedColorIndex != SelectedColorIndex)
            {
                SelectedColorIndex = assignedColorIndex;
                SetPlayerTrailColor(Lobby.GameSettings.trailColorPalette[SelectedColorIndex]);
            }
        }

        Refresh();
    }

    private void PublishSelection()
    {
        if (Lobby == null || !MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return;

        int paletteColorCount = Lobby.GameSettings != null && Lobby.GameSettings.trailColorPalette != null
            ? Lobby.GameSettings.trailColorPalette.Count
            : 0;
        MultiplayerSessionDriver.RequestLocalTrailColor(SelectedColorIndex, paletteColorCount);
    }
}
