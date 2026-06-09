using UnityEngine;

public sealed class MultiplayerLobby : Lobby
{
    private readonly MultiplayerHostOpponentSlotView _hostOpponents = new();
    private readonly MultiplayerClientOpponentSlotView _clientOpponents = new();
    private readonly ScooterSelectView _scooters = new();
    private readonly MultiplayerTrailColorSelectionView _trailColors = new();
    [SerializeField] private MultiplayerHostMatchSettingsView hostMatchSettings = new();
    [SerializeField] private MultiplayerClientMatchSettingsView clientMatchSettings = new();

    protected override bool IsSingleplayerLobby => false;

    protected override void ConfigureComponentsForCurrentRole()
    {
        if (IsHost())
            UseComponents(_hostOpponents, _scooters, _trailColors, hostMatchSettings);
        else
            UseComponents(_clientOpponents, _scooters, _trailColors, clientMatchSettings);
    }

    protected override bool CanStartMatch()
    {
        return IsHost();
    }

    private static bool IsHost()
    {
        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && bootstrap.IsServerStarted;
    }
}
