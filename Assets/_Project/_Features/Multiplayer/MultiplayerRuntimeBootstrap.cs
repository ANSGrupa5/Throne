using System.Collections;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class MultiplayerRuntimeBootstrap : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string MultiplayerScenePrefix = "Multiplayer";
    private const string PrefabCollectionResourcePath = "Networking/DefaultPrefabObjects";
    private const string SessionDriverResourcePath = "Networking/MultiplayerSessionDriver";

    private static MultiplayerRuntimeBootstrap _instance;

    private NetworkManager _networkManager;
    private DefaultPrefabObjects _prefabCollection;
    private GameObject _sessionDriverPrefab;
    private string _joinAddress = "127.0.0.1";
    private bool _matchLoadInProgress;
    private string _pendingArenaSceneName;

    public static MultiplayerRuntimeBootstrap Instance => _instance;

    public bool IsServerStarted => _networkManager != null && _networkManager.IsServerStarted;
    public bool IsHostStartingOrStarted => _networkManager != null && (_networkManager.IsServerStarted || _networkManager.IsClientStarted);
    public bool IsHostReady => InstanceFinder.IsServerStarted && InstanceFinder.IsClientStarted;

    public static bool IsMultiplayerScene(Scene scene)
    {
        return scene.IsValid() && scene.name.StartsWith(MultiplayerScenePrefix, System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsActiveMultiplayerScene()
    {
        return IsMultiplayerScene(UnitySceneManager.GetActiveScene());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureRuntime()
    {
        if (_instance != null)
            return;

        GameObject runtimeObject = new(nameof(MultiplayerRuntimeBootstrap));
        DontDestroyOnLoad(runtimeObject);
        _instance = runtimeObject.AddComponent<MultiplayerRuntimeBootstrap>();
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
            UnitySceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public bool HostGame()
    {
        return RequestHostGame();
    }

    public bool RequestHostGame()
    {
        NetworkManager manager = EnsureNetworkManager();
        if (manager == null)
        {
            Debug.LogError("[MultiplayerRuntimeBootstrap] Cannot host because NetworkManager is missing.");
            return false;
        }

        StartHost();
        return true;
    }

    public void JoinGame()
    {
        JoinGame(_joinAddress);
    }

    public void JoinGame(string address)
    {
        NetworkManager manager = EnsureNetworkManager();
        if (manager == null)
            return;

        _joinAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
        StartClient();
    }

    public void BackToMainMenu()
    {
        StopNetworkingIfNeeded();
        UnitySceneManager.LoadScene(MainMenuSceneName);
    }

    public void StartMatch()
    {
        EnsureNetworkManager();

        if (!InstanceFinder.IsServerStarted)
        {
            Debug.LogWarning("Cannot start multiplayer match because the server is not started.");
            return;
        }

        EnsureSessionDriverSpawned();

        MultiplayerSessionDriver driver = MultiplayerSessionDriver.Instance;
        if (driver == null)
        {
            Debug.LogWarning("Cannot start multiplayer match because MultiplayerSessionDriver is not ready.");
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
        EnsureNetworkManager();

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

    private void StartHost()
    {
        NetworkManager manager = EnsureNetworkManager();
        if (manager == null)
            return;

        MultiplayerMatchBroadcasts.RegisterClientHandlers(manager);

        if (!manager.IsServerStarted)
            manager.ServerManager.StartConnection();

        if (!manager.IsClientStarted)
            manager.ClientManager.StartConnection();
    }

    private void StartClient()
    {
        NetworkManager manager = EnsureNetworkManager();
        if (manager == null)
            return;

        string address = string.IsNullOrWhiteSpace(_joinAddress) ? "127.0.0.1" : _joinAddress.Trim();
        MultiplayerMatchBroadcasts.RegisterClientHandlers(manager);
        manager.ClientManager.StartConnection(address);
    }

    private void StopNetworking()
    {
        if (_networkManager == null)
            return;

        if (_networkManager.IsClientStarted)
            _networkManager.ClientManager.StopConnection();
        if (_networkManager.IsServerStarted)
            _networkManager.ServerManager.StopConnection(true);

        MultiplayerMatchState.SetFrozen(false);
        Time.timeScale = 1f;
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
            return;
        }

        EnsureNetworkManager();
    }

    private NetworkManager EnsureNetworkManager()
    {
        if (_networkManager != null)
            return _networkManager;

        NetworkManager existing = FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            _networkManager = existing;
            MultiplayerMatchBroadcasts.RegisterClientHandlers(_networkManager);
            return _networkManager;
        }

        _prefabCollection = LoadPrefabCollection();
        _sessionDriverPrefab = Resources.Load<GameObject>(SessionDriverResourcePath);

        if (_prefabCollection == null)
        {
            Debug.LogError($"Missing FishNet prefab collection. Expected Resources/{PrefabCollectionResourcePath}.");
            return null;
        }

        if (_sessionDriverPrefab == null)
        {
            Debug.LogError($"Missing session driver prefab at Resources/{SessionDriverResourcePath}.");
            return null;
        }

        GameObject managerObject = new("FishNetRuntime");
        managerObject.SetActive(false);

        _networkManager = managerObject.AddComponent<NetworkManager>();
        _networkManager.SpawnablePrefabs = _prefabCollection;
        managerObject.AddComponent<Tugboat>();

        if (_networkManager.SpawnablePrefabs == null)
        {
            Debug.LogError("FishNetRuntime NetworkManager was created without SpawnablePrefabs assigned.");
            Destroy(managerObject);
            _networkManager = null;
            return null;
        }

        DontDestroyOnLoad(managerObject);
        managerObject.SetActive(true);
        MultiplayerMatchBroadcasts.RegisterClientHandlers(_networkManager);
        return _networkManager;
    }

    private DefaultPrefabObjects LoadPrefabCollection()
    {
        DefaultPrefabObjects prefabCollection = Resources.Load<DefaultPrefabObjects>(PrefabCollectionResourcePath);
        if (prefabCollection != null)
            return prefabCollection;

        Debug.LogError($"FishNet prefab collection was not found at Resources/{PrefabCollectionResourcePath}.");
        return null;
    }

    private void EnsureSessionDriverSpawned()
    {
        if (_networkManager == null || !_networkManager.IsServerStarted || MultiplayerSessionDriver.Instance != null)
            return;

        if (_sessionDriverPrefab == null)
            _sessionDriverPrefab = Resources.Load<GameObject>(SessionDriverResourcePath);

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
    }
}
