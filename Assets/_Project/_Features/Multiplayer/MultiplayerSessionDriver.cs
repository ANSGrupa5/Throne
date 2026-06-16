using FishNet;
using FishNet.Object;
using UnityEngine;

public sealed class MultiplayerSessionDriver : NetworkBehaviour
{
    public static MultiplayerSessionDriver Instance { get; private set; }

    [Header("Default Config Assets")]
    [SerializeField] private MatchDefaults matchDefaults;
    [SerializeField] private MatchRules matchRules;
    [SerializeField] private VehiclePrefabSet networkVehiclePrefabSet;
    [SerializeField] private TrailColorPalette trailColorPalette;

    public bool IsMatchRunning { get; private set; }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Instance = this;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (Instance == this)
            Instance = null;

        IsMatchRunning = false;
        MultiplayerMatchState.Reset();
        MultiplayerHudBridge.ResetAppliedState();
    }

    [Server]
    public void StartMatch()
    {
        StartMatch(matchDefaults, matchRules, networkVehiclePrefabSet, trailColorPalette);
    }

    [Server]
    public void StartMatch(
        MatchDefaults matchDefaultsOverride,
        MatchRules matchRulesOverride,
        VehiclePrefabSet networkVehiclePrefabSetOverride,
        TrailColorPalette trailColorPaletteOverride)
    {
        if (IsMatchRunning)
            return;

        if (!EnsureMultiplayerSession(matchDefaultsOverride, matchRulesOverride, networkVehiclePrefabSetOverride, trailColorPaletteOverride))
            return;

        StartMatchAfterSessionCreated();
    }

    [Server]
    public void StartMatch(
        MatchSettings settingsOverride,
        MatchRules matchRulesOverride,
        VehiclePrefabSet networkVehiclePrefabSetOverride,
        TrailColorPalette trailColorPaletteOverride,
        Color selectedPlayerTrailColor)
    {
        if (IsMatchRunning)
            return;

        if (!EnsureMultiplayerSession(settingsOverride, matchRulesOverride, networkVehiclePrefabSetOverride, trailColorPaletteOverride, selectedPlayerTrailColor))
            return;

        StartMatchAfterSessionCreated();
    }

    [Server]
    private void StartMatchAfterSessionCreated()
    {
        GameSessionRuntime session = GameSessionBootstrap.CurrentSession;
        if (session == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot start match because no GameSessionRuntime exists after session creation.");
            return;
        }

        if (string.IsNullOrWhiteSpace(session.arenaSceneName))
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot start match because session arena scene name is empty.");
            return;
        }

        MultiplayerRuntimeBootstrap runtime = MultiplayerRuntimeBootstrap.Instance;
        if (runtime == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot start multiplayer arena load because MultiplayerRuntimeBootstrap.Instance is null.");
            return;
        }

        runtime.BeginServerArenaLoadAndInitialize(session.arenaSceneName);
    }

    [Server]
    private bool EnsureMultiplayerSession(
        MatchDefaults matchDefaultsOverride,
        MatchRules matchRulesOverride,
        VehiclePrefabSet networkVehiclePrefabSetOverride,
        TrailColorPalette trailColorPaletteOverride)
    {
        GameSessionRuntime currentSession = GameSessionBootstrap.CurrentSession;
        if (currentSession != null && currentSession.isSingleplayer)
        {
            Debug.LogWarning("[MultiplayerSessionDriver] Replacing existing singleplayer session before multiplayer match start.");
        }

        MatchDefaults activeMatchDefaults = matchDefaultsOverride != null ? matchDefaultsOverride : matchDefaults;
        MatchRules activeMatchRules = matchRulesOverride != null ? matchRulesOverride : matchRules;
        VehiclePrefabSet activeNetworkVehiclePrefabSet = networkVehiclePrefabSetOverride != null ? networkVehiclePrefabSetOverride : networkVehiclePrefabSet;
        TrailColorPalette activeTrailColorPalette = trailColorPaletteOverride != null ? trailColorPaletteOverride : trailColorPalette;

        if (activeMatchDefaults == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because MatchDefaults is not assigned.");
            return false;
        }

        if (activeMatchRules == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because MatchRules is not assigned.");
            return false;
        }

        if (activeNetworkVehiclePrefabSet == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because Network VehiclePrefabSet is not assigned.");
            return false;
        }

        if (activeTrailColorPalette == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because TrailColorPalette is not assigned.");
            return false;
        }

        int connectedHumanCount = CountConnectedHumans();
        MatchSettings settings = activeMatchDefaults.CreateSettings();
        settings.PlayerCount = connectedHumanCount;
        settings.BotCount = 0;
        settings = activeMatchRules.Validate(settings);
        settings.BotCount = 0;

        GameSessionRuntime session = GameSessionRuntime.FromSettings(
            settings,
            activeNetworkVehiclePrefabSet,
            activeTrailColorPalette,
            isSingleplayer: false,
            ResolveDefaultPlayerTrailColor(activeTrailColorPalette));

        GameSessionBootstrap.SetSession(session);
        return true;
    }

    [Server]
    private bool EnsureMultiplayerSession(
        MatchSettings settingsOverride,
        MatchRules matchRulesOverride,
        VehiclePrefabSet networkVehiclePrefabSetOverride,
        TrailColorPalette trailColorPaletteOverride,
        Color selectedPlayerTrailColor)
    {
        GameSessionRuntime currentSession = GameSessionBootstrap.CurrentSession;
        if (currentSession != null && currentSession.isSingleplayer)
        {
            Debug.LogWarning("[MultiplayerSessionDriver] Replacing existing singleplayer session before multiplayer match start.");
        }

        MatchRules activeMatchRules = matchRulesOverride != null ? matchRulesOverride : matchRules;
        VehiclePrefabSet activeNetworkVehiclePrefabSet = networkVehiclePrefabSetOverride != null
            ? networkVehiclePrefabSetOverride
            : networkVehiclePrefabSet;
        TrailColorPalette activeTrailColorPalette = trailColorPaletteOverride != null
            ? trailColorPaletteOverride
            : trailColorPalette;

        if (settingsOverride == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because MatchSettings override is null.");
            return false;
        }

        if (activeMatchRules == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because MatchRules is not assigned.");
            return false;
        }

        if (activeNetworkVehiclePrefabSet == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because Network VehiclePrefabSet is not assigned.");
            return false;
        }

        if (activeTrailColorPalette == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because TrailColorPalette is not assigned.");
            return false;
        }

        MatchSettings settings = CloneSettings(settingsOverride);
        if (settings == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because MatchSettings clone is null.");
            return false;
        }

        int connectedHumanCount = CountConnectedHumans();
        settings.PlayerCount = connectedHumanCount;
        settings.BotCount = 0;

        settings = activeMatchRules.Validate(settings);
        settings.PlayerCount = connectedHumanCount;
        settings.BotCount = 0;

        GameSessionRuntime session = GameSessionRuntime.FromSettings(
            settings,
            activeNetworkVehiclePrefabSet,
            activeTrailColorPalette,
            isSingleplayer: false,
            selectedPlayerTrailColor);

        if (session == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because GameSessionRuntime.FromSettings returned null.");
            return false;
        }

        GameSessionBootstrap.SetSession(session);

        return true;
    }

    private int CountConnectedHumans()
    {
        if (!InstanceFinder.IsServerStarted || InstanceFinder.ServerManager == null)
            return 1;

        int count = 0;
        foreach (var connection in InstanceFinder.ServerManager.Clients.Values)
        {
            if (connection != null && connection.IsAuthenticated)
                count++;
        }

        return Mathf.Max(count, 1);
    }

    private static int CountSessionBots(GameSessionRuntime session)
    {
        if (session == null)
            return 0;

        int count = 0;
        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry != null)
                count += Mathf.Max(0, entry.count);
        }

        return count;
    }

    private static string GetPrefabName(GameObject prefab)
    {
        return prefab != null ? prefab.name : "<none>";
    }

    private static Color ResolveDefaultPlayerTrailColor(TrailColorPalette activeTrailColorPalette)
    {
        return activeTrailColorPalette != null
            ? activeTrailColorPalette.GetDefaultColor(Color.white)
            : Color.white;
    }

    private static MatchSettings CloneSettings(MatchSettings source)
    {
        return source != null ? source.Clone() : null;
    }
}
