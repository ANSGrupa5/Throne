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
        MultiplayerMatchState.SetFrozen(false);
    }

    [Server]
    public void StartMatch()
    {
        if (IsMatchRunning)
            return;

        if (!EnsureMultiplayerSession())
            return;

        MatchInitializer initializer = FindFirstObjectByType<MatchInitializer>();
        if (initializer == null)
        {
            Debug.LogWarning("Unable to start multiplayer match because no MatchInitializer was found in the active scene.");
            return;
        }

        initializer.BeginMatchInitialization();
    }

    [Server]
    private bool EnsureMultiplayerSession()
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

        if (matchDefaults == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because MatchDefaults is not assigned.");
            return false;
        }

        if (matchRules == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because MatchRules is not assigned.");
            return false;
        }

        if (networkVehiclePrefabSet == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because Network VehiclePrefabSet is not assigned.");
            return false;
        }

        if (trailColorPalette == null)
        {
            Debug.LogError("[MultiplayerSessionDriver] Cannot create multiplayer session because TrailColorPalette is not assigned.");
            return false;
        }

        int connectedHumanCount = CountConnectedHumans();
        MatchSettings settings = matchDefaults.CreateSettings();
        settings.PlayerCount = connectedHumanCount;
        settings.BotCount = 0;
        settings = matchRules.Validate(settings);

        GameSessionRuntime session = GameSessionRuntime.FromSettings(
            settings,
            networkVehiclePrefabSet,
            trailColorPalette,
            isSingleplayer: false);

        GameSessionBootstrap.SetSession(session);

        Debug.Log(
            $"[MultiplayerSessionDriver] Created multiplayer session: isSingleplayer={session.isSingleplayer}, " +
            $"arena='{session.arenaSceneName}', maxPlayers={session.maxPlayers}, connectedHumans={connectedHumanCount}, " +
            $"bots={CountSessionBots(session)}, playerPrefab='{GetPrefabName(session.playerPrefab)}'.");
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

    [Server]
    public IEnumerator RunMatchStartSequence(int seconds, float goDuration, GameStartTimer startTimer, GameTimer timer, float matchDuration)
    {
        IsMatchRunning = true;
        MultiplayerMatchState.SetFrozen(true);
        RpcSetFrozen(true);

        for (int i = seconds; i > 0; i--)
        {
            if (startTimer != null)
                startTimer.ShowCount(i);

            RpcShowCount(i);
            yield return new WaitForSecondsRealtime(1f);
        }

        if (startTimer != null)
            startTimer.ShowGo();

        RpcShowGo();
        yield return new WaitForSecondsRealtime(goDuration);

        if (startTimer != null)
            startTimer.Hide();
        if (timer != null)
            timer.Begin(matchDuration);

        RpcHideCountdown();
        RpcBeginTimer(matchDuration);
        RpcSetFrozen(false);
        MultiplayerMatchState.SetFrozen(false);
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

    [ObserversRpc]
    private void RpcShowCount(int seconds)
    {
        if (IsServerInitialized)
            return;

        GameStartTimer startTimer = FindFirstObjectByType<GameStartTimer>(FindObjectsInactive.Include);
        if (startTimer != null)
            startTimer.ShowCount(seconds);
    }

    [ObserversRpc]
    private void RpcShowGo()
    {
        if (IsServerInitialized)
            return;

        GameStartTimer startTimer = FindFirstObjectByType<GameStartTimer>(FindObjectsInactive.Include);
        if (startTimer != null)
            startTimer.ShowGo();
    }

    [ObserversRpc]
    private void RpcHideCountdown()
    {
        if (IsServerInitialized)
            return;

        GameStartTimer startTimer = FindFirstObjectByType<GameStartTimer>(FindObjectsInactive.Include);
        if (startTimer != null)
            startTimer.Hide();
    }

    [ObserversRpc]
    private void RpcBeginTimer(float matchDuration)
    {
        if (IsServerInitialized)
            return;

        GameTimer timer = FindFirstObjectByType<GameTimer>(FindObjectsInactive.Include);
        if (timer != null)
            timer.Begin(matchDuration);
    }

    [ObserversRpc(RunLocally = true)]
    private void RpcSetFrozen(bool frozen)
    {
        MultiplayerMatchState.SetFrozen(frozen);
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
        GameOverPayload.reason = reason;

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
