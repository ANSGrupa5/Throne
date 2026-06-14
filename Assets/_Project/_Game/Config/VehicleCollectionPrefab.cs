using UnityEngine;

[CreateAssetMenu(fileName = "VehicleCollectionPrefab", menuName = "Game/Settings/VehicleCollectionPrefab")]
public class VehicleCollectionPrefab : ScriptableObject
{
    public GameObject playerVehiclePrefab;
    public GameObject botVehiclePrefab;
}
