using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MultiplayerRuntimeBootstrap : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string MultiplayerScenePrefix = "Multiplayer";
    private const string PrefabCollectionResourcePath = "Networking/DefaultPrefabObjects";
    private const string EditorPrefabCollectionAssetPath = "Assets/DefaultPrefabObjects.asset";
    private const string SessionDriverResourcePath = "Networking/MultiplayerSessionDriver";

    private static MultiplayerRuntimeBootstrap _instance;

    private NetworkManager _networkManager;
    private DefaultPrefabObjects _prefabCollection;
    private GameObject _sessionDriverPrefab;
    private string _joinAddress = "127.0.0.1";
    private bool _driverSpawnRequested;
    private bool _showJoinHud;
    private GameObject _connectionTypePanel;
    private GameObject _hostPanel;

    public static MultiplayerRuntimeBootstrap Instance => _instance;

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
        if (!IsActiveMultiplayerScene() || !_showJoinHud)
            return;

        EnsureNetworkManager();
        DrawJoinHud();
    }

    private void DrawJoinHud()
    {
        GUILayout.BeginArea(new Rect(16f, 16f, 320f, 170f), GUI.skin.box);
        GUILayout.Label("Join Game");

        if (_networkManager == null)
        {
            GUILayout.Label("FishNet bootstrap is unavailable.");
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label("Join Address");
        _joinAddress = GUILayout.TextField(_joinAddress ?? "127.0.0.1");

        GUILayout.Space(8f);
        if (GUILayout.Button("Join", GUILayout.Height(28f)))
        {
            StartClient();
        }

        GUILayout.EndArea();
    }

    public void HostGame()
    {
        EnsureNetworkManager();
        if (_networkManager == null)
            return;

        SetConnectionTypeVisible(false);
        SetHostPanelVisible(true);
        _showJoinHud = false;
        StartHost();
    }

    public void JoinGame()
    {
        EnsureNetworkManager();
        if (_networkManager == null)
            return;

        SetConnectionTypeVisible(false);
        SetHostPanelVisible(false);
        _showJoinHud = true;
    }

    public void BackToMainMenu()
    {
        StopNetworkingIfNeeded();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void StartMatch()
    {
        if (_networkManager == null || !_networkManager.IsServerStarted)
            return;

        MultiplayerSessionDriver driver = MultiplayerSessionDriver.Instance;
        if (driver != null && !driver.IsMatchRunning)
            driver.StartMatch();
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
        _showJoinHud = false;
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
        _showJoinHud = false;
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
            _showJoinHud = false;
            return;
        }

        EnsureNetworkManager();
        RefreshSceneUiState();
        if (_networkManager != null && _networkManager.IsServerStarted)
        {
            _driverSpawnRequested = true;
            EnsureSessionDriverSpawned();
        }
    }

    private void RefreshSceneUiState()
    {
        if (_networkManager == null)
            return;

        if (_networkManager.IsServerStarted)
        {
            SetConnectionTypeVisible(false);
            SetHostPanelVisible(true);
            _showJoinHud = false;
            return;
        }

        if (_networkManager.IsClientStarted)
        {
            SetConnectionTypeVisible(false);
            SetHostPanelVisible(false);
            _showJoinHud = false;
            return;
        }

        SetConnectionTypeVisible(true);
        SetHostPanelVisible(false);
        _showJoinHud = false;
    }

    private void SetConnectionTypeVisible(bool visible)
    {
        CacheScenePanels();
        if (_connectionTypePanel != null)
            _connectionTypePanel.SetActive(visible);
    }

    private void SetHostPanelVisible(bool visible)
    {
        CacheScenePanels();
        if (_hostPanel != null)
            _hostPanel.SetActive(visible);
    }

    private void CacheScenePanels()
    {
        if (_connectionTypePanel == null)
            _connectionTypePanel = GameObject.Find("ConnectionType");

        if (_hostPanel == null)
            _hostPanel = GameObject.Find("Panel");
    }

    private void EnsureNetworkManager()
    {
        if (_networkManager != null)
            return;

        _prefabCollection = LoadPrefabCollection();
        _sessionDriverPrefab = Resources.Load<GameObject>(SessionDriverResourcePath);

        if (_prefabCollection == null)
        {
            Debug.LogError($"Missing FishNet prefab collection. Expected Resources/{PrefabCollectionResourcePath}.");
            return;
        }

        if (_sessionDriverPrefab == null)
        {
            Debug.LogError($"Missing session driver prefab at Resources/{SessionDriverResourcePath}.");
            return;
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
            return;
        }

        DontDestroyOnLoad(managerObject);
        managerObject.SetActive(true);
    }

    private DefaultPrefabObjects LoadPrefabCollection()
    {
        DefaultPrefabObjects prefabCollection = Resources.Load<DefaultPrefabObjects>(PrefabCollectionResourcePath);
        if (prefabCollection != null)
            return prefabCollection;

#if UNITY_EDITOR
        prefabCollection = UnityEditor.AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(EditorPrefabCollectionAssetPath);
        if (prefabCollection != null)
        {
            Debug.LogWarning($"FishNet prefab collection was not found at Resources/{PrefabCollectionResourcePath}; using editor asset '{EditorPrefabCollectionAssetPath}'.");
            return prefabCollection;
        }
#endif

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
        _driverSpawnRequested = false;
    }
}
