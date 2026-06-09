using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

public class PlayerVehicleInput : NetworkBehaviour, IVehicleCommandSource
{
    private const string MainCameraName = "Main Camera";
    private const string SpectatorCameraName = "SpectatorCamera";

    private VehicleCommand _localCommand;
    private VehicleCommand _replicatedCommand;
    private Camera _playerCamera;
    private AudioListener _audioListener;
    private bool _hasStarted;

    private void Awake()
    {
        CacheLocalPresentation();
    }

    private void Start()
    {
        _hasStarted = true;
        RefreshLocalPresentation();
    }

    private void OnEnable()
    {
        if (_hasStarted)
            RefreshLocalPresentation();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        RefreshLocalPresentation();
        SubmitCurrentCommand();
    }

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        base.OnOwnershipClient(prevOwner);
        RefreshLocalPresentation();
        SubmitCurrentCommand();
    }

    private void Update()
    {
        if (!CanReadLocalInput())
            return;

        _localCommand = ReadLocalCommand();
        SubmitCurrentCommand();
    }

    public VehicleCommand GetCommand()
    {
        if (IsSingleplayerSession())
            return ReadLocalCommand();

        if (!IsSpawned)
            return ReadLocalCommand();

        if (IsServerStarted)
            return CanReadLocalInput() ? _localCommand : _replicatedCommand;

        return _localCommand;
    }

    [ServerRpc]
    private void SubmitCommandServerRpc(float turn, bool boost)
    {
        _replicatedCommand = new VehicleCommand(turn, boost);
    }

    private void SubmitCurrentCommand()
    {
        if (IsSingleplayerSession())
            return;

        if (!IsClientStarted || !IsClientInitialized || !IsSpawned || !CanReadLocalInput())
            return;

        SubmitCommandServerRpc(_localCommand.turn, _localCommand.boost);
    }

    private VehicleCommand ReadLocalCommand()
    {
        float turn = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(turn) < 0.01f)
        {
            KeyCode left = InputManager.Instance != null ? InputManager.Instance.TurnLeft : KeyCode.A;
            KeyCode right = InputManager.Instance != null ? InputManager.Instance.TurnRight : KeyCode.D;

            if (Input.GetKey(left))
                turn -= 1f;
            if (Input.GetKey(right))
                turn += 1f;
        }

        return new VehicleCommand(turn, false);
    }

    private void CacheLocalPresentation()
    {
        if (_playerCamera == null)
            _playerCamera = GetComponentInChildren<Camera>(true);
        if (_audioListener == null)
            _audioListener = GetComponentInChildren<AudioListener>(true);
    }

    public void RefreshLocalPresentation()
    {
        CacheLocalPresentation();
        bool isLocalOwner = CanReadLocalInput();

        if (_playerCamera != null)
        {
            _playerCamera.gameObject.SetActive(isLocalOwner);
            _playerCamera.enabled = isLocalOwner;
        }

        if (_audioListener != null)
            _audioListener.enabled = isLocalOwner;

        if (isLocalOwner && _playerCamera != null)
            DisableSceneFallbackCameras();
    }

    private bool CanReadLocalInput()
    {
        if (IsSingleplayerSession())
            return true;

        if (!IsSpawned)
            return !IsClientStarted && !IsServerStarted;

        return IsClientInitialized && Owner.IsLocalClient;
    }

    private bool IsSingleplayerSession()
    {
        return GameSessionBootstrap.CurrentSession != null && GameSessionBootstrap.CurrentSession.isSingleplayer;
    }

    private void DisableSceneFallbackCameras()
    {
        Camera[] sceneCameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sceneCameras.Length; i++)
        {
            Camera sceneCamera = sceneCameras[i];
            if (sceneCamera == null || sceneCamera == _playerCamera || sceneCamera.transform.IsChildOf(transform))
                continue;

            string cameraName = sceneCamera.gameObject.name;
            if (cameraName == MainCameraName || cameraName == SpectatorCameraName)
                sceneCamera.gameObject.SetActive(false);
        }
    }
}
