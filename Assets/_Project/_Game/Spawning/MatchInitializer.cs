using System;
using System.Collections;
using UnityEngine;

public class MatchInitializer : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Min(0f)] private float spawnInterval = 0.25f;
    [SerializeField, Min(1)] private int preMatchCountdownSeconds = 5;
    [SerializeField, Min(0f)] private float goDisplayDuration = 0.75f;
    [SerializeField] private GameStartTimer gameStartTimer;
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private EndGameController endGameController;

    [Header("Bot AI")]
    [SerializeField] private LayerMask botMapBoundaryMask;
    [SerializeField] private LayerMask botSuddenDeathMask;
    [SerializeField] private LayerMask botTrailMask;
    [SerializeField] private LayerMask botPowerupMask;
    [SerializeField] private Transform botMapCenter;

    public static event Action OnMatchStart;

    private bool _hasInitializationStarted;
    private bool _isFreezeOwned;

    private void Start()
    {
        // Singleplayer lobby creates a local session before loading the arena.
        // MultiplayerSessionDriver explicitly calls BeginMatchInitialization on the server after FishNet scene load.
        // Multiplayer clients must not auto-start this initializer because they do not own GameSessionRuntime.
        if (!GameSessionBootstrap.TryGetSession(out GameSessionRuntime session))
            return;

        if (!session.isSingleplayer)
            return;

        BeginMatchInitialization();
    }

    public void BeginMatchInitialization()
    {
        if (MultiplayerRuntimeMode.IsClientOnly)
        {
            Debug.Log("[MatchInitializer] Ignoring client-only BeginMatchInitialization. Server owns multiplayer initialization.");
            return;
        }

        if (_hasInitializationStarted)
            return;

        _hasInitializationStarted = true;
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        GameSessionRuntime session = ResolveSession();
        if (session == null)
        {
            Debug.LogError("Match initialization aborted: Runtime session is null.");
            yield break;
        }

        if (!TryValidateSceneReferences(session, out string sceneReferenceError))
        {
            Debug.LogError($"Match initialization aborted: {sceneReferenceError}");
            yield break;
        }

        MatchInitializationContext context = CreateContext(session);
        MatchSpawnPlanner spawnPlanner = new MatchSpawnPlanner(obstacleMask);
        BotSpawnFactory botSpawnFactory = new BotSpawnFactory(context);
        MatchSpawnService spawnService = new MatchSpawnService(spawnPlanner, botSpawnFactory);

        if (session.isSingleplayer)
        {
            if (!TryValidateSession(session, out string validationError))
            {
                Debug.LogError($"Match initialization aborted: {validationError}");
                yield break;
            }

            SingleplayerMatchFlow flow = new SingleplayerMatchFlow(context, spawnService, botSpawnFactory, SetFreeze, RaiseMatchStarted);
            yield return StartCoroutine(flow.Run());
            yield break;
        }

        if (!MultiplayerRuntimeMode.IsFishNetServerStarted)
            yield break;

        if (!TryValidateSession(session, out string multiplayerValidationError))
        {
            Debug.LogError($"Match initialization aborted: {multiplayerValidationError}");
            yield break;
        }

        MultiplayerMatchFlow multiplayerFlow = new MultiplayerMatchFlow(context, spawnService, botSpawnFactory, SetFreeze, RaiseMatchStarted);
        yield return StartCoroutine(multiplayerFlow.Run());
    }

    private MatchInitializationContext CreateContext(GameSessionRuntime session)
    {
        MatchSceneReferences sceneReferences = new MatchSceneReferences(
            gameStartTimer,
            gameTimer,
            endGameController,
            botMapCenter);

        return new MatchInitializationContext(
            this,
            session,
            sceneReferences,
            obstacleMask,
            spawnInterval,
            preMatchCountdownSeconds,
            goDisplayDuration,
            botMapBoundaryMask,
            botSuddenDeathMask,
            botTrailMask,
            botPowerupMask);
    }

    private GameSessionRuntime ResolveSession()
    {
        if (GameSessionBootstrap.TryGetSession(out var activeSession))
            return activeSession;

        Debug.LogError("MatchInitializer could not start because no GameSessionRuntime was provided. Start the game through SingleplayerLobby or MultiplayerLobby. For direct scene testing, add an explicit DevMatchBootstrap later.");
        return null;
    }

    private bool TryValidateSceneReferences(GameSessionRuntime session, out string error)
    {
        if (gameStartTimer == null)
        {
            error = "GameStartTimer is not assigned on MatchInitializer.";
            return false;
        }

        if (gameTimer == null)
        {
            error = "GameTimer is not assigned on MatchInitializer.";
            return false;
        }

        if (endGameController == null)
        {
            error = "EndGameController is not assigned on MatchInitializer.";
            return false;
        }

        if (!endGameController.HasGameTimer)
        {
            error = "GameTimer is not assigned on EndGameController.";
            return false;
        }

        if (SessionWillSpawnBots(session) && botMapCenter == null)
        {
            error = "BotMapCenter is not assigned on MatchInitializer, but this session will spawn bots.";
            return false;
        }

        if (session != null && session.isSuddenDeath && !endGameController.HasAreaBoundary)
        {
            error = "AreaBoundaryScript is not assigned on EndGameController, but sudden death is enabled.";
            return false;
        }

        error = null;
        return true;
    }

    private bool TryValidateSession(GameSessionRuntime session, out string error)
    {
        if (session == null)
        {
            error = "Runtime session is null.";
            return false;
        }

        if (session.playerPrefab == null)
        {
            error = "Player prefab is not configured.";
            return false;
        }

        if (session.maxPlayers < 2)
        {
            error = $"maxPlayers is invalid ({session.maxPlayers}).";
            return false;
        }

        int totalBots = 0;
        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry == null)
                continue;

            if (entry.prefab == null)
            {
                error = $"Bot entry at index {i} has no prefab.";
                return false;
            }

            if (entry.count < 0)
            {
                error = $"Bot entry at index {i} has negative count ({entry.count}).";
                return false;
            }

            totalBots += entry.count;
        }

        int totalPlayers = totalBots + 1;
        if (totalPlayers > session.maxPlayers)
        {
            error = $"Total participants ({totalPlayers}) exceed maxPlayers ({session.maxPlayers}).";
            return false;
        }

        error = null;
        return true;
    }

    private static bool SessionWillSpawnBots(GameSessionRuntime session)
    {
        if (session == null)
            return false;

        if (session.isSingleplayer && session.maxPlayers > 1)
            return true;

        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry != null && entry.count > 0)
                return true;
        }

        return false;
    }

    private void SetFreeze(bool freeze)
    {
        MultiplayerMatchState.SetFrozen(freeze);

        if (MultiplayerRuntimeMode.IsFishNetActive)
            return;

        if (freeze)
        {
            if (_isFreezeOwned)
                return;

            Time.timeScale = 0f;
            _isFreezeOwned = true;
            return;
        }

        if (!_isFreezeOwned)
            return;

        Time.timeScale = 1f;
        _isFreezeOwned = false;
    }

    private void RaiseMatchStarted()
    {
        OnMatchStart?.Invoke();
    }

    private void OnDisable()
    {
        SetFreeze(false);
        if (gameTimer != null)
            gameTimer.Hide();
    }
}
