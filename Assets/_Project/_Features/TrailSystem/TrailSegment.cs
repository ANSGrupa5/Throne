using FishNet;
using UnityEngine;

public class TrailSegment : MonoBehaviour
{
    public static readonly System.Collections.Generic.List<TrailSegment> Active = new System.Collections.Generic.List<TrailSegment>();

    [SerializeField] private VehicleColorApplier colorApplier;
    [SerializeField] private Collider segmentCollider;

    private VehicleLife _ownerLife;
    private string _ownerDisplayName;
    private string _ownerId;

    public VehicleLife OwnerLife => _ownerLife;
    public string OwnerDisplayName => _ownerDisplayName;
    public string OwnerId => _ownerId;

    public void Configure(VehicleLife ownerLife, string ownerDisplayName, string ownerId, Color color)
    {
        _ownerLife = ownerLife;
        _ownerDisplayName = string.IsNullOrWhiteSpace(ownerDisplayName)
            ? (ownerLife != null ? ownerLife.DisplayName : string.Empty)
            : ownerDisplayName.Trim();
        _ownerId = string.IsNullOrWhiteSpace(ownerId)
            ? (ownerLife != null ? ownerLife.OwnerId : _ownerDisplayName)
            : ownerId.Trim();

        if (colorApplier == null)
            colorApplier = GetComponent<VehicleColorApplier>();
        if (colorApplier != null)
            colorApplier.SetColor(color);
    }

    private void Awake()
    {
        if (segmentCollider == null)
            segmentCollider = GetComponent<Collider>();

        if (colorApplier == null)
            colorApplier = GetComponent<VehicleColorApplier>();
    }

    private void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (InstanceFinder.IsClientStarted && !InstanceFinder.IsServerStarted)
            return;

        if (_ownerLife == null || other == null)
            return;

        VehicleLife victim = ResolveVictim(other);
        if (victim == null || victim == _ownerLife || !victim.CanBeKilled)
            return;

        victim.Kill(_ownerLife.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        OnTriggerEnter(other);
    }

    private VehicleLife ResolveVictim(Collider other)
    {
        VehicleLife victim = other.GetComponentInParent<VehicleLife>();
        if (victim != null)
            return victim;

        if (other.attachedRigidbody != null)
            return other.attachedRigidbody.GetComponent<VehicleLife>();

        return null;
    }
}
