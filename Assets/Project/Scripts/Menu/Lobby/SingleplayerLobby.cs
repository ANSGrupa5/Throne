using UnityEngine;

public class SingleplayerLobby : Lobby
{
    [SerializeField] private SingleplayerOpponentSlotView opponents = new();
    [SerializeField] private ScooterSelectView scooters = new();
    private readonly SingleplayerTrailColorSelectionView _trailColors = new();
    [SerializeField] private EditableMatchSettingsView matchSettings = new();

    protected override bool IsSingleplayerLobby => true;

    protected override void ConfigureComponentsForCurrentRole()
    {
        UseComponents(opponents, scooters, _trailColors, matchSettings);
    }

    protected override bool CanStartMatch()
    {
        return BotCount > 0;
    }
}
