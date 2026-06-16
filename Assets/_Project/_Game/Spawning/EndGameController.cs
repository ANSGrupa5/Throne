using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;

public class EndGameController : MonoBehaviour
{
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private AreaBoundaryScript areaBoundary;
    [SerializeField] private SceneReference gameOverScene;
    [SerializeField, Min(0.1f)] private float slowDownDuration = 1.5f;
    [SerializeField, Min(0f)] private float postFreezeDelay = 1f;
    [SerializeField, Min(0f)] private float finalTimescale = 0f;

    private GameSessionRuntime _session;
    private bool _isEnding;
    private bool _wasSubscribed;

    public bool HasGameTimer => gameTimer != null;
    public bool HasAreaBoundary => areaBoundary != null;

    private void OnEnable()
    {
        TryBindSession();
        BindEvents();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        if (_wasSubscribed)
            return;

        if (gameTimer != null)
            gameTimer.TimerEnded += HandleTimerEnded;

        VehicleLife.AnyVehicleDied += HandleVehicleDied;
        _wasSubscribed = true;
    }

    private void UnbindEvents()
    {
        if (!_wasSubscribed)
            return;

        if (gameTimer != null)
            gameTimer.TimerEnded -= HandleTimerEnded;

        VehicleLife.AnyVehicleDied -= HandleVehicleDied;
        _wasSubscribed = false;
    }

    private void TryBindSession()
    {
        if (GameSessionBootstrap.TryGetSession(out var session))
            _session = session;
    }

    private void HandleTimerEnded()
    {
        if (MultiplayerRuntimeMode.IsClientOnly)
            return;

        TryBindSession();
        if (_session == null)
        {
            Debug.LogError("[EndGameController] HandleTimerEnded: _session is null!");
            return;
        }

        if (_session.gameMode == GameSessionRuntime.KingOfTheHillMode && _session.isSuddenDeath)
        {
            if (areaBoundary == null)
            {
                Debug.LogWarning("[EndGameController] Sudden death requested, but no AreaBoundaryScript was found. Match will continue without shrinking.");
                return;
            }

            areaBoundary.isShrinking = true;
            return;
        }

        if (_session.gameMode != GameSessionRuntime.DeathmatchMode)
        {
            Debug.LogWarning($"[EndGameController] Skipping end sequence - gameMode is {_session.gameMode}, expected {GameSessionRuntime.DeathmatchMode}");
            return;
        }

        BeginEndSequence(GameOverPayload.EndReason.TimeUp);
    }

    private void HandleVehicleDied(VehicleLife victim, GameObject killer)
    {
        if (MultiplayerRuntimeMode.IsClientOnly)
            return;

        if (victim == null)
            return;

        TryBindSession();
        if (_session == null)
            return;

        VehicleLife killerLife = killer != null ? killer.GetComponent<VehicleLife>() : null;
        Color killerColor = GetVehicleColor(killer);
        if (killerLife != null)
        {
            GameSessionRuntime.PlayerMatchStats killerStats = _session.GetOrCreateStats(killerLife.OwnerId, killerLife.DisplayName, killerColor);
            killerStats.kills++;
        }

        Color victimColor = GetVehicleColor(victim.gameObject);
        GameSessionRuntime.PlayerMatchStats victimStats = _session.GetOrCreateStats(victim.OwnerId, victim.DisplayName, victimColor);
        victimStats.deaths++;

        if (_session.gameMode == GameSessionRuntime.KingOfTheHillMode && CountAliveVehicles() <= 1)
            BeginEndSequence(GameOverPayload.EndReason.LastAlive);
    }

    private int CountAliveVehicles()
    {
        VehicleLife[] vehicles = Object.FindObjectsByType<VehicleLife>(FindObjectsSortMode.None);
        int alive = 0;

        for (int i = 0; i < vehicles.Length; i++)
        {
            VehicleLife life = vehicles[i];
            if (life != null && !life.IsDead)
                alive++;
        }

        return alive;
    }

    public void BeginEndSequence(string reason)
    {
        BeginEndSequence(GameOverPayload.ParseReason(reason));
    }

    public void BeginEndSequence(GameOverPayload.EndReason reason)
    {
        bool isMultiplayerSession = IsMultiplayerSession();
        if (isMultiplayerSession && InstanceFinder.IsClientStarted && !InstanceFinder.IsServerStarted)
        {
            return;
        }

        if (_isEnding)
        {
            Debug.LogWarning("[EndGameController] Already ending, ignoring call");
            return;
        }

        TryBindSession();
        if (_session == null)
        {
            Debug.LogError("[EndGameController] No session found, aborting end sequence.");
            return;
        }

        _isEnding = true;
        if (gameTimer != null)
            gameTimer.StopTimer();

        if (isMultiplayerSession && InstanceFinder.IsServerStarted)
        {
            StartCoroutine(RunMultiplayerEndSequence(reason));
            return;
        }

        StartCoroutine(RunEndSequence(reason));
    }

    private IEnumerator RunEndSequence(GameOverPayload.EndReason reason)
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
        yield return new WaitForSecondsRealtime(postFreezeDelay);

        PrepareGameOverPayload(reason);
        Time.timeScale = 1f;

        StatsManager.Instance.IncDistDriven(DistanceTracker.Instance.GetTotalDistance());

        if (!TryGetGameOverSceneName(out string sceneName))
            yield break;

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private IEnumerator RunMultiplayerEndSequence(GameOverPayload.EndReason reason)
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
        yield return new WaitForSecondsRealtime(postFreezeDelay);

        MultiplayerMatchBroadcasts.EndGameResultSnapshot[] snapshots = BuildEndGameSnapshots(reason);

        MultiplayerMatchBroadcasts.SendEndGamePayload(reason.ToString(), snapshots);

        yield return null;
        yield return null;

        Time.timeScale = 1f;

        if (!TryGetGameOverSceneName(out string sceneName))
            yield break;

        SceneLoadData sceneLoadData = new(sceneName);
        sceneLoadData.ReplaceScenes = ReplaceOption.All;
        sceneLoadData.PreferredActiveScene = new PreferredScene(new SceneLookupData(sceneName));

        InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);
    }

    private bool TryGetGameOverSceneName(out string sceneName)
    {
        sceneName = gameOverScene != null ? gameOverScene.SceneName : string.Empty;

        if (!string.IsNullOrWhiteSpace(sceneName))
            return true;

        Debug.LogError("[EndGameController] Cannot load GameOver scene because gameOverScene is not assigned.");
        return false;
    }

    private void PrepareGameOverPayload(GameOverPayload.EndReason reason)
    {
        GameOverPayload.Clear();
        GameOverPayload.reason = reason;

        if (_session == null)
            return;

        EnsureStatsForSpawnedVehicles();

        for (int i = 0; i < _session.playerStats.Count; i++)
        {
            GameSessionRuntime.PlayerMatchStats stats = _session.playerStats[i];
            if (stats == null)
                continue;

            GameOverPayload.results.Add(new GameOverPayload.MatchResult
            {
                ownerId = stats.ownerId,
                displayName = stats.displayName,
                kills = stats.kills,
                deaths = stats.deaths,
                trailColor = stats.trailColor
            });
        }
    }

    private MultiplayerMatchBroadcasts.EndGameResultSnapshot[] BuildEndGameSnapshots(GameOverPayload.EndReason reason)
    {
        TryBindSession();

        if (_session == null)
        {
            Debug.LogError("[MultiplayerEndGame] Cannot build end-game payload because session is null.");
            return System.Array.Empty<MultiplayerMatchBroadcasts.EndGameResultSnapshot>();
        }

        EnsureStatsForSpawnedVehicles();

        List<MultiplayerMatchBroadcasts.EndGameResultSnapshot> results =
            new List<MultiplayerMatchBroadcasts.EndGameResultSnapshot>();

        for (int i = 0; i < _session.playerStats.Count; i++)
        {
            GameSessionRuntime.PlayerMatchStats stats = _session.playerStats[i];
            if (stats == null)
                continue;

            results.Add(new MultiplayerMatchBroadcasts.EndGameResultSnapshot
            {
                OwnerId = stats.ownerId,
                DisplayName = stats.displayName,
                Kills = stats.kills,
                Deaths = stats.deaths,
                TrailColor = stats.trailColor
            });
        }

        return results.ToArray();
    }

    private void EnsureStatsForSpawnedVehicles()
    {
        if (_session == null)
            return;

        VehicleLife[] vehicles = Object.FindObjectsByType<VehicleLife>(FindObjectsSortMode.None);
        for (int i = 0; i < vehicles.Length; i++)
        {
            VehicleLife life = vehicles[i];
            if (life == null)
                continue;

            Color color = GetVehicleColor(life.gameObject);
            GameSessionRuntime.PlayerMatchStats stats = _session.GetOrCreateStats(life.OwnerId, life.DisplayName, color);
            stats.trailColor = color;
        }
    }

    private Color GetVehicleColor(GameObject vehicle)
    {
        if (vehicle == null)
            return Color.white;

        VehicleColorApplier applier = vehicle.GetComponent<VehicleColorApplier>();
        if (applier != null)
            return TrailColorPalette.SanitizeColor(applier.GetColor(), Color.white);

        return Color.white;
    }

    private static bool IsMultiplayerSession()
    {
        return MultiplayerRuntimeMode.HasMultiplayerSessionOnThisPeer;
    }
}
