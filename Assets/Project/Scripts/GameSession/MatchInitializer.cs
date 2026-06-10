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
    [SerializeField, Min(1)] private int preMatchCountdownSeconds = 3;
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
        if (ShouldWaitForNetworkMatchStart())
            return;

        BeginMatchInitialization();
    }

    private bool ShouldWaitForNetworkMatchStart()
    {
        if (GameSessionBootstrap.TryGetSession(out GameSessionRuntime session))
            return !session.isSingleplayer;

        if (MultiplayerRuntimeBootstrap.IsActiveMultiplayerScene())
            return true;

        return InstanceFinder.IsClientStarted || InstanceFinder.IsServerStarted;
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

        if (!session.isSingleplayer && InstanceFinder.IsClientStarted && !InstanceFinder.IsServerStarted)
            yield break;

        if (!TryValidateSession(session, out string validationError))
        {
            Debug.LogError($"Match initialization aborted: {validationError}");
            yield break;
        }

        var spots = SpawnSpot.Active.ToList();
        if (spots.Count == 0)
        {
            Debug.LogWarning("No SpawnSpots found in scene.");
            yield break;
        }

        List<NetworkConnection> players = session.isSingleplayer
            ? new List<NetworkConnection>()
            : GetConnectedPlayers();
        int totalHumanPlayers = Mathf.Max(1, players.Count);
        EnsureBotLooks(session);
        int totalBots = GetRequestedBotCount(session, totalHumanPlayers);

        int totalToSpawn = totalBots + totalHumanPlayers;

        if (totalToSpawn > spots.Count)
        {
            Debug.LogError($"Match initialization aborted: requested {totalToSpawn} entities but only {spots.Count} SpawnSpots are available.");
            yield break;
        }

        List<SpawnSpot> chosen = SelectSpawnSpots(spots, totalToSpawn);
        if (chosen.Count < totalToSpawn)
        {
            Debug.LogError($"Match initialization aborted: could not reserve enough SpawnSpots ({chosen.Count}/{totalToSpawn}).");
            yield break;
        }

        int index = 0;
        SetFreeze(true);

        for (int playerIndex = 0; playerIndex < totalHumanPlayers; playerIndex++)
        {
            NetworkConnection ownerConnection = playerIndex < players.Count ? players[playerIndex] : null;
            string ownerId = ownerConnection != null ? $"player_{ownerConnection.ClientId}" : session.playerOwnerId;
            string displayName = playerIndex == 0 ? session.playerDisplayName : $"Player {playerIndex + 1}";
            Color trailColor = ResolvePlayerColor(session, playerIndex, ownerConnection);
            GameObject spawnedPlayer = SpawnAt(session, session.playerPrefab, chosen[index], displayName, ownerId, trailColor, false, ownerConnection);
            if (playerIndex == 0)
                RegisterLocalPlayer(spawnedPlayer, displayName);
            index++;
            yield return new WaitForSecondsRealtime(spawnInterval);
        }

        // Spawn bots
        for (int i = 0; i < totalBots; i++)
        {
            if (index >= chosen.Count) break;

            PlayerLook botLook = i < session.botLooks.Count ? session.botLooks[i] : CreateFallbackBotLook(session, i);
            SpawnAt(session, botLook.playerPrefab, chosen[index], botLook.displayName, botLook.ownerId, botLook.trailColor, true, null);
            index++;
            yield return new WaitForSecondsRealtime(spawnInterval);
        }

        // Wait one frame to ensure all Awake/Start run
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

    private GameObject SpawnAt(GameSessionRuntime session, GameObject prefab, SpawnSpot spot, string displayName, string ownerId, Color trailColor, bool isBot, NetworkConnection ownerConnection)
    {
        if (prefab == null || spot == null)
            return null;

        Vector3 pos = spot.Position;
        Quaternion rot = spot.Rotation;
        GameObject go = InstantiateVehiclePrefab(session, prefab, pos, rot);
        PrepareSpawnedVehicleObject(go, isBot);
        _spawned.Add(go);

        VehicleColorApplier colorApplier = go.GetComponent<VehicleColorApplier>();
        if (colorApplier == null)
        {
            Debug.LogError($"Spawned vehicle '{go.name}' is missing VehicleColorApplier.", go);
            return go;
        }
        colorApplier.SetColor(trailColor);

        VehicleLife life = go.GetComponent<VehicleLife>();
        if (life == null)
        {
            Debug.LogError($"Spawned vehicle '{go.name}' is missing VehicleLife.", go);
            return go;
        }
        life.ConfigureSpawn(pos, rot);
        life.ConfigureIdentity(displayName, ownerId);
        session.GetOrCreateStats(ownerId, displayName, trailColor);

        TrailEmitter trailEmitter = go.GetComponent<TrailEmitter>();
        if (trailEmitter != null)
            trailEmitter.Configure(life, trailColor, session != null ? session.trailLength : 1);

        if (isBot)
        {
            BotVehicleInput botInput = go.GetComponent<BotVehicleInput>();
            if (botInput != null)
                botInput.ConfigureRuntime(botMapBoundaryMask, botSuddenDeathMask, botTrailMask, botPowerupMask, botMapCenter);
        }

        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        if (networkObject != null && InstanceFinder.IsServerStarted && !session.isSingleplayer)
            InstanceFinder.ServerManager.Spawn(networkObject, ownerConnection);

        return go;
    }

    private GameObject InstantiateVehiclePrefab(GameSessionRuntime session, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (session == null || !session.isSingleplayer)
            return Instantiate(prefab, position, rotation);

        GameObject inactiveParent = new GameObject("OfflineVehicleSpawnRoot");
        inactiveParent.SetActive(false);

        GameObject instance = Instantiate(prefab, position, rotation, inactiveParent.transform);
        StripSingleplayerNetworkComponents(instance);
        instance.transform.SetParent(null, true);
        Destroy(inactiveParent);
        return instance;
    }

    private void StripSingleplayerNetworkComponents(GameObject instance)
    {
        if (instance == null)
            return;

        NetworkBehaviour[] networkBehaviours = instance.GetComponents<NetworkBehaviour>();
        for (int i = networkBehaviours.Length - 1; i >= 0; i--)
        {
            NetworkBehaviour behaviour = networkBehaviours[i];
            if (behaviour == null)
                continue;

            if (behaviour is PlayerVehicleInput || behaviour is VehicleLife)
                continue;

            DestroyImmediate(behaviour);
        }

        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject != null)
            DestroyImmediate(networkObject);
    }

    private void PrepareSpawnedVehicleObject(GameObject vehicle, bool isBot)
    {
        if (vehicle == null)
            return;

        vehicle.SetActive(true);
        SetVehicleChildrenActive(vehicle, !isBot);

        EnableBehaviour<VehicleController>(vehicle, true);
        EnableBehaviour<VehicleLife>(vehicle, true);
        EnableBehaviour<VehicleColorApplier>(vehicle, true);
        EnableBehaviour<TrailEmitter>(vehicle, true);
        EnableBehaviour<VehicleDeathSequence>(vehicle, true);

        BotVehicleInput botInput = vehicle.GetComponent<BotVehicleInput>();
        if (botInput != null)
            botInput.enabled = isBot;
        else if (isBot)
            Debug.LogError($"Spawned bot '{vehicle.name}' is missing BotVehicleInput.", vehicle);

        PlayerVehicleInput playerInput = vehicle.GetComponent<PlayerVehicleInput>();
        if (playerInput != null)
        {
            playerInput.enabled = !isBot;
            if (!isBot)
                playerInput.RefreshLocalPresentation();
        }
        else if (!isBot)
            Debug.LogError($"Spawned player '{vehicle.name}' is missing PlayerVehicleInput.", vehicle);

        VehicleCameraController cameraController = vehicle.GetComponent<VehicleCameraController>();
        if (cameraController != null)
            cameraController.enabled = !isBot;

        if (isBot)
            SetVehicleCameraState(vehicle, false);

        Rigidbody rb = vehicle.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SetVehicleChildrenActive(GameObject vehicle, bool includeCameras)
    {
        Transform[] children = vehicle.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child == vehicle.transform)
                continue;

            if (!includeCameras && (child.GetComponent<Camera>() != null || child.GetComponent<AudioListener>() != null))
                continue;

            child.gameObject.SetActive(true);
        }
    }

    private void SetVehicleCameraState(GameObject vehicle, bool active)
    {
        Camera[] cameras = vehicle.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null)
                continue;

            camera.gameObject.SetActive(active);
            camera.enabled = active;
        }

        AudioListener[] listeners = vehicle.GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null)
                listener.enabled = active;
        }
    }

    private void EnableBehaviour<T>(GameObject owner, bool enabled) where T : Behaviour
    {
        T behaviour = owner.GetComponent<T>();
        if (behaviour != null)
            behaviour.enabled = enabled;
    }

    private void RegisterLocalPlayer(GameObject spawnedPlayer, string displayName)
    {
        if (spawnedPlayer == null)
            return;

        if (PlayerProfileStats.Instance != null)
            PlayerProfileStats.Instance.SetPlayer(spawnedPlayer, displayName);

        if (DistanceTracker.Instance != null)
            DistanceTracker.Instance.SetTarget(spawnedPlayer.transform);
    }

    private List<NetworkConnection> GetConnectedPlayers()
    {
        if (!InstanceFinder.IsServerStarted || InstanceFinder.ServerManager == null)
            return new List<NetworkConnection>();

        List<NetworkConnection> players = InstanceFinder.ServerManager.Clients.Values
            .Where(connection => connection != null && connection.IsAuthenticated)
            .OrderBy(connection => connection.ClientId)
            .ToList();

        NetworkConnection localConnection = InstanceFinder.IsClientStarted && InstanceFinder.ClientManager != null
            ? InstanceFinder.ClientManager.Connection
            : null;

        if (localConnection != null && localConnection.IsAuthenticated && !ContainsLocalClient(players, localConnection))
            players.Insert(0, localConnection);

        return players;
    }

    private bool ContainsLocalClient(List<NetworkConnection> players, NetworkConnection localConnection)
    {
        if (players == null || localConnection == null)
            return false;

        for (int i = 0; i < players.Count; i++)
        {
            NetworkConnection connection = players[i];
            if (connection == null)
                continue;

            if (connection == localConnection || connection.IsLocalClient || connection.IsHost)
                return true;

            if (localConnection.IsValid && connection.ClientId == localConnection.ClientId)
                return true;
        }

        return false;
    }

    private Color ResolvePlayerColor(GameSessionRuntime session, int playerIndex, NetworkConnection ownerConnection = null)
    {
        if (session == null || session.trailColorPalette == null || session.trailColorPalette.Count == 0)
            return Color.white;

        if (!session.isSingleplayer &&
            ownerConnection != null &&
            MultiplayerSessionDriver.Instance != null &&
            MultiplayerSessionDriver.Instance.TryGetTrailColorIndex(ownerConnection.ClientId, out int selectedColorIndex))
        {
            selectedColorIndex = Mathf.Clamp(selectedColorIndex, 0, session.trailColorPalette.Count - 1);
            return session.trailColorPalette[selectedColorIndex];
        }

        if (playerIndex == 0)
            return session.playerTrailColor;

        List<Color> availableColors = new List<Color>();
        for (int i = 0; i < session.trailColorPalette.Count; i++)
        {
            Color color = session.trailColorPalette[i];
            if (!ApproximatelySameColor(color, session.playerTrailColor))
                availableColors.Add(color);
        }

        if (availableColors.Count == 0)
            return session.trailColorPalette[playerIndex % session.trailColorPalette.Count];

        return availableColors[(playerIndex - 1) % availableColors.Count];
    }

    private bool ApproximatelySameColor(Color first, Color second)
    {
        const float tolerance = 0.001f;
        return Mathf.Abs(first.r - second.r) < tolerance &&
               Mathf.Abs(first.g - second.g) < tolerance &&
               Mathf.Abs(first.b - second.b) < tolerance &&
               Mathf.Abs(first.a - second.a) < tolerance;
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

    private int GetRequestedBotCount(GameSessionRuntime session, int totalHumanPlayers)
    {
        if (session == null)
            return 0;

        int configuredBots = 0;
        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry != null)
                configuredBots += Mathf.Max(0, entry.count);
        }

        if (configuredBots > 0)
            return Mathf.Min(configuredBots, Mathf.Max(0, session.maxPlayers - totalHumanPlayers));

        return Mathf.Max(0, session.maxPlayers - totalHumanPlayers);
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
        return GameSessionRuntime.FromDefaults(gameSettings, botsSettings, playerLook);
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
        }

        int humanPlayers = session.isSingleplayer ? 1 : Mathf.Max(1, GetConnectedPlayers().Count);
        int totalBots = GetRequestedBotCount(session, humanPlayers);
        int totalPlayers = totalBots + humanPlayers;
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
