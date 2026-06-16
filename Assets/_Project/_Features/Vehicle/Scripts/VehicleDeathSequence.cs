using System.Collections;
using FishNet.Object;
using UnityEngine;

public class VehicleDeathSequence : MonoBehaviour
{
    [SerializeField] private VehicleLife life;
    [SerializeField] private VehicleCameraController cameraController;
    [SerializeField, Min(0f), Tooltip("How long the death camera placeholder stays before switching to spectator or respawn.")] private float deathAnimationTime = 3f;
    [SerializeField] private Transform spectatorCameraTarget;
    [SerializeField] private string spectatorCameraName = "SpectatorCamera";

    private bool _isHandlingDeath;
    private Coroutine _deathCoroutine;

    private void Awake()
    {
        if (life == null)
            life = GetComponent<VehicleLife>();
        if (cameraController == null)
            cameraController = GetComponentInChildren<VehicleCameraController>(true);
    }

    private void OnEnable()
    {
        VehicleLife.AnyVehicleDied += HandleAnyVehicleDied;
    }

    private void OnDisable()
    {
        VehicleLife.AnyVehicleDied -= HandleAnyVehicleDied;
    }

    private void HandleAnyVehicleDied(VehicleLife victim, GameObject killer)
    {
        if (victim == null || victim != life || _isHandlingDeath)
            return;

        if (ShouldRunLocalDeathPresentation())
        {
            if (StatsManager.Instance.CheckIfPlayerIsKiller(killer))
                StatsManager.Instance.IncOppsElim();

            if (StatsManager.Instance.CheckIfPlayerIsEliminated(victim))
                StatsManager.Instance.IncTimesElim();

            _deathCoroutine = StartCoroutine(DeathRoutine());
            return;
        }

        if (ShouldScheduleServerNetworkRespawn())
            _deathCoroutine = StartCoroutine(ServerNetworkRespawnRoutine());
    }

    private bool ShouldRunLocalDeathPresentation()
    {
        if (!MultiplayerRuntimeMode.IsFishNetActive)
            return true;

        NetworkObject networkObject = life != null ? life.GetComponent<NetworkObject>() : null;
        if (networkObject == null)
            return MultiplayerRuntimeMode.IsServerOrSingleplayerAuthority;

        return networkObject.Owner != null && networkObject.Owner.IsLocalClient;
    }

    private IEnumerator DeathRoutine()
    {
        _isHandlingDeath = true;

        if (cameraController != null)
            cameraController.SetPresetByLabel("TopDown");

        yield return new WaitForSecondsRealtime(deathAnimationTime);

        if (life != null)
            life.HideDeadBody();

        GameSessionRuntime session = GameSessionBootstrap.CurrentSession;
        int gameMode = session != null ? session.gameMode : 0;

        if (gameMode == 0)
        {
            Transform spectatorTarget = ResolveSpectatorTarget();
            if (cameraController != null && spectatorTarget != null)
                cameraController.SetSpectatorTarget(spectatorTarget);
        }
        else
        {
            if (life != null && CanRespawnRuntimeLife())
            {
                NetworkVehicleLife networkLife = life.GetComponent<NetworkVehicleLife>();
                if (MultiplayerRuntimeMode.IsFishNetActive && networkLife != null)
                    networkLife.ServerRespawn(life.SpawnPosition, life.SpawnRotation);
                else
                    life.Respawn();
            }

            if (cameraController != null)
            {
                cameraController.ClearSpectatorTarget();
                cameraController.SetPresetByLabel("Follow");
            }
        }

        _isHandlingDeath = false;
        _deathCoroutine = null;
    }

    private IEnumerator ServerNetworkRespawnRoutine()
    {
        _isHandlingDeath = true;

        yield return new WaitForSecondsRealtime(deathAnimationTime);

        if (life != null && CanRespawnRuntimeLife())
        {
            NetworkVehicleLife networkLife = life.GetComponent<NetworkVehicleLife>();
            if (networkLife != null)
                networkLife.ServerRespawn(life.SpawnPosition, life.SpawnRotation);
        }

        _isHandlingDeath = false;
        _deathCoroutine = null;
    }

    public void ResetDeathPresentation()
    {
        if (_deathCoroutine != null)
        {
            StopCoroutine(_deathCoroutine);
            _deathCoroutine = null;
        }

        _isHandlingDeath = false;

        if (cameraController != null)
        {
            cameraController.ClearSpectatorTarget();
            cameraController.SetPresetByLabel("Follow");
        }

        Debug.Log($"[VehicleDeathSequence] ResetDeathPresentation object='{name}'");
    }

    private bool ShouldScheduleServerNetworkRespawn()
    {
        if (!MultiplayerRuntimeMode.IsFishNetActive || !MultiplayerRuntimeMode.IsFishNetServerStarted)
            return false;

        if (GetCurrentGameMode() == 0)
            return false;

        NetworkVehicleLife networkLife = life != null ? life.GetComponent<NetworkVehicleLife>() : null;
        return networkLife != null && networkLife.IsServerInitialized;
    }

    private bool CanRespawnRuntimeLife()
    {
        if (!MultiplayerRuntimeMode.IsFishNetActive)
            return true;

        NetworkVehicleLife networkLife = life != null ? life.GetComponent<NetworkVehicleLife>() : null;
        if (networkLife == null)
            return MultiplayerRuntimeMode.IsServerOrSingleplayerAuthority;

        if (!MultiplayerRuntimeMode.IsFishNetServerStarted)
            return false;

        return networkLife.IsServerInitialized;
    }

    private Transform ResolveSpectatorTarget()
    {
        if (spectatorCameraTarget != null)
            return spectatorCameraTarget;

        GameObject sceneSpectator = GameObject.Find(spectatorCameraName);
        if (sceneSpectator != null)
            return sceneSpectator.transform;

        return null;
    }

    private static int GetCurrentGameMode()
    {
        GameSessionRuntime session = GameSessionBootstrap.CurrentSession;
        return session != null ? session.gameMode : 0;
    }
}
