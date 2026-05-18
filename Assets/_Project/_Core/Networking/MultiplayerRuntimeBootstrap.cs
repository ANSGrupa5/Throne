using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MultiplayerRuntimeBootstrap : MonoBehaviour
{
    private const string MultiplayerScenePrefix = "multi ";
    private const string PrefabCollectionResourcePath = "Networking/DefaultPrefabObjects";
    private const string SessionDriverResourcePath = "Networking/MultiplayerSessionDriver";

    private static MultiplayerRuntimeBootstrap _instance;

    private NetworkManager _networkManager;
    private DefaultPrefabObjects _prefabCollection;
    private GameObject _sessionDriverPrefab;
    private string _joinAddress = "127.0.0.1";
    private bool _driverSpawnRequested;

    public static bool IsMultiplayerScene(Scene scene)
    {
        return scene.IsValid() && scene.name.StartsWith(MultiplayerScenePrefix, System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsActiveMultiplayerScene()
    {
        return IsMultiplayerScene(SceneManager.GetActiveScene());
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
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (!IsActiveMultiplayerScene() || _networkManager == null)
            return;

        if (_networkManager.IsServerStarted && _driverSpawnRequested)
            EnsureSessionDriverSpawned();
    }

    private void OnGUI()
    {
        if (!IsActiveMultiplayerScene())
            return;

        EnsureNetworkManager();
        DrawHud();
    }

    private void DrawHud()
    {
        GUILayout.BeginArea(new Rect(16f, 16f, 320f, 220f), GUI.skin.box);
        GUILayout.Label("Multiplayer Arena");
        GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");

        if (_networkManager == null)
        {
            GUILayout.Label("FishNet bootstrap is unavailable.");
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label($"Server: {(_networkManager.IsServerStarted ? "Online" : "Offline")}");
        GUILayout.Label($"Client: {(_networkManager.IsClientStarted ? "Online" : "Offline")}");
        GUILayout.Space(6f);

        if (_networkManager.IsOffline)
        {
            if (GUILayout.Button("Start Host", GUILayout.Height(28f)))
                StartHost();

            GUILayout.Space(4f);
            GUILayout.Label("Join Address");
            _joinAddress = GUILayout.TextField(_joinAddress ?? "127.0.0.1");

            if (GUILayout.Button("Join Client", GUILayout.Height(28f)))
                StartClient();
        }
        else
        {
            if (_networkManager.IsServerStarted)
            {
                MultiplayerSessionDriver driver = MultiplayerSessionDriver.Instance;
                int connectedPlayers = _networkManager.ServerManager != null ? _networkManager.ServerManager.Clients.Count : 0;
                GUILayout.Label($"Connected Players: {Mathf.Max(1, connectedPlayers)}");

                bool canStartMatch = driver != null && !driver.IsMatchRunning;
                GUI.enabled = canStartMatch;
                if (GUILayout.Button("Start Match", GUILayout.Height(28f)))
                    driver.StartMatch();
                GUI.enabled = true;
            }

            if (GUILayout.Button("Stop Networking", GUILayout.Height(28f)))
                StopNetworking();
        }

        GUILayout.EndArea();
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

    private void StartClient()
    {
        EnsureNetworkManager();
        if (_networkManager == null)
            return;

        string address = string.IsNullOrWhiteSpace(_joinAddress) ? "127.0.0.1" : _joinAddress.Trim();
        _networkManager.ClientManager.StartConnection(address);
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
        MultiplayerMatchState.SetFrozen(false);
        Time.timeScale = 1f;
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
        if (_networkManager != null && _networkManager.IsServerStarted)
        {
            _driverSpawnRequested = true;
            EnsureSessionDriverSpawned();
        }
    }

    private void EnsureNetworkManager()
    {
        if (_networkManager != null)
            return;

        _prefabCollection = Resources.Load<DefaultPrefabObjects>(PrefabCollectionResourcePath);
        _sessionDriverPrefab = Resources.Load<GameObject>(SessionDriverResourcePath);

        if (_prefabCollection == null)
        {
            Debug.LogError($"Missing FishNet prefab collection at Resources/{PrefabCollectionResourcePath}.");
            return;
        }

        if (_sessionDriverPrefab == null)
        {
            Debug.LogError($"Missing session driver prefab at Resources/{SessionDriverResourcePath}.");
            return;
        }

        GameObject managerObject = new("FishNetRuntime");
        managerObject.SetActive(false);
        DontDestroyOnLoad(managerObject);

        _networkManager = managerObject.AddComponent<NetworkManager>();
        managerObject.AddComponent<Tugboat>();
        _networkManager.SpawnablePrefabs = _prefabCollection;

        managerObject.SetActive(true);
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
        _driverSpawnRequested = false;
    }
}
