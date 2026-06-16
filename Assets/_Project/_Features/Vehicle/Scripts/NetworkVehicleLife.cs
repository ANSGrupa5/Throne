using FishNet;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(VehicleLife))]
public class NetworkVehicleLife : NetworkBehaviour
{
    private VehicleLife _life;

    private void Awake()
    {
        _life = GetComponent<VehicleLife>();
        if (_life != null)
        {
            _life.SetGameplayAuthority(CanApplyAuthoritativeGameplay);
            _life.Died += HandleLifeDied;
            _life.Respawned += HandleLifeRespawned;
        }
    }

    private void OnDestroy()
    {
        if (_life == null)
            return;

        _life.Died -= HandleLifeDied;
        _life.Respawned -= HandleLifeRespawned;
        _life.SetGameplayAuthority(null);
    }

    private void HandleLifeDied(VehicleLife victim, GameObject killer)
    {
        if (victim != _life)
            return;

        if (IsSpawned && IsServerInitialized)
            RpcApplyDeath();
    }

    private void HandleLifeRespawned(VehicleLife life)
    {
        if (life != _life)
            return;

        if (IsSpawned && IsServerInitialized)
            ObserversRespawn(life.transform.position, life.transform.rotation);
    }

    [Server]
    public void ServerRespawn(Vector3 position, Quaternion rotation)
    {
        if (_life == null)
            return;

        if (!IsServerInitialized)
            return;

        Debug.Log(
            $"[NetworkVehicleLife] ServerRespawn object='{name}' " +
            $"owner={(Owner != null ? Owner.ClientId.ToString() : "<null>")}");

        _life.RespawnAt(position, rotation);
    }

    [ObserversRpc]
    private void RpcApplyDeath()
    {
        if (IsServerInitialized || _life == null)
            return;

        _life.ApplyReplicatedDeath(null);
    }

    [ObserversRpc]
    private void ObserversRespawn(Vector3 position, Quaternion rotation)
    {
        if (IsServerInitialized)
            return;

        Debug.Log($"[NetworkVehicleLife] ObserversRespawn object='{name}'");
        ApplyRespawnPresentation(position, rotation);
    }

    private void ApplyRespawnPresentation(Vector3 position, Quaternion rotation)
    {
        if (_life != null)
            _life.ApplyReplicatedRespawn(position, rotation);

        VehicleDeathSequence deathSequence = GetComponent<VehicleDeathSequence>();
        if (deathSequence != null)
            deathSequence.ResetDeathPresentation();

        NetworkPlayerVehicleInput input = GetComponent<NetworkPlayerVehicleInput>();
        if (input != null)
            input.RefreshAfterRespawn();

        VehicleController vehicleController = GetComponent<VehicleController>();
        if (vehicleController != null)
            vehicleController.ResetPresentationState();
    }

    private bool CanApplyAuthoritativeGameplay()
    {
        if (!InstanceFinder.IsServerStarted && !InstanceFinder.IsClientStarted)
            return true;

        return IsServerInitialized;
    }
}
