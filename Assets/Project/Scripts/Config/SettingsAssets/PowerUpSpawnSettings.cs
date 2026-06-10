using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpSpawnSettings", menuName = "Game/Settings/PowerUpSpawnSettings")]
public sealed class PowerUpSpawnSettings : ScriptableObject
{
    [System.Serializable]
    public sealed class PowerUpPrefabEntry
    {
        public GameObject prefab;
        [Range(0f, 100f)] public float weight = 1f;
    }

    [Header("Prefabs")]
    [SerializeField] private List<PowerUpPrefabEntry> powerUpPrefabs = new();

    [Header("Timing")]
    [SerializeField, Min(0f)] private float initialSpawnDelay = 2f;
    [SerializeField, Min(0f)] private float minSpawnInterval = 5f;
    [SerializeField, Min(0f)] private float maxSpawnInterval = 15f;

    [Header("Limits")]
    [SerializeField, Min(0)] private int maxPowerUpsOnMap = 3;

    public IReadOnlyList<PowerUpPrefabEntry> PowerUpPrefabs => powerUpPrefabs;
    public float InitialSpawnDelay => Mathf.Max(0f, initialSpawnDelay);
    public float MinSpawnInterval => Mathf.Max(0f, minSpawnInterval);
    public float MaxSpawnInterval => Mathf.Max(MinSpawnInterval, maxSpawnInterval);
    public int MaxPowerUpsOnMap => Mathf.Max(0, maxPowerUpsOnMap);
}
