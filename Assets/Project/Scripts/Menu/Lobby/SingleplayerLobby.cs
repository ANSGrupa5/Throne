using UnityEngine;

public class SingleplayerLobby : Lobby
{
    [SerializeField] private SingleplayerOpponentSlotView opponents = new();
    private readonly ScooterSelectView _scooters = new();
    private readonly SingleplayerTrailColorSelectionView _trailColors = new();
    [SerializeField] private EditableMatchSettingsView matchSettings = new();

    protected override bool IsSingleplayerLobby => true;

    protected override void ConfigureComponentsForCurrentRole()
    {
        UseComponents(opponents, _scooters, _trailColors, matchSettings);
    }

    protected override bool CanStartMatch()
    {
        return BotCount > 0;
    }
}
