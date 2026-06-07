public class SingleplayerLobby : Lobby
{
    private readonly SingleplayerOpponentSlotView _opponents = new();
    private readonly ScooterSelectView _scooters = new();
    private readonly SingleplayerTrailColorSelectionView _trailColors = new();
    private readonly EditableMatchSettingsView _matchSettings = new();

    protected override bool IsSingleplayerLobby => true;

    protected override void ConfigureComponentsForCurrentRole()
    {
        UseComponents(_opponents, _scooters, _trailColors, _matchSettings);
    }

    protected override bool CanStartMatch()
    {
        return BotCount > 0;
    }
}
