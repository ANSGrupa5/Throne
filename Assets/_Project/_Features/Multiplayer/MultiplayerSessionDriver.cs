using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

public sealed class MultiplayerSessionDriver : NetworkBehaviour
{
    public struct MatchResultSnapshot
    {
        public string OwnerId;
        public string DisplayName;
        public int Kills;
        public int Deaths;
        public Color TrailColor;
    }

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
        TrailColorPalette trailColorPaletteOverride)
    {
        if (IsMatchRunning)
            return;

        if (!EnsureMultiplayerSession(settingsOverride, matchRulesOverride, networkVehiclePrefabSetOverride, trailColorPaletteOverride))
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
        else if (currentSession != null)
        {
            Debug.Log("[MultiplayerSessionDriver] Refreshing existing multiplayer session before match start.");
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
            isSingleplayer: false);

        GameSessionBootstrap.SetSession(session);

        Debug.Log(
            $"[MultiplayerSessionDriver] Created multiplayer session: isSingleplayer={session.isSingleplayer}, " +
            $"arena='{session.arenaSceneName}', maxPlayers={session.maxPlayers}, connectedHumans={connectedHumanCount}, " +
            $"bots={CountSessionBots(session)}, playerPrefab='{GetPrefabName(session.playerPrefab)}'.");
        return true;
    }

    [Server]
    private bool EnsureMultiplayerSession(
        MatchSettings settingsOverride,
        MatchRules matchRulesOverride,
        VehiclePrefabSet networkVehiclePrefabSetOverride,
        TrailColorPalette trailColorPaletteOverride)
    {
        GameSessionRuntime currentSession = GameSessionBootstrap.CurrentSession;
        if (currentSession != null && currentSession.isSingleplayer)
        {
            Debug.LogWarning("[MultiplayerSessionDriver] Replacing existing singleplayer session before multiplayer match start.");
        }
        else if (currentSession != null)
        {
            Debug.Log("[MultiplayerSessionDriver] Refreshing existing multiplayer session before match start.");
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
            isSingleplayer: false);

        if (session == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because GameSessionRuntime.FromSettings returned null.");
            return false;
        }

        GameSessionBootstrap.SetSession(session);

        Debug.Log(
            $"[MultiplayerSessionDriver] Created multiplayer session from lobby settings: " +
            $"isSingleplayer={session.isSingleplayer}, arena='{session.arenaSceneName}', " +
            $"maxPlayers={session.maxPlayers}, connectedHumans={connectedHumanCount}, " +
            $"bots={CountSessionBots(session)}, mode={session.gameMode}, duration={session.matchDuration}, " +
            $"trailLength={session.trailLength}, playerPrefab='{GetPrefabName(session.playerPrefab)}'.");

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

    private static MatchSettings CloneSettings(MatchSettings source)
    {
        return source != null ? source.Clone() : null;
    }

    [Server]
    public IEnumerator RunMatchStartSequence(int seconds, float goDuration, GameStartTimer startTimer, GameTimer timer, float matchDuration)
    {
        IsMatchRunning = true;
        BroadcastCountdownStarted(seconds);

        for (int i = seconds - 1; i > 0; i--)
        {
            yield return new WaitForSecondsRealtime(1f);
            BroadcastCountdownValue(i);
        }

        yield return new WaitForSecondsRealtime(1f);
        BroadcastCountdownGo();
        yield return new WaitForSecondsRealtime(goDuration);

        BroadcastCountdownEnded();
        BroadcastMatchTimerStarted(matchDuration);
    }

    [Server]
    public void BroadcastCountdownStarted(float duration)
    {
        ObserversCountdownStarted(duration);
    }

    [Server]
    public void BroadcastCountdownValue(int value)
    {
        ObserversCountdownValue(value);
    }

    [Server]
    public void BroadcastCountdownEnded()
    {
        ObserversCountdownEnded();
    }

    [Server]
    private void BroadcastCountdownGo()
    {
        ObserversCountdownGo();
    }

    [Server]
    private void BroadcastMatchTimerStarted(float matchDuration)
    {
        ObserversMatchTimerStarted(matchDuration);
    }

    [Server]
    public void BeginNetworkEndSequence(string reason, float slowDownDuration, float postFreezeDelay, float finalTimescale, string gameOverSceneName)
    {
        StartCoroutine(RunNetworkEndSequence(reason, slowDownDuration, postFreezeDelay, finalTimescale, gameOverSceneName));
    }

    [Server]
    private IEnumerator RunNetworkEndSequence(string reason, float slowDownDuration, float postFreezeDelay, float finalTimescale, string gameOverSceneName)
    {
        RpcPlayEndSequence(slowDownDuration, finalTimescale);
        yield return new WaitForSecondsRealtime(slowDownDuration + postFreezeDelay);

        List<MatchResultSnapshot> results = BuildResults();
        RpcPrepareGameOver(reason, results.ToArray());

        SceneLoadData sceneLoadData = new(gameOverSceneName);
        sceneLoadData.ReplaceScenes = ReplaceOption.All;
        sceneLoadData.PreferredActiveScene = new PreferredScene(new SceneLookupData(gameOverSceneName));
        InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);
    }

    private List<MatchResultSnapshot> BuildResults()
    {
        List<MatchResultSnapshot> results = new();
        GameSessionRuntime session = GameSessionBootstrap.CurrentSession;
        if (session == null)
            return results;

        for (int i = 0; i < session.playerStats.Count; i++)
        {
            GameSessionRuntime.PlayerMatchStats stats = session.playerStats[i];
            if (stats == null)
                continue;

            results.Add(new MatchResultSnapshot
            {
                OwnerId = stats.ownerId,
                DisplayName = stats.displayName,
                Kills = stats.kills,
                Deaths = stats.deaths,
                TrailColor = stats.trailColor
            });
        }

        return results;
    }

    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void ObserversCountdownStarted(float duration)
    {
        int value = Mathf.CeilToInt(duration);
        MultiplayerMatchState.SetFrozen(true);
        MultiplayerMatchState.SetCountdownCount(value);
        MultiplayerHudBridge.ApplyCountdownNow("RpcShowCount");

        Debug.Log($"[MultiplayerCountdown] RpcShowCount({value}) on peer.");
    }

    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void ObserversCountdownValue(int value)
    {
        MultiplayerMatchState.SetCountdownCount(value);
        MultiplayerHudBridge.ApplyCountdownNow("RpcShowCount");

        Debug.Log($"[MultiplayerCountdown] RpcShowCount({value}) on peer.");
    }

    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void ObserversCountdownGo()
    {
        MultiplayerMatchState.SetCountdownGo();
        MultiplayerHudBridge.ApplyCountdownNow("RpcShowGo");

        Debug.Log("[MultiplayerCountdown] RpcShowGo on peer.");
    }

    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void ObserversCountdownEnded()
    {
        MultiplayerMatchState.HideCountdown();
        MultiplayerMatchState.SetFrozen(false);
        MultiplayerHudBridge.ApplyCountdownNow("RpcHideCountdown");

        Debug.Log("[MultiplayerCountdown] RpcHideCountdown on peer.");
    }

    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void ObserversMatchTimerStarted(float matchDuration)
    {
        MultiplayerMatchState.BeginTimer(matchDuration);
        MultiplayerHudBridge.ApplyTimerNow("RpcBeginTimer");

        Debug.Log($"[MultiplayerCountdown] RpcBeginTimer({matchDuration}) on peer.");
    }

    [ObserversRpc(RunLocally = true)]
    private void RpcPlayEndSequence(float slowDownDuration, float finalTimescale)
    {
        StartCoroutine(PlayEndSequenceLocally(slowDownDuration, finalTimescale));
    }

    [ObserversRpc(RunLocally = true)]
    private void RpcPrepareGameOver(string reason, MatchResultSnapshot[] results)
    {
        GameOverPayload.Clear();
        GameOverPayload.reason = GameOverPayload.ParseReason(reason);

        if (results == null)
            return;

        for (int i = 0; i < results.Length; i++)
        {
            MatchResultSnapshot result = results[i];
            GameOverPayload.results.Add(new GameOverPayload.MatchResult
            {
                ownerId = result.OwnerId,
                displayName = result.DisplayName,
                kills = result.Kills,
                deaths = result.Deaths,
                trailColor = result.TrailColor
            });
        }
    }

    private IEnumerator PlayEndSequenceLocally(float slowDownDuration, float finalTimescale)
    {
        float initialTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        float elapsed = 0f;

        while (elapsed < slowDownDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slowDownDuration);
            Time.timeScale = Mathf.Lerp(initialTimeScale, finalTimescale, t);
            yield return null;
        }

        Time.timeScale = finalTimescale;
    }
}
