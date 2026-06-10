using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class MultiplayerRuntimeBootstrap : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string MultiplayerScenePrefix = "Multiplayer";
    private const string MultiplayerArenaScenePrefix = "multi ";
    private const string MultiplayerArenaSceneSuffix = " Multiplayer";
    private const int MinimumHumanPlayersToStart = 2;
    private const float MatchSceneStartGraceTime = 0.75f;

    private static MultiplayerRuntimeBootstrap _instance;

    [SerializeField] private DefaultPrefabObjects _prefabCollection;
    [SerializeField] private GameObject _sessionDriverPrefab;

    private NetworkManager _networkManager;
    private string _joinAddress = "127.0.0.1";
    private bool _driverSpawnRequested;
    private bool _startMatchAfterSceneLoad;
    private string _pendingMatchSceneName;
    private HashSet<int> _pendingMatchClientIds = new();
    private bool _clientJoinPending;
    private bool _clientAuthenticated;
    private Coroutine _joinTimeoutCoroutine;

    public static MultiplayerRuntimeBootstrap Instance => _instance;
    public bool IsServerStarted => _networkManager != null && _networkManager.IsServerStarted;
    public bool IsClientStarted => _networkManager != null && _networkManager.IsClientStarted;
    public int ConnectedPlayerCount => GetAuthenticatedClientCount();
    public int MinimumHumanPlayers => MinimumHumanPlayersToStart;

    public event Action<string> StatusChanged;
    public event Action<int, int> PlayerCountChanged;

    public static bool IsMultiplayerScene(Scene scene)
    {
        return scene.IsValid() &&
            (scene.name.StartsWith(MultiplayerScenePrefix, System.StringComparison.OrdinalIgnoreCase) ||
             scene.name.StartsWith(MultiplayerArenaScenePrefix, System.StringComparison.OrdinalIgnoreCase) ||
             scene.name.EndsWith(MultiplayerArenaSceneSuffix, System.StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsActiveMultiplayerScene()
    {
        return IsMultiplayerScene(UnitySceneManager.GetActiveScene());
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        UnitySceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            UnsubscribeNetworkEvents();
            UnitySceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void Update()
    {
        if (!IsActiveMultiplayerScene() || _networkManager == null)
            return;

        if (_networkManager.IsServerStarted && _driverSpawnRequested)
            EnsureSessionDriverSpawned();
    }

    public void HostGame()
    {
        EnsureNetworkManager();
        if (_networkManager == null)
            return;

        MultiplayerSessionDriver.ClearTrailColorSelections();
        MultiplayerSessionDriver.ClearLobbyState();
        StartHost();
        EmitLobbyStatus();
    }

    public bool JoinGame(string address)
    {
        EnsureNetworkManager();
        if (_networkManager == null)
            return false;

        _joinAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
        MultiplayerSessionDriver.ClearTrailColorSelections();
        MultiplayerSessionDriver.ClearLobbyState();
        bool started = StartClient();
        if (started)
        {
            _clientJoinPending = true;
            _clientAuthenticated = false;
            RestartJoinTimeout();
            EmitStatus("Looking for host");
        }
        else
        {
            EmitStatus("No game found at given address");
        }

        return started;
    }

    public void BackToMainMenu()
    {
        StopNetworkingIfNeeded();
        SceneTransitionLoader.LoadScene(MainMenuSceneName);
    }

    public void StartMatch()
    {
        if (_networkManager == null || !_networkManager.IsServerStarted)
            return;

        MultiplayerSessionDriver driver = MultiplayerSessionDriver.Instance;
        if (driver != null && !driver.IsMatchRunning)
            driver.StartMatch();
    }

    public void LoadMultiplayerMatchScene(string sceneName)
    {
        EnsureNetworkManager();
        if (_networkManager == null || !_networkManager.IsServerStarted)
        {
            Debug.LogWarning("Only the host can load a multiplayer match scene.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Cannot load multiplayer match scene because the scene name is empty.");
            return;
        }

        if (GetAuthenticatedClientCount() < MinimumHumanPlayersToStart)
        {
            EmitStatus("No player has joined");
            EmitLobbyStatus();
            return;
        }

        _startMatchAfterSceneLoad = true;
        _pendingMatchSceneName = sceneName;
        _pendingMatchClientIds = GetAuthenticatedServerClientIds();
        SceneLoadData sceneLoadData = new(sceneName);
        sceneLoadData.ReplaceScenes = ReplaceOption.All;
        sceneLoadData.PreferredActiveScene = new PreferredScene(new SceneLookupData(sceneName));
        InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);
    }

    private void StartHost()
    {
        EnsureNetworkManager();
        if (_networkManager == null)
            return;

        _driverSpawnRequested = true;
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();
    }

    private bool StartClient()
    {
        EnsureNetworkManager();
        if (_networkManager == null)
            return false;

        string address = string.IsNullOrWhiteSpace(_joinAddress) ? "127.0.0.1" : _joinAddress.Trim();
        return _networkManager.ClientManager.StartConnection(address);
    }

    private void StopNetworking()
    {
        if (_networkManager == null)
            return;

        if (_networkManager.IsClientStarted)
            _networkManager.ClientManager.StopConnection();
        if (_networkManager.IsServerStarted)
            _networkManager.ServerManager.StopConnection(true);

        _driverSpawnRequested = false;
        _startMatchAfterSceneLoad = false;
        _pendingMatchSceneName = null;
        _pendingMatchClientIds.Clear();
        _clientJoinPending = false;
        _clientAuthenticated = false;
        StopJoinTimeout();
        MultiplayerSessionDriver.ClearTrailColorSelections();
        MultiplayerSessionDriver.ClearLobbyState();
        MultiplayerMatchState.SetFrozen(false);
        Time.timeScale = 1f;
        EmitStatus(string.Empty);
    }

    private void StopNetworkingIfNeeded()
    {
        if (_networkManager != null && (_networkManager.IsServerStarted || _networkManager.IsClientStarted))
            StopNetworking();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsMultiplayerScene(scene))
        {
            MultiplayerMatchState.SetFrozen(false);
            Time.timeScale = 1f;
            _startMatchAfterSceneLoad = false;
            _pendingMatchSceneName = null;
            _pendingMatchClientIds.Clear();
            return;
        }

        EnsureNetworkManager();
        RefreshSceneNetworkState();
        if (_networkManager != null && _networkManager.IsServerStarted)
        {
            _driverSpawnRequested = true;
            EnsureSessionDriverSpawned();
        }

        if (_startMatchAfterSceneLoad && _networkManager != null && _networkManager.IsServerStarted)
            StartCoroutine(StartMatchWhenSceneReady());
    }

    private void RefreshSceneNetworkState()
    {
        if (_networkManager == null)
            return;

        if (_networkManager.IsServerStarted)
            EmitLobbyStatus();
        else if (_networkManager.IsClientStarted)
            EmitStatus("Waiting for host to start the game");
        else
            EmitStatus(string.Empty);
    }

    private void EnsureNetworkManager()
    {
        if (_networkManager != null)
            return;

        if (_prefabCollection == null)
        {
            Debug.LogError($"{nameof(MultiplayerRuntimeBootstrap)} is missing a FishNet prefab collection reference.", this);
            return;
        }

        if (_sessionDriverPrefab == null)
        {
            Debug.LogError($"{nameof(MultiplayerRuntimeBootstrap)} is missing a session driver prefab reference.", this);
            return;
        }

        GameObject managerObject = new("FishNetRuntime");
        managerObject.SetActive(false);
        DontDestroyOnLoad(managerObject);

        _networkManager = AddNetworkManager(managerObject);
        managerObject.AddComponent<Tugboat>();
        _networkManager.SpawnablePrefabs = _prefabCollection;

        managerObject.SetActive(true);
        SubscribeNetworkEvents();
    }

    private static NetworkManager AddNetworkManager(GameObject managerObject)
    {
#if UNITY_EDITOR
        bool wasLoggingEnabled = Debug.unityLogger.logEnabled;
        try
        {
            Debug.unityLogger.logEnabled = false;
            return managerObject.AddComponent<NetworkManager>();
        }
        finally
        {
            Debug.unityLogger.logEnabled = wasLoggingEnabled;
        }
#else
        return managerObject.AddComponent<NetworkManager>();
#endif
    }

    private void EnsureSessionDriverSpawned()
    {
        if (_networkManager == null || !_networkManager.IsServerStarted || MultiplayerSessionDriver.Instance != null)
            return;

        if (_sessionDriverPrefab == null)
            return;

        GameObject instance = Instantiate(_sessionDriverPrefab);
        instance.name = _sessionDriverPrefab.name;
        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("Multiplayer session driver prefab is missing a NetworkObject.");
            Destroy(instance);
            return;
        }

        _networkManager.ServerManager.Spawn(networkObject);
        _driverSpawnRequested = false;
    }

    private IEnumerator StartMatchWhenSceneReady()
    {
        _startMatchAfterSceneLoad = false;

        while (MultiplayerSessionDriver.Instance == null)
        {
            EnsureSessionDriverSpawned();
            yield return null;
        }

        while (FindFirstObjectByType<MatchInitializer>() == null)
            yield return null;

        yield return StartCoroutine(WaitForMatchClientsToLoadScene());
        yield return null;

        MultiplayerSessionDriver.Instance.StartMatch();
    }

    private IEnumerator WaitForMatchClientsToLoadScene()
    {
        if (_networkManager == null || _networkManager.SceneManager == null || _pendingMatchClientIds.Count == 0)
            yield break;

        float timeoutAt = Time.realtimeSinceStartup + MatchSceneStartGraceTime;
        while (Time.realtimeSinceStartup < timeoutAt)
        {
            if (ExpectedClientsLoadedMatchScene())
                yield break;

            yield return null;
        }
    }

    private bool ExpectedClientsLoadedMatchScene()
    {
        Scene matchScene = string.IsNullOrWhiteSpace(_pendingMatchSceneName)
            ? UnitySceneManager.GetActiveScene()
            : UnitySceneManager.GetSceneByName(_pendingMatchSceneName);

        if (!matchScene.IsValid() || !matchScene.isLoaded)
            return false;

        if (!_networkManager.SceneManager.SceneConnections.TryGetValue(matchScene, out HashSet<NetworkConnection> sceneConnections))
            return false;

        foreach (int clientId in _pendingMatchClientIds)
        {
            if (!IsServerClientStillAuthenticated(clientId))
                continue;

            bool loaded = sceneConnections.Any(connection => connection != null && connection.ClientId == clientId);
            if (!loaded)
                return false;
        }

        return true;
    }

    private void SubscribeNetworkEvents()
    {
        if (_networkManager == null)
            return;

        _networkManager.ServerManager.OnServerConnectionState += HandleServerConnectionState;
        _networkManager.ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
        _networkManager.ServerManager.OnAuthenticationResult += HandleAuthenticationResult;
        _networkManager.ClientManager.OnClientConnectionState += HandleClientConnectionState;
        _networkManager.ClientManager.OnAuthenticated += HandleClientAuthenticated;
    }

    private void UnsubscribeNetworkEvents()
    {
        if (_networkManager == null)
            return;

        _networkManager.ServerManager.OnServerConnectionState -= HandleServerConnectionState;
        _networkManager.ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
        _networkManager.ServerManager.OnAuthenticationResult -= HandleAuthenticationResult;
        _networkManager.ClientManager.OnClientConnectionState -= HandleClientConnectionState;
        _networkManager.ClientManager.OnAuthenticated -= HandleClientAuthenticated;
    }

    private void HandleServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
            EmitLobbyStatus();
        else if (args.ConnectionState == LocalConnectionState.Stopped)
            EmitStatus(string.Empty);
    }

    private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        EmitLobbyStatus();
    }

    private void HandleAuthenticationResult(NetworkConnection connection, bool authenticated)
    {
        EmitLobbyStatus();
    }

    private void HandleClientConnectionState(ClientConnectionStateArgs args)
    {
        if (_networkManager != null && _networkManager.IsServerStarted)
            return;

        if (args.ConnectionState == LocalConnectionState.Starting)
            EmitStatus("Looking for host");
        else if (args.ConnectionState == LocalConnectionState.Started)
            EmitStatus("Waiting for host to start the game");
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            if (_clientJoinPending && !_clientAuthenticated)
                EmitStatus("No game found at given address");
        }
    }

    private void HandleClientAuthenticated()
    {
        _clientJoinPending = false;
        _clientAuthenticated = true;
        StopJoinTimeout();

        if (_networkManager != null && _networkManager.IsServerStarted)
            EmitLobbyStatus();
        else
            EmitStatus("Waiting for host to start the game");
    }

    private void EmitLobbyStatus()
    {
        int connected = GetAuthenticatedClientCount();
        PlayerCountChanged?.Invoke(connected, MinimumHumanPlayersToStart);

        if (_networkManager == null || !_networkManager.IsServerStarted)
            return;

        if (connected < MinimumHumanPlayersToStart)
            EmitStatus($"Waiting for players ({connected}/{MinimumHumanPlayersToStart})");
        else
            EmitStatus($"Players joined ({connected}/{MinimumHumanPlayersToStart})");
    }

    private void EmitStatus(string message)
    {
        StatusChanged?.Invoke(message);
    }

    private void RestartJoinTimeout()
    {
        StopJoinTimeout();
        _joinTimeoutCoroutine = StartCoroutine(JoinTimeoutRoutine());
    }

    private void StopJoinTimeout()
    {
        if (_joinTimeoutCoroutine == null)
            return;

        StopCoroutine(_joinTimeoutCoroutine);
        _joinTimeoutCoroutine = null;
    }

    private IEnumerator JoinTimeoutRoutine()
    {
        yield return new WaitForSecondsRealtime(4f);

        if (_clientJoinPending && !_clientAuthenticated)
        {
            _clientJoinPending = false;
            EmitStatus("No game found at given address");

            if (_networkManager != null && _networkManager.IsClientStarted)
                _networkManager.ClientManager.StopConnection();
        }

        _joinTimeoutCoroutine = null;
    }

    private int GetAuthenticatedClientCount()
    {
        if (_networkManager == null)
            return 0;

        if (_networkManager.ServerManager != null && _networkManager.IsServerStarted)
        {
            int count = _networkManager.ServerManager.Clients.Values
                .Count(connection => connection != null && connection.IsAuthenticated);

            if (_networkManager.IsClientStarted)
            {
                NetworkConnection localConnection = _networkManager.ClientManager.Connection;
                bool localClientIncluded = _networkManager.ServerManager.Clients.Values
                    .Any(connection => connection != null &&
                                       connection.IsAuthenticated &&
                                       (connection == localConnection ||
                                        connection.IsLocalClient ||
                                        connection.IsHost ||
                                        (localConnection != null && localConnection.IsValid && connection.ClientId == localConnection.ClientId)));

                if (!localClientIncluded && localConnection != null && localConnection.IsAuthenticated)
                    count++;
            }

            return count;
        }

        return _networkManager.IsClientStarted ? 1 : 0;
    }

    private HashSet<int> GetAuthenticatedServerClientIds()
    {
        if (_networkManager == null || _networkManager.ServerManager == null)
            return new HashSet<int>();

        NetworkConnection localConnection = _networkManager.IsClientStarted ? _networkManager.ClientManager.Connection : null;
        return new HashSet<int>(_networkManager.ServerManager.Clients.Values
            .Where(connection => connection != null && connection.IsAuthenticated)
            .Where(connection => connection != localConnection && !connection.IsLocalClient && !connection.IsHost)
            .Select(connection => connection.ClientId)
            .Where(clientId => localConnection == null || !localConnection.IsValid || clientId != localConnection.ClientId));
    }

    private bool IsServerClientStillAuthenticated(int clientId)
    {
        if (_networkManager == null || _networkManager.ServerManager == null)
            return false;

        return _networkManager.ServerManager.Clients.Values
            .Any(connection => connection != null &&
                               connection.ClientId == clientId &&
                               connection.IsAuthenticated);
    }
}
