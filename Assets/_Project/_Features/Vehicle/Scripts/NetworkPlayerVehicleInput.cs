using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class NetworkPlayerVehicleInput : NetworkBehaviour, IVehicleCommandSource
{
    private VehicleCommand _localCommand;
    private VehicleCommand _replicatedCommand;
    private Camera _playerCamera;
    private AudioListener _audioListener;

    private void Awake()
    {
        CacheLocalPresentation();
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
        if (!Owner.IsLocalClient)
            return;

        _localCommand = ReadLocalCommand();
        SubmitCurrentCommand();
    }

    public VehicleCommand GetCommand()
    {
        if (!IsSpawned)
            return ReadLocalCommand();

        if (IsServerStarted)
            return Owner.IsLocalClient ? _localCommand : _replicatedCommand;

        return _localCommand;
    }

    [ServerRpc]
    private void SubmitCommandServerRpc(float turn, bool boost)
    {
        _replicatedCommand = new VehicleCommand(turn, boost);
    }

    private void SubmitCurrentCommand()
    {
        if (!IsClientStarted || !Owner.IsLocalClient)
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
        if (_playerCamera == null)
            _playerCamera = GetComponentInChildren<Camera>(true);
        if (_audioListener == null)
            _audioListener = GetComponentInChildren<AudioListener>(true);
    }

    private void RefreshLocalPresentation()
    {
        CacheLocalPresentation();
        bool isLocalOwner = !IsSpawned || Owner.IsLocalClient;

        if (_playerCamera != null)
            _playerCamera.gameObject.SetActive(isLocalOwner);

        if (_audioListener != null)
            _audioListener.enabled = isLocalOwner;
    }
}
