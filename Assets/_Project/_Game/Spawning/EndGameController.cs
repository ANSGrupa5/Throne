using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameController : MonoBehaviour
{
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private AreaBoundaryScript areaBoundary;
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField, Min(0.1f)] private float slowDownDuration = 1.5f;
    [SerializeField, Min(0f)] private float postFreezeDelay = 1f;
    [SerializeField, Min(0f)] private float finalTimescale = 0f;

    private GameSessionRuntime _session;
    private bool _isEnding;
    private bool _wasSubscribed;

    private void Awake()
    {
        if (gameTimer == null)
            gameTimer = GetComponent<GameTimer>();

        TryBindAreaBoundary();
    }

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

    private void TryBindAreaBoundary()
    {
        if (areaBoundary != null)
            return;

        areaBoundary = Object.FindAnyObjectByType<AreaBoundaryScript>();
    }

    private void HandleTimerEnded()
    {
        if (MultiplayerRuntimeMode.IsClientOnly)
            return;

        Debug.Log("[EndGameController] Timer ended!");
        TryBindSession();
        if (_session == null)
        {
            Debug.LogError("[EndGameController] HandleTimerEnded: _session is null!");
            return;
        }

        Debug.Log($"[EndGameController] gameMode: {_session.gameMode}");

        if (_session.gameMode == 0 && _session.isSuddenDeath)
        {
            TryBindAreaBoundary();
            if (areaBoundary == null)
            {
                Debug.LogWarning("[EndGameController] Sudden death requested, but no AreaBoundaryScript was found. Match will continue without shrinking.");
                return;
            }

            Debug.Log("[EndGameController] Sudden death triggered - enabling arena shrink.");
            areaBoundary.isShrinking = true;
            return;
        }

        if (_session.gameMode != 1)
        {
            Debug.LogWarning($"[EndGameController] Skipping end sequence - gameMode is {_session.gameMode}, expected 1");
            return;
        }

        Debug.Log($"[EndGameController] Calling BeginEndSequence with reason: {GameOverPayload.EndReason.TimeUp}");
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

        if (_session.gameMode == 0 && CountAliveVehicles() <= 1)
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
        Debug.Log($"[EndGameController] BeginEndSequence called with reason: {reason}, _isEnding: {_isEnding}");
        if (MultiplayerRuntimeMode.IsClientOnly)
        {
            Debug.Log("[EndGameController] Ignoring client-only BeginEndSequence. Server owns multiplayer end sequence.");
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
        Debug.Log("[EndGameController] _isEnding set to true");
        if (gameTimer != null)
            gameTimer.StopTimer();

        if (IsMultiplayerSession() && MultiplayerRuntimeMode.IsFishNetActive && MultiplayerSessionDriver.Instance != null)
        {
            MultiplayerSessionDriver.Instance.BeginNetworkEndSequence(reason.ToString(), slowDownDuration, postFreezeDelay, finalTimescale, gameOverSceneName);
            return;
        }

        Debug.Log("[EndGameController] Starting RunEndSequence coroutine");
        StartCoroutine(RunEndSequence(reason));
    }

    public void SetAreaBoundary(AreaBoundaryScript boundary)
    {
        areaBoundary = boundary;
    }

    private IEnumerator RunEndSequence(GameOverPayload.EndReason reason)
    {
        Debug.Log("[EndGameController] RunEndSequence started");
        float initialTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        float elapsed = 0f;

        Debug.Log($"[EndGameController] Starting slow-down from {initialTimeScale} to {finalTimescale} over {slowDownDuration}s");
        while (elapsed < slowDownDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slowDownDuration);
            Time.timeScale = Mathf.Lerp(initialTimeScale, finalTimescale, t);
            yield return null;
        }

        Time.timeScale = finalTimescale;
        Debug.Log($"[EndGameController] Slow-down complete. Waiting {postFreezeDelay}s before loading scene");
        yield return new WaitForSecondsRealtime(postFreezeDelay);

        PrepareGameOverPayload(reason);
        Debug.Log($"[EndGameController] Loaded game over payload. Loading scene: {gameOverSceneName}");
        Time.timeScale = 1f;

        StatsManager.Instance.IncDistDriven(DistanceTracker.Instance.GetTotalDistance());

        SceneManager.LoadScene(gameOverSceneName);
        Debug.Log("[EndGameController] SceneManager.LoadScene called - sequence complete");
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
