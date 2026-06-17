using System.Collections;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class MultiplayerRuntimeBootstrap : MonoBehaviour
{
    [Header("FishNet")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private NetworkObject sessionDriverPrefab;

    [Header("Scenes")]
    [SerializeField] private SceneReference mainMenuScene;

    [Header("Connection")]
    [SerializeField] private string defaultJoinAddress = "127.0.0.1";
    [SerializeField] private float clientConnectTimeoutSeconds = 8f;

    private static MultiplayerRuntimeBootstrap _instance;

    private string _joinAddress;
    private bool _matchLoadInProgress;
    private string _pendingArenaSceneName;
    private bool _clientConnectAttemptActive;
    private bool _clientHadConnection;
    private bool _connectionPopupOpen;
    private bool _intentionalDisconnect;
    private bool _isHosting;
    private bool _joinSuccessPopupShown;
    private Coroutine _clientConnectTimeoutRoutine;
    private NetworkManager _registeredConnectionEventsManager;

    public static MultiplayerRuntimeBootstrap Instance => _instance;

    public bool IsServerStarted => networkManager != null && networkManager.IsServerStarted;
    public bool IsHostStartingOrStarted => networkManager != null && (networkManager.IsServerStarted || networkManager.IsClientStarted);
    public bool IsHostReady => InstanceFinder.IsServerStarted && InstanceFinder.IsClientStarted;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _joinAddress = NormalizeJoinAddress(defaultJoinAddress);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        UnregisterConnectionHandlers();

        if (_instance == this)
            _instance = null;
    }

    public bool HostGame()
    {
        return RequestHostGame();
    }

    public bool RequestHostGame()
    {
        if (!TryValidateNetworkSetup(out NetworkManager manager))
            return false;

        _intentionalDisconnect = false;
        _isHosting = true;
        _clientConnectAttemptActive = false;
        StopClientConnectTimeout();
        StartHost(manager);
        return true;
    }

    public void JoinGame()
    {
        JoinGame(defaultJoinAddress);
    }

    public void JoinGame(string address)
    {
        if (!TryValidateNetworkSetup(out NetworkManager manager))
            return;

        _intentionalDisconnect = false;
        _isHosting = false;
        _clientHadConnection = false;
        _clientConnectAttemptActive = true;
        _joinSuccessPopupShown = false;
        _joinAddress = NormalizeJoinAddress(address);
        StartClientConnectTimeout(manager);
        StartClient(manager);
    }

    public void BackToMainMenu()
    {
        _intentionalDisconnect = true;
        _clientConnectAttemptActive = false;
        StopClientConnectTimeout();
        StopNetworkingIfNeeded();

        LoadMainMenuScene();
    }

    private void LoadMainMenuScene()
    {
        if (mainMenuScene == null || string.IsNullOrWhiteSpace(mainMenuScene.SceneName))
        {
            Debug.LogWarning("[MultiplayerRuntimeBootstrap] Main Menu scene is not assigned. Loading build scene index 0 as fallback.");
            UnitySceneManager.LoadScene(0);
            return;
        }

        UnitySceneManager.LoadScene(mainMenuScene.SceneName);
    }

    public void StartMatch()
    {
        if (!TryValidateNetworkSetup(out _))
            return;

        if (!InstanceFinder.IsServerStarted)
        {
            Debug.LogWarning("[MultiplayerRuntimeBootstrap] Cannot start match because FishNet server is not started yet.");
            return;
        }

        EnsureSessionDriverSpawned();

        MultiplayerSessionDriver driver = MultiplayerSessionDriver.Instance;
        if (driver == null)
        {
            Debug.LogWarning("[MultiplayerRuntimeBootstrap] Cannot start match because MultiplayerSessionDriver is not ready.");
            return;
        }

        if (!driver.IsMatchRunning)
            driver.StartMatch();
    }

    public void StartMatch(
        MatchSettings settings,
        MatchRules matchRules,
        VehiclePrefabSet networkVehiclePrefabSet,
        TrailColorPalette trailColorPalette,
        Color selectedPlayerTrailColor)
    {
        if (!TryValidateNetworkSetup(out _))
            return;

        if (!InstanceFinder.IsServerStarted)
        {
            Debug.LogWarning("[MultiplayerRuntimeBootstrap] Cannot start match because FishNet server is not started yet.");
            return;
        }

        MultiplayerSessionDriver driver = MultiplayerSessionDriver.Instance;
        if (driver == null)
        {
            EnsureSessionDriverSpawned();
            driver = MultiplayerSessionDriver.Instance;
        }

        if (driver == null)
        {
            Debug.LogError("[MultiplayerRuntimeBootstrap] Cannot start match because MultiplayerSessionDriver.Instance is null.");
            return;
        }

        if (driver.IsMatchRunning)
            return;

        driver.StartMatch(settings, matchRules, networkVehiclePrefabSet, trailColorPalette, selectedPlayerTrailColor);
    }

    public void BeginServerArenaLoadAndInitialize(string arenaSceneName)
    {
        if (!InstanceFinder.IsServerStarted)
        {
            Debug.LogWarning("[MultiplayerRuntimeBootstrap] Cannot load multiplayer arena because server is not started.");
            return;
        }

        if (string.IsNullOrWhiteSpace(arenaSceneName))
        {
            Debug.LogError("[MultiplayerRuntimeBootstrap] Cannot load multiplayer arena because arena scene name is empty.");
            return;
        }

        if (_matchLoadInProgress)
        {
            Debug.LogWarning($"[MultiplayerRuntimeBootstrap] Arena load already in progress for '{_pendingArenaSceneName}'. Ignoring duplicate request for '{arenaSceneName}'.");
            return;
        }

        _pendingArenaSceneName = arenaSceneName;
        StartCoroutine(ServerLoadArenaAndInitializeRoutine(arenaSceneName));
    }

    private IEnumerator ServerLoadArenaAndInitializeRoutine(string arenaSceneName)
    {
        _matchLoadInProgress = true;

        SceneLoadData sceneLoadData = new(arenaSceneName);
        sceneLoadData.ReplaceScenes = ReplaceOption.All;
        sceneLoadData.PreferredActiveScene = new PreferredScene(new SceneLookupData(arenaSceneName));

        InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);

        // Let FishNet and Unity process the global scene load request.
        yield return null;
        yield return null;

        float timeoutAt = Time.realtimeSinceStartup + 15f;
        MatchInitializer initializer = null;

        while (Time.realtimeSinceStartup < timeoutAt)
        {
            if (!InstanceFinder.IsServerStarted)
            {
                Debug.LogWarning("[MultiplayerRuntimeBootstrap] Server stopped while waiting for MatchInitializer.");
                _matchLoadInProgress = false;
                yield break;
            }

            initializer = FindFirstObjectByType<MatchInitializer>();
            if (initializer != null)
                break;

            yield return null;
        }

        if (initializer == null)
        {
            Debug.LogError($"[MultiplayerRuntimeBootstrap] Arena '{arenaSceneName}' loaded but no MatchInitializer was found before timeout.");
            _matchLoadInProgress = false;
            yield break;
        }

        // Conservative settle delay so clients finish scene transition before server spawns owned player objects.
        float settleUntil = Time.realtimeSinceStartup + 0.5f;
        while (Time.realtimeSinceStartup < settleUntil)
            yield return null;

        initializer.BeginMatchInitialization();

        _matchLoadInProgress = false;
    }

    private void StartHost(NetworkManager manager)
    {
        if (manager == null)
            return;

        if (!manager.IsServerStarted)
            manager.ServerManager.StartConnection();

        if (!manager.IsClientStarted)
            manager.ClientManager.StartConnection();
    }

    private void StartClient(NetworkManager manager)
    {
        if (manager == null)
            return;

        manager.ClientManager.StartConnection(_joinAddress);
    }

    private void StopNetworking()
    {
        if (networkManager == null)
            return;

        if (networkManager.IsClientStarted)
            networkManager.ClientManager.StopConnection();
        if (networkManager.IsServerStarted)
            networkManager.ServerManager.StopConnection(true);

        MultiplayerMatchState.SetFrozen(false);
        Time.timeScale = 1f;
    }

    private void StopNetworkingIfNeeded()
    {
        if (networkManager != null && (networkManager.IsServerStarted || networkManager.IsClientStarted))
            StopNetworking();
    }

    private bool TryValidateNetworkSetup(out NetworkManager manager)
    {
        manager = networkManager;

        if (manager == null)
        {
            Debug.LogError("[MultiplayerRuntimeBootstrap] NetworkManager is not assigned. Assign it on MultiplayerBootstrap.prefab.");
            return false;
        }

        networkManager = manager;

        if (networkManager.SpawnablePrefabs == null)
        {
            Debug.LogError("[MultiplayerRuntimeBootstrap] NetworkManager.SpawnablePrefabs is not assigned. Assign DefaultPrefabObjects.asset in Unity.");
            return false;
        }

        if (sessionDriverPrefab == null)
        {
            Debug.LogError("[MultiplayerRuntimeBootstrap] Session driver prefab is not assigned. Assign MultiplayerSessionDriver.prefab NetworkObject in Unity.");
            return false;
        }

        MultiplayerMatchBroadcasts.RegisterClientHandlers(networkManager);
        RegisterConnectionHandlers(networkManager);
        return true;
    }

    private void EnsureSessionDriverSpawned()
    {
        if (networkManager == null || !networkManager.IsServerStarted || MultiplayerSessionDriver.Instance != null)
            return;

        if (sessionDriverPrefab == null)
        {
            Debug.LogError("[MultiplayerRuntimeBootstrap] Cannot spawn session driver because sessionDriverPrefab is not assigned.");
            return;
        }

        NetworkObject instance = Instantiate(sessionDriverPrefab);
        instance.name = sessionDriverPrefab.name;
        networkManager.ServerManager.Spawn(instance);
    }

    private string NormalizeJoinAddress(string address)
    {
        if (!string.IsNullOrWhiteSpace(address))
            return address.Trim();

        if (!string.IsNullOrWhiteSpace(defaultJoinAddress))
            return defaultJoinAddress.Trim();

        return "127.0.0.1";
    }

    private void RegisterConnectionHandlers(NetworkManager manager)
    {
        if (_registeredConnectionEventsManager == manager)
            return;

        UnregisterConnectionHandlers();

        if (manager == null)
            return;

        if (manager.ClientManager != null)
            manager.ClientManager.OnClientConnectionState += OnClientConnectionState;

        _registeredConnectionEventsManager = manager;
    }

    private void UnregisterConnectionHandlers()
    {
        if (_registeredConnectionEventsManager == null)
            return;

        if (_registeredConnectionEventsManager.ClientManager != null)
            _registeredConnectionEventsManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;

        _registeredConnectionEventsManager = null;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            _clientHadConnection = true;
            _clientConnectAttemptActive = false;
            StopClientConnectTimeout();

            if (!_isHosting && !_joinSuccessPopupShown)
            {
                _joinSuccessPopupShown = true;
                RuntimePopupDialog.Show("Joined game successfully.", "Disconnect", () =>
                {
                    _intentionalDisconnect = true;
                    BackToMainMenu();
                });
            }

            return;
        }

        if (args.ConnectionState != LocalConnectionState.Stopped)
            return;

        StopClientConnectTimeout();

        if (_intentionalDisconnect || _isHosting)
        {
            _clientConnectAttemptActive = false;
            _clientHadConnection = false;
            return;
        }

        if (_clientConnectAttemptActive && !_clientHadConnection)
        {
            _clientConnectAttemptActive = false;
            ShowConnectionPopup("Could not connect to server.");
            return;
        }

        if (_clientHadConnection)
            ShowConnectionPopup("Host closed the game.");

        _clientHadConnection = false;
    }

    private void StartClientConnectTimeout(NetworkManager manager)
    {
        StopClientConnectTimeout();

        if (manager != null)
            _clientConnectTimeoutRoutine = StartCoroutine(ClientConnectTimeoutRoutine(manager));
    }

    private void StopClientConnectTimeout()
    {
        if (_clientConnectTimeoutRoutine == null)
            return;

        StopCoroutine(_clientConnectTimeoutRoutine);
        _clientConnectTimeoutRoutine = null;
    }

    private IEnumerator ClientConnectTimeoutRoutine(NetworkManager manager)
    {
        float timeoutAt = Time.realtimeSinceStartup + Mathf.Max(1f, clientConnectTimeoutSeconds);

        while (Time.realtimeSinceStartup < timeoutAt)
        {
            if (_intentionalDisconnect || !_clientConnectAttemptActive)
            {
                _clientConnectTimeoutRoutine = null;
                yield break;
            }

            if (manager != null && manager.IsClientStarted)
            {
                _clientConnectAttemptActive = false;
                _clientConnectTimeoutRoutine = null;
                yield break;
            }

            yield return null;
        }

        _clientConnectTimeoutRoutine = null;

        if (!_intentionalDisconnect && _clientConnectAttemptActive)
        {
            _clientConnectAttemptActive = false;
            ShowConnectionPopup("Could not connect to server.");
        }
    }

    private void ShowConnectionPopup(string message)
    {
        if (_connectionPopupOpen)
            return;

        _connectionPopupOpen = true;
        RuntimePopupDialog.Show(message, () =>
        {
            _connectionPopupOpen = false;
            _intentionalDisconnect = true;
            BackToMainMenu();
        });
    }
}
