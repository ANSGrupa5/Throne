using System.Collections.Generic;
using UnityEngine;

public class TrailEmitter : MonoBehaviour
{
    private struct TrailProfile
    {
        public float segmentSpacing;
        public float segmentLifetime;
        public float width;
        public float height;
        public float length;

        public TrailProfile(float spacing, float lifetime, float width, float height, float length)
        {
            segmentSpacing = spacing;
            segmentLifetime = lifetime;
            this.width = width;
            this.height = height;
            this.length = length;
        }
    }

    [SerializeField] private VehicleLife ownerLife;
    [SerializeField] private VehicleColorApplier ownerColorApplier;
    [SerializeField, Range(0, 3)] private int trailPreset = 1;
    [SerializeField, Min(0.01f)] private float spawnOffset = 0.15f;
    [SerializeField] private string trailLayerName = "Trail";
    [SerializeField] private Material segmentMaterial;

    private Rigidbody _rb;
    private Vector3 _lastSpawnPosition;
    private bool _hasSpawnPosition;
    private Color _trailColor = Color.white;
    private int _trailLayer;

    private static readonly TrailProfile[] Profiles =
    {
        new TrailProfile(0.75f, 2.5f, 0.72f, 2.0f, 0.72f),
        new TrailProfile(0.55f, 4.5f, 0.82f, 2.0f, 0.82f),
        new TrailProfile(0.40f, 7.5f, 0.92f, 2.0f, 0.92f),
        new TrailProfile(0.30f, 0f, 1.00f, 2.0f, 1.00f)
    };

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _trailLayer = LayerMask.NameToLayer(trailLayerName);
        if (_trailLayer < 0)
        {
            Debug.LogWarning($"[TrailEmitter] Layer '{trailLayerName}' was not found. Falling back to '{gameObject.layer}'.", this);
            _trailLayer = gameObject.layer;
        }

        if (ownerLife == null)
            ownerLife = GetComponent<VehicleLife>();
        if (ownerColorApplier == null)
            ownerColorApplier = GetComponent<VehicleColorApplier>();
    }

    private void OnEnable()
    {
        _hasSpawnPosition = false;
        _lastSpawnPosition = transform.position;
    }

    private void Update()
    {
        if (ownerLife != null && ownerLife.IsDead)
            return;

        if (_rb == null)
            return;

        Vector3 currentPosition = transform.position;
        if (!_hasSpawnPosition)
        {
            _lastSpawnPosition = currentPosition;
            _hasSpawnPosition = true;
            SpawnSegment(currentPosition);
            return;
        }

        float spacing = GetProfile().segmentSpacing;
        float distance = Vector3.Distance(currentPosition, _lastSpawnPosition);
        if (distance < spacing)
            return;

        SpawnSegment(currentPosition);
        _lastSpawnPosition = currentPosition;
    }

    public void Configure(VehicleLife life, Color trailColor, int preset)
    {
        ownerLife = life;
        trailPreset = Mathf.Clamp(preset, 0, 3);
        _trailColor = trailColor;

        if (ownerColorApplier == null)
            ownerColorApplier = GetComponent<VehicleColorApplier>();
        if (ownerColorApplier != null)
            ownerColorApplier.SetColor(trailColor);
    }

    private TrailProfile GetProfile()
    {
        return Profiles[Mathf.Clamp(trailPreset, 0, Profiles.Length - 1)];
    }

    private void SpawnSegment(Vector3 position)
    {
        TrailProfile profile = GetProfile();
        Vector3 spawnPosition = position + Vector3.up * (profile.height * 0.5f - spawnOffset);

        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = "TrailSegment";
        segment.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        segment.transform.localScale = new Vector3(profile.width, profile.height, profile.length);
        segment.layer = _trailLayer;

        Renderer renderer = segment.GetComponent<Renderer>();
        if (renderer != null && segmentMaterial != null)
            renderer.sharedMaterial = segmentMaterial;

        Collider collider = segment.GetComponent<Collider>();
        if (collider != null)
        {
            collider.gameObject.layer = _trailLayer;
            collider.isTrigger = true;
        }

        VehicleColorApplier colorApplier = segment.AddComponent<VehicleColorApplier>();
        colorApplier.SetColor(_trailColor);

        TrailSegment trailSegment = segment.AddComponent<TrailSegment>();
        string ownerDisplayName = ownerLife != null ? ownerLife.DisplayName : gameObject.name;
        string ownerId = ownerLife != null ? ownerLife.OwnerId : ownerDisplayName;
        trailSegment.Configure(ownerLife, ownerDisplayName, ownerId, _trailColor);

        if (ownerLife != null)
        {
            Collider[] ownerColliders = ownerLife.GetComponentsInChildren<Collider>(true);
            Collider segmentCollider = segment.GetComponent<Collider>();
            if (ownerColliders != null && segmentCollider != null)
            {
                for (int i = 0; i < ownerColliders.Length; i++)
                {
                    Collider ownerCollider = ownerColliders[i];
                    if (ownerCollider == null || ownerCollider == segmentCollider)
                        continue;

                    Physics.IgnoreCollision(segmentCollider, ownerCollider, true);
                }
            }
        }

        if (profile.segmentLifetime > 0f)
            Object.Destroy(segment, profile.segmentLifetime);
    }
}
