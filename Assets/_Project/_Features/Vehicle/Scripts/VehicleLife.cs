using System.Collections;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleLife : MonoBehaviour
{
    public static event Action<VehicleLife, GameObject> AnyVehicleDied;

    [SerializeField, Min(0f)] private float respawnProtectionTime = 1f;

    private VehicleController _vehicleController;
    private VehicleColorApplier _colorApplier;
    private TrailEmitter _trailEmitter;
    private Rigidbody _rb;
    private Collider[] _colliders;
    private Renderer[] _renderers;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private bool _isDead;
    private bool _isInvulnerable;
    private string _displayName;
    private string _ownerId;
    private Func<bool> _canApplyGameplay;

    public bool IsDead => _isDead;
    public bool CanBeKilled => !_isDead && !_isInvulnerable;
    public GameObject LastKiller { get; private set; }
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? gameObject.name : _displayName;
    public string OwnerId => string.IsNullOrWhiteSpace(_ownerId) ? DisplayName : _ownerId;
    public Vector3 SpawnPosition => _spawnPosition;
    public Quaternion SpawnRotation => _spawnRotation;
    public event Action<VehicleLife, GameObject> Died;
    public event Action<VehicleLife> Respawned;

    private void Awake()
    {
        _vehicleController = GetComponent<VehicleController>();
        _colorApplier = GetComponent<VehicleColorApplier>();
        _trailEmitter = GetComponent<TrailEmitter>();
        _rb = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>(true);
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!CanApplyGameplay())
            return;

        if (!CanBeKilled)
            return;

        int layer = collision.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Map Boundry") || layer == LayerMask.NameToLayer("Dead Zone"))
        {
            Kill(collision.gameObject);
        }
    }

    public void ConfigureSpawn(Vector3 position, Quaternion rotation)
    {
        _spawnPosition = position;
        _spawnRotation = rotation;
    }

    public void ConfigureIdentity(string displayName, string ownerId)
    {
        _displayName = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName.Trim();
        _ownerId = string.IsNullOrWhiteSpace(ownerId) ? _displayName : ownerId.Trim();
    }

    public bool Kill(GameObject killer)
    {
        if (!CanApplyGameplay())
            return false;

        if (!CanBeKilled)
            return false;

        ApplyDeath(killer, invokeEvent: true);
        return true;
    }

    public void HideDeadBody()
    {
        SetVisibility(false);
    }

    private Coroutine _invulnerabilityCoroutine;

    public void Respawn()
    {
        if (!CanApplyGameplay())
            return;

        ApplyRespawn(invokeEvent: true);
    }

    public void RespawnAt(Vector3 position, Quaternion rotation)
    {
        ConfigureSpawn(position, rotation);
        Respawn();
    }

    public void GrantInvulnerability(float duration)
    {
        if (!CanApplyGameplay())
            return;

        _isInvulnerable = true;
        if (_invulnerabilityCoroutine != null)
        {
            StopCoroutine(_invulnerabilityCoroutine);
        }
        _invulnerabilityCoroutine = StartCoroutine(ClearInvulnerabilityAfterDelay(duration));
    }

    private IEnumerator ClearInvulnerabilityAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _isInvulnerable = false;
    }

    public void SetGameplayAuthority(Func<bool> canApplyGameplay)
    {
        _canApplyGameplay = canApplyGameplay;
    }

    public void ApplyReplicatedDeath(GameObject killer)
    {
        ApplyDeath(killer, invokeEvent: true);
    }

    public void ApplyReplicatedRespawn()
    {
        ApplyRespawn(invokeEvent: true);
    }

    public void ApplyReplicatedRespawn(Vector3 position, Quaternion rotation)
    {
        ConfigureSpawn(position, rotation);
        ApplyRespawn(invokeEvent: true);
    }

    private void ApplyDeath(GameObject killer, bool invokeEvent)
    {
        LastKiller = killer;
        _isDead = true;
        FreezePhysicsForDeath();
        SetGameplayActive(false);
        if (invokeEvent)
        {
            Died?.Invoke(this, killer);
            AnyVehicleDied?.Invoke(this, killer);
        }
    }

    private void ApplyRespawn(bool invokeEvent)
    {
        transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        SetVisibility(true);
        SetGameplayActive(true);
        if (_vehicleController != null)
            _vehicleController.ResetPresentationState();

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.detectCollisions = true;
        }

        _isInvulnerable = respawnProtectionTime > 0f;
        _isDead = false;
        LastKiller = null;

        if (_invulnerabilityCoroutine != null)
            StopCoroutine(_invulnerabilityCoroutine);

        if (respawnProtectionTime > 0f)
            _invulnerabilityCoroutine = StartCoroutine(ClearInvulnerabilityAfterDelay(respawnProtectionTime));
        else
            _isInvulnerable = false;

        if (invokeEvent)
            Respawned?.Invoke(this);
    }

    private bool CanApplyGameplay()
    {
        return _canApplyGameplay == null || _canApplyGameplay();
    }

    private void FreezePhysicsForDeath()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
        }
    }

    private void SetGameplayActive(bool active)
    {
        if (_vehicleController != null)
            _vehicleController.enabled = active;

        if (_trailEmitter != null)
            _trailEmitter.enabled = active;

        if (_colorApplier != null)
            _colorApplier.enabled = active;

        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                Collider collider = _colliders[i];
                if (collider == null)
                    continue;

                if (collider is WheelCollider)
                    continue;

                collider.enabled = active;
            }
        }
    }

    private void SetVisibility(bool visible)
    {
        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer != null)
                    renderer.enabled = visible;
            }
        }
    }
}
