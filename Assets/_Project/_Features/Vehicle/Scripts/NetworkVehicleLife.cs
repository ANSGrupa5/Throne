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
            RpcApplyRespawn();
    }

    [ObserversRpc]
    private void RpcApplyDeath()
    {
        if (IsServerInitialized || _life == null)
            return;

        _life.ApplyReplicatedDeath(null);
    }

    [ObserversRpc]
    private void RpcApplyRespawn()
    {
        if (IsServerInitialized || _life == null)
            return;

        _life.ApplyReplicatedRespawn();
    }

    private bool CanApplyAuthoritativeGameplay()
    {
        if (!InstanceFinder.IsServerStarted && !InstanceFinder.IsClientStarted)
            return true;

        return IsServerInitialized;
    }
}
