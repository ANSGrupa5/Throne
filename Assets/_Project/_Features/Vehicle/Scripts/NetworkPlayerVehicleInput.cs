using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class NetworkPlayerVehicleInput : NetworkBehaviour, IVehicleCommandSource
{
    private const string NetworkInputLogPrefix = "[NetworkPlayerVehicleInput]";

    private VehicleCommand _localCommand;
    private VehicleCommand _replicatedCommand;
    private Camera _playerCamera;
    private AudioListener _audioListener;
    private VehicleCameraController _cameraController;
    private VehicleController _vehicleController;
    private PlayerVehicleInput _singleplayerInput;
    private bool _hasLocalInputAuthority;
    private bool _hasSentObserverPresentationSteer;
    private float _lastObserverPresentationSteer;

    private void Awake()
    {
        CacheLocalPresentation();
        DisableDuplicateLocalInputs();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        DisableDuplicateLocalInputs();
        RefreshLocalInputState();
        RefreshLocalPresentation();
        SubmitCurrentCommand();
        WarnIfSpawnedWithoutValidOwner();
    }

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        base.OnOwnershipClient(prevOwner);
        RefreshLocalInputState();
        RefreshLocalPresentation();
        SubmitCurrentCommand();
        WarnIfSpawnedWithoutValidOwner();
    }

    private void Update()
    {
        RefreshLocalInputState();
        if (!_hasLocalInputAuthority)
            return;

        _localCommand = ReadLocalCommand();
        ApplyLocalPresentationSteer(_localCommand.turn);
        SubmitCurrentCommand();
    }

    public void RefreshAfterRespawn()
    {
        RefreshLocalInputState();
        RefreshLocalPresentation();

        if (_vehicleController != null)
            _vehicleController.ResetPresentationState();
    }

    public VehicleCommand GetCommand()
    {
        if (!IsSpawned)
            return VehicleCommand.Neutral;

        if (IsServerStarted)
            return _hasLocalInputAuthority ? _localCommand : _replicatedCommand;

        return _hasLocalInputAuthority ? _localCommand : VehicleCommand.Neutral;
    }

    [ServerRpc]
    private void SubmitCommandServerRpc(float turn, bool boost)
    {
        _replicatedCommand = new VehicleCommand(turn, boost);

        if (!_hasSentObserverPresentationSteer || Mathf.Abs(_lastObserverPresentationSteer - turn) > 0.01f)
        {
            _hasSentObserverPresentationSteer = true;
            _lastObserverPresentationSteer = turn;
            ObserversSetPresentationSteer(turn);
        }
    }

    private void SubmitCurrentCommand()
    {
        if (!IsClientStarted || !_hasLocalInputAuthority)
            return;

        SubmitCommandServerRpc(_localCommand.turn, _localCommand.boost);
    }

    private VehicleCommand ReadLocalCommand()
    {
        float turn = Input.GetAxisRaw("Horizontal");
        return new VehicleCommand(turn, false);
    }

    private void CacheLocalPresentation()
    {
        if (_vehicleController == null)
            _vehicleController = GetComponent<VehicleController>();
        if (_cameraController == null)
            _cameraController = GetComponentInChildren<VehicleCameraController>(true);
        if (_playerCamera == null)
            _playerCamera = GetComponentInChildren<Camera>(true);
        if (_audioListener == null)
            _audioListener = GetComponentInChildren<AudioListener>(true);
        if (_singleplayerInput == null)
            _singleplayerInput = GetComponentInChildren<PlayerVehicleInput>(true);
    }

    private void RefreshLocalPresentation()
    {
        CacheLocalPresentation();
        bool isLocalOwner = HasLocalInputAuthority();

        if (_cameraController != null)
            _cameraController.SetCameraActive(isLocalOwner);

        if (_playerCamera != null)
            _playerCamera.enabled = isLocalOwner;

        if (_audioListener != null)
            _audioListener.enabled = isLocalOwner;

        if (isLocalOwner)
            DisableSpectatorCameraForLocalPlayer();
    }

    private bool IsLocalOwner()
    {
        return Owner != null && Owner.IsLocalClient;
    }

    private bool HasLocalInputAuthority()
    {
        return IsSpawned && Owner != null && Owner.IsLocalClient;
    }

    private void RefreshLocalInputState()
    {
        _hasLocalInputAuthority = HasLocalInputAuthority();
    }

    private void ApplyLocalPresentationSteer(float steer)
    {
        CacheLocalPresentation();

        if (_vehicleController == null)
            return;

        _vehicleController.SetPresentationSteer(steer);
    }

    [ObserversRpc(BufferLast = true)]
    private void ObserversSetPresentationSteer(float steer)
    {
        if (IsOwner)
            return;

        CacheLocalPresentation();
        if (_vehicleController != null)
            _vehicleController.SetPresentationSteer(steer);
    }

    private void DisableDuplicateLocalInputs()
    {
        PlayerVehicleInput[] localInputs = GetComponentsInChildren<PlayerVehicleInput>(true);

        for (int i = 0; i < localInputs.Length; i++)
        {
            PlayerVehicleInput localInput = localInputs[i];
            if (localInput == null)
                continue;

            localInput.enabled = false;

            Debug.LogWarning(
                $"{NetworkInputLogPrefix} Disabled duplicate PlayerVehicleInput on network vehicle '{name}'. " +
                "NetworkPlayerVehicle1 should not use non-network local input.");
        }
    }

    private void WarnIfSpawnedWithoutValidOwner()
    {
        if (IsSpawned && (Owner == null || Owner.ClientId < 0))
        {
            Debug.LogWarning(
                $"{NetworkInputLogPrefix} Spawned network player vehicle '{name}' has no valid owner. " +
                "This vehicle cannot be controlled by any client. Check MatchInitializer spawn ownership.");
        }
    }

    private static void DisableSpectatorCameraForLocalPlayer()
    {
        GameObject spectator = GameObject.Find("SpectatorCamera");
        if (spectator == null)
            return;

        Camera spectatorCamera = spectator.GetComponent<Camera>();
        if (spectatorCamera != null)
            spectatorCamera.enabled = false;

        AudioListener spectatorListener = spectator.GetComponent<AudioListener>();
        if (spectatorListener != null)
            spectatorListener.enabled = false;
    }
}
