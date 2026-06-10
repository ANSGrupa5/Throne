using System.Collections;
using FishNet;
using UnityEngine;

public class VehicleDeathSequence : MonoBehaviour
{
    [SerializeField] private VehicleLife life;
    [SerializeField] private VehicleCameraController cameraController;
    [SerializeField, Min(0f), Tooltip("How long the death camera placeholder stays before switching to spectator or respawn.")] private float deathAnimationTime = 3f;
    [SerializeField] private Transform spectatorCameraTarget;
    [SerializeField] private string spectatorCameraName = "SpectatorCamera";

    private bool _isHandlingDeath;

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

        if (PlayerProfileStats.Instance.CheckIfPlayerIsKiller(killer))
            PlayerProfileStats.Instance.IncOppsElim();

        if (PlayerProfileStats.Instance.CheckIfPlayerIsEliminated(victim))
            PlayerProfileStats.Instance.IncTimesElim();

        StartCoroutine(DeathRoutine());
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
            if (life != null && life.CanRunLocalRespawn())
                life.Respawn();

            if (cameraController != null)
            {
                cameraController.ClearSpectatorTarget();
                cameraController.SetPresetByLabel("Follow");
            }
        }

        _isHandlingDeath = false;
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
}
