using UnityEngine;

[CreateAssetMenu(fileName = "VehiclePrefabSet", menuName = "Throne/Vehicle/Vehicle Prefab Set")]
public sealed class VehiclePrefabSet : ScriptableObject
{
    [SerializeField] private GameObject playerVehiclePrefab;
    [SerializeField] private GameObject botVehiclePrefab;

    public GameObject PlayerVehiclePrefab => playerVehiclePrefab;
    public GameObject BotVehiclePrefab => botVehiclePrefab;
}
