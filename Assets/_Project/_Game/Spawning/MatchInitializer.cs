using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class MatchInitializer : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Fallback assets for direct scene testing when no runtime session exists.")]
    [SerializeField] private BotsSettings botsSettings;
    [Tooltip("Fallback assets for direct scene testing when no runtime session exists.")]
    [SerializeField] private GameSettings gameSettings;
    [Tooltip("Fallback assets for direct scene testing when no runtime session exists.")]
    [SerializeField] private PlayerLook playerLook;

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

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private bool _hasInitializationStarted;
    private bool _isFreezeOwned;

    [Obsolete]
    private void Awake()
    {
        BindSceneReferences();
    }

    private void Start()
    {
        if (MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return;

        BeginMatchInitialization();
    }

    public void BeginMatchInitialization()
    {
        if (_hasInitializationStarted)
            return;

        _hasInitializationStarted = true;
        StartCoroutine(InitializeRoutine());
    }

    [Obsolete]
    private void BindSceneReferences()
    {
        if (endGameController == null)
            endGameController = FindObjectOfType<EndGameController>();

        if (endGameController == null)
            return;

        AreaBoundaryScript areaBoundary = FindObjectOfType<AreaBoundaryScript>();
        if (areaBoundary != null)
            endGameController.SetAreaBoundary(areaBoundary);
    }

    private IEnumerator InitializeRoutine()
    {
        GameSessionRuntime session = ResolveSession();
        if (session == null)
        {
            Debug.LogError("Match initialization aborted: Runtime session is null.");
            yield break;
        }

        if (session.isSingleplayer)
        {
            yield return StartCoroutine(InitializeSingleplayer(session));
            yield break;
        }

        yield return StartCoroutine(InitializeMultiplayer(session));
    }

    private IEnumerator InitializeSingleplayer(GameSessionRuntime session)
    {
        if (!TryValidateSession(session, out string validationError))
        {
            Debug.LogError($"Match initialization aborted: {validationError}");
            yield break;
        }

        EnsureBotLooks(session);
        int totalHumanPlayers = 1;
        int totalBots = Mathf.Max(0, session.maxPlayers - totalHumanPlayers);
        int totalToSpawn = totalHumanPlayers + totalBots;

        if (!TrySelectSpawnSpots(totalToSpawn, out List<SpawnSpot> chosen))
            yield break;

        int index = 0;
        SetFreeze(true);

        SpawnLocalAt(session, session.playerPrefab, chosen[index], session.playerDisplayName, session.playerOwnerId, session.playerTrailColor, false);
        index++;
        StatsManager.Instance.GetPlayerPrefab(session.playerDisplayName);
        //Temporary disable: DistanceTracker uses string to find player, unsafe.
        //DistanceTracker.Instance.GetTarget();
        yield return new WaitForSecondsRealtime(spawnInterval);

        for (int i = 0; i < totalBots; i++)
        {
            if (index >= chosen.Count) break;

            PlayerLook botLook = i < session.botLooks.Count ? session.botLooks[i] : CreateFallbackBotLook(session, i);
            SpawnLocalAt(session, botLook.playerPrefab, chosen[index], botLook.displayName, botLook.ownerId, botLook.trailColor, true);
            index++;
            yield return new WaitForSecondsRealtime(spawnInterval);
        }

        yield return null;
        yield return StartCoroutine(CountdownAndStart(preMatchCountdownSeconds));
        if (gameTimer != null)
            gameTimer.Begin(session.matchDuration);

        SetFreeze(false);
        OnMatchStart?.Invoke();
    }

    private IEnumerator InitializeMultiplayer(GameSessionRuntime session)
    {
        if (InstanceFinder.IsClientStarted && !InstanceFinder.IsServerStarted)
            yield break;

        if (!TryValidateSession(session, out string validationError))
        {
            Debug.LogError($"Match initialization aborted: {validationError}");
            yield break;
        }

        List<NetworkConnection> players = GetConnectedPlayers();
        int totalHumanPlayers = Mathf.Max(1, players.Count);
        EnsureBotLooks(session);
        int totalBots = Mathf.Max(0, session.maxPlayers - totalHumanPlayers);
        int totalToSpawn = totalHumanPlayers + totalBots;

        if (!TrySelectSpawnSpots(totalToSpawn, out List<SpawnSpot> chosen))
            yield break;

        int index = 0;
        SetFreeze(true);

        for (int playerIndex = 0; playerIndex < totalHumanPlayers; playerIndex++)
        {
            NetworkConnection ownerConnection = playerIndex < players.Count ? players[playerIndex] : null;
            string ownerId = ownerConnection != null ? $"player_{ownerConnection.ClientId}" : session.playerOwnerId;
            string displayName = playerIndex == 0 ? session.playerDisplayName : $"Player {playerIndex + 1}";
            Color trailColor = ResolvePlayerColor(session, playerIndex);
            SpawnNetworkAt(session, session.playerPrefab, chosen[index], displayName, ownerId, trailColor, false, ownerConnection);
            index++;
            StatsManager.Instance.GetPlayerPrefab(session.playerDisplayName);
            //Temporary disable: DistanceTracker uses string to find player, unsafe.
            //DistanceTracker.Instance.GetTarget();
            yield return new WaitForSecondsRealtime(spawnInterval);
        }

        for (int i = 0; i < totalBots; i++)
        {
            if (index >= chosen.Count) break;

            PlayerLook botLook = i < session.botLooks.Count ? session.botLooks[i] : CreateFallbackBotLook(session, i);
            SpawnNetworkAt(session, botLook.playerPrefab, chosen[index], botLook.displayName, botLook.ownerId, botLook.trailColor, true, null);
            index++;
            yield return new WaitForSecondsRealtime(spawnInterval);
        }

        yield return null;

        if (MultiplayerSessionDriver.Instance != null && InstanceFinder.IsServerStarted)
        {
            yield return StartCoroutine(MultiplayerSessionDriver.Instance.RunMatchStartSequence(
                preMatchCountdownSeconds,
                goDisplayDuration,
                gameStartTimer,
                gameTimer,
                session.matchDuration));
        }
        else
        {
            yield return StartCoroutine(CountdownAndStart(preMatchCountdownSeconds));
            if (gameTimer != null)
                gameTimer.Begin(session.matchDuration);
        }

        SetFreeze(false);
        OnMatchStart?.Invoke();
    }

    private GameObject SpawnLocalAt(GameSessionRuntime session, GameObject prefab, SpawnSpot spot, string displayName, string ownerId, Color trailColor, bool isBot)
    {
        if (prefab == null || spot == null) return null;

        Vector3 pos = spot.Position;
        Quaternion rot = spot.Rotation;
        GameObject go = Instantiate(prefab, pos, rot);
        _spawned.Add(go);

        if (!ConfigureSpawnedVehicle(session, go, prefab, pos, rot, displayName, ownerId, trailColor, isBot))
            return null;

        return go;
    }

    private GameObject SpawnNetworkAt(GameSessionRuntime session, GameObject prefab, SpawnSpot spot, string displayName, string ownerId, Color trailColor, bool isBot, NetworkConnection ownerConnection)
    {
        if (prefab == null || spot == null) return null;

        Vector3 pos = spot.Position;
        Quaternion rot = spot.Rotation;
        GameObject go = Instantiate(prefab, pos, rot);
        _spawned.Add(go);

        if (!ConfigureSpawnedVehicle(session, go, prefab, pos, rot, displayName, ownerId, trailColor, isBot))
            return null;

        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        if (networkObject != null && InstanceFinder.IsServerStarted)
            InstanceFinder.ServerManager.Spawn(networkObject, ownerConnection);

        return go;
    }

    private bool ConfigureSpawnedVehicle(GameSessionRuntime session, GameObject go, GameObject prefab, Vector3 pos, Quaternion rot, string displayName, string ownerId, Color trailColor, bool isBot)
    {
        VehicleColorApplier colorApplier = go.GetComponent<VehicleColorApplier>();
        colorApplier.SetColor(trailColor);

        VehicleLife life = go.GetComponent<VehicleLife>();
        if (life == null)
        {
            Debug.LogError($"Match initialization aborted: spawned prefab '{prefab.name}' has no VehicleLife component.");
            Destroy(go);
            _spawned.Remove(go);
            return false;
        }

        life.ConfigureSpawn(pos, rot);
        life.ConfigureIdentity(displayName, ownerId);
        session.GetOrCreateStats(ownerId, displayName, trailColor);

        TrailEmitter trailEmitter = go.GetComponent<TrailEmitter>();
        trailEmitter.Configure(life, trailColor, session != null ? session.trailLength : 1);

        if (isBot)
        {
            BotVehicleInput botInput = go.GetComponent<BotVehicleInput>();
            if (botInput != null)
                botInput.ConfigureRuntime(botMapBoundaryMask, botSuddenDeathMask, botTrailMask, botPowerupMask, botMapCenter);
        }

        return true;
    }

    private bool TrySelectSpawnSpots(int totalToSpawn, out List<SpawnSpot> chosen)
    {
        chosen = null;

        var spots = SpawnSpot.Active.ToList();
        if (spots.Count == 0)
        {
            Debug.LogWarning("No SpawnSpots found in scene.");
            return false;
        }

        if (totalToSpawn > spots.Count)
        {
            Debug.LogError($"Match initialization aborted: requested {totalToSpawn} entities but only {spots.Count} SpawnSpots are available.");
            return false;
        }

        chosen = SelectSpawnSpots(spots, totalToSpawn);
        if (chosen.Count < totalToSpawn)
        {
            Debug.LogError($"Match initialization aborted: could not reserve enough SpawnSpots ({chosen.Count}/{totalToSpawn}).");
            return false;
        }

        return true;
    }

    private List<NetworkConnection> GetConnectedPlayers()
    {
        if (!InstanceFinder.IsServerStarted || InstanceFinder.ServerManager == null)
            return new List<NetworkConnection>();

        return InstanceFinder.ServerManager.Clients.Values
            .Where(connection => connection != null && connection.IsAuthenticated)
            .OrderBy(connection => connection.ClientId)
            .ToList();
    }

    private Color ResolvePlayerColor(GameSessionRuntime session, int playerIndex)
    {
        if (session == null || session.trailColorPalette == null || session.trailColorPalette.Count == 0)
            return Color.white;

        if (playerIndex == 0)
            return session.playerTrailColor;

        return session.trailColorPalette[playerIndex % session.trailColorPalette.Count];
    }

    private PlayerLook CreateFallbackBotLook(GameSessionRuntime session, int botIndex)
    {
        PlayerLook look = ScriptableObject.CreateInstance<PlayerLook>();
        look.hideFlags = HideFlags.DontSave;
        look.playerPrefab = session.botDefaultPrefab != null ? session.botDefaultPrefab : session.playerPrefab;
        look.displayName = $"BOT{botIndex + 1}";
        look.ownerId = $"bot_{botIndex + 1}";
        look.trailColor = ResolvePlayerColor(session, botIndex + 1);
        return look;
    }

    private void EnsureBotLooks(GameSessionRuntime session)
    {
        if (session == null)
            return;

        if (session.botLooks.Count > 0)
            return;

        List<GameObject> prefabs = new List<GameObject>();
        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry == null || entry.prefab == null || entry.count <= 0)
                continue;

            for (int repeat = 0; repeat < entry.count; repeat++)
                prefabs.Add(entry.prefab);
        }

        if (prefabs.Count == 0)
            return;

        GameObject defaultBotPrefab = session.botDefaultPrefab;
        if (defaultBotPrefab == null)
            defaultBotPrefab = prefabs[0];

        List<Color> availableColors = new List<Color>();
        for (int i = 0; i < session.trailColorPalette.Count; i++)
        {
            Color color = session.trailColorPalette[i];
            if (color == session.playerTrailColor)
                continue;

            availableColors.Add(color);
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            Color color = availableColors.Count > 0
                ? PickAndRemoveColor(availableColors)
                : (session.trailColorPalette.Count > 0
                    ? session.trailColorPalette[UnityEngine.Random.Range(0, session.trailColorPalette.Count)]
                    : Color.white);

            PlayerLook look = ScriptableObject.CreateInstance<PlayerLook>();
            look.hideFlags = HideFlags.DontSave;
            look.playerPrefab = defaultBotPrefab;
            look.displayName = $"BOT{i + 1}";
            look.ownerId = $"bot_{i + 1}";
            look.trailColor = color;
            session.botLooks.Add(look);
        }
    }

    private Color PickAndRemoveColor(List<Color> availableColors)
    {
        int index = UnityEngine.Random.Range(0, availableColors.Count);
        Color selected = availableColors[index];
        availableColors.RemoveAt(index);
        return selected;
    }

    private GameSessionRuntime ResolveSession()
    {
        if (GameSessionBootstrap.TryGetSession(out var activeSession))
            return activeSession;

        Debug.LogWarning("No runtime session found. Falling back to default ScriptableObject assets.");
        GameSessionRuntime fallbackSession = GameSessionRuntime.FromDefaults(gameSettings, botsSettings, playerLook);
        if (MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            fallbackSession.isSingleplayer = false;

        GameSessionBootstrap.SetSession(fallbackSession);
        return fallbackSession;
    }

    private List<SpawnSpot> SelectSpawnSpots(List<SpawnSpot> available, int count)
    {
        List<SpawnSpot> candidates = new List<SpawnSpot>(available.Where(s => s.IsAvailable));
        List<SpawnSpot> result = new List<SpawnSpot>();

        if (count <= 0 || candidates.Count == 0) return result;

        // Filter by clear spots first
        candidates = candidates.Where(s => s.IsClear(obstacleMask)).ToList();

        if (candidates.Count == 0)
        {
            // fallback to any available
            candidates = new List<SpawnSpot>(available.Where(s => s.IsAvailable));
        }

        if (count >= candidates.Count)
        {
            result.AddRange(candidates);
            return result;
        }

        // Anti-clump selection: pick first random, then pick spots that maximize distance to existing selection
        System.Random rng = new System.Random();
        int firstIndex = rng.Next(0, candidates.Count);
        result.Add(candidates[firstIndex]);
        candidates.RemoveAt(firstIndex);

        while (result.Count < count && candidates.Count > 0)
        {
            SpawnSpot best = null;
            float bestMinDist = -1f;
            foreach (var c in candidates)
            {
                float minDist = float.MaxValue;
                foreach (var chosen in result)
                {
                    float d = Vector3.SqrMagnitude(c.Position - chosen.Position);
                    if (d < minDist) minDist = d;
                }
                if (minDist > bestMinDist)
                {
                    bestMinDist = minDist;
                    best = c;
                }
            }
            if (best == null) break;
            result.Add(best);
            candidates.Remove(best);
        }

        return result;
    }

    private IEnumerator CountdownAndStart(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            Debug.Log(i);
            if (gameStartTimer != null)
                gameStartTimer.ShowCount(i);
            yield return new WaitForSecondsRealtime(1f);
        }

        Debug.Log("GO");
        if (gameStartTimer != null)
            gameStartTimer.ShowGo();
        yield return new WaitForSecondsRealtime(goDisplayDuration);
        if (gameStartTimer != null)
            gameStartTimer.Hide();
        yield break;
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

    private void SetFreeze(bool freeze)
    {
        MultiplayerMatchState.SetFrozen(freeze);

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

    private void OnDisable()
    {
        SetFreeze(false);
        if (gameTimer != null)
            gameTimer.Hide();
    }
}
