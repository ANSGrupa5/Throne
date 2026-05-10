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

    public bool IsDead => _isDead;
    public bool CanBeKilled => !_isDead && !_isInvulnerable;
    public GameObject LastKiller { get; private set; }
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? gameObject.name : _displayName;
    public string OwnerId => string.IsNullOrWhiteSpace(_ownerId) ? DisplayName : _ownerId;

    private void Awake()
    {
        _vehicleController = GetComponent<VehicleController>();
        _colorApplier = GetComponent<VehicleColorApplier>();
        _trailEmitter = GetComponent<TrailEmitter>();
        _rb = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>(true);
        _renderers = GetComponentsInChildren<Renderer>(true);
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
        if (!CanBeKilled)
            return false;

        LastKiller = killer;
        _isDead = true;
        FreezePhysicsForDeath();
        SetGameplayActive(false);
        AnyVehicleDied?.Invoke(this, killer);
        return true;
    }

    public void HideDeadBody()
    {
        SetVisibility(false);
    }

    public void Respawn()
    {
        transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        SetVisibility(true);
        SetGameplayActive(true);

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = false;
            _rb.detectCollisions = true;
        }

        _isInvulnerable = respawnProtectionTime > 0f;
        _isDead = false;
        LastKiller = null;

        if (respawnProtectionTime > 0f)
        {
            StartCoroutine(ClearInvulnerabilityAfterDelay());
        }
        else
        {
            _isInvulnerable = false;
        }
    }

    private IEnumerator ClearInvulnerabilityAfterDelay()
    {
        yield return new WaitForSecondsRealtime(respawnProtectionTime);
        _isInvulnerable = false;
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
