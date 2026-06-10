using UnityEngine;
using System.Collections.Generic;
using FishNet;

public class PowerUpSpawner : MonoBehaviour
{
    [System.Serializable]
    public class PowerUpPrefabEntry
    {
        public GameObject prefab;
        [Range(0f, 100f)]
        public float weight = 1f; // Waga na potrzeby losowania
    }

    [Header("Spawning Settings")]
    [SerializeField] private PowerUpSpawnSettings spawnSettings;

    [Header("Legacy Spawning Settings")]
    public List<PowerUpPrefabEntry> powerUpPrefabs;
    public Transform[] spawnPoints;
    
    [SerializeField] private float initialSpawnDelay = 2f;
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 15f;
    [SerializeField] private int maxPowerUpsOnMap = 3;

    private float _nextSpawnTime;
    private readonly List<GameObject> _activePowerUps = new List<GameObject>();
    private readonly List<WeightedPrefab> _weightedPrefabs = new List<WeightedPrefab>();

    private readonly struct WeightedPrefab
    {
        public readonly GameObject Prefab;
        public readonly float CumulativeWeight;

        public WeightedPrefab(GameObject prefab, float cumulativeWeight)
        {
            Prefab = prefab;
            CumulativeWeight = cumulativeWeight;
        }
    }

    private void Start()
    {
        if (InstanceFinder.IsClientStarted || InstanceFinder.IsServerStarted)
        {
            enabled = false;
            return;
        }

        _nextSpawnTime = Time.time + GetInitialSpawnDelay();
    }

    private void Update()
    {
        // Oczyszczanie zebranych/zniszczonych powerupów z listy
        _activePowerUps.RemoveAll(item => item == null);

        int maxPowerUps = GetMaxPowerUpsOnMap();
        if (maxPowerUps <= 0)
            return;

        if (Time.time >= _nextSpawnTime && _activePowerUps.Count < maxPowerUps)
        {
            SpawnRandomPowerUp();
            ScheduleNextSpawn();
        }
    }

    private void SpawnRandomPowerUp()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        List<Transform> availableSpawnPoints = new List<Transform>();
        foreach (Transform sp in spawnPoints)
        {
            if (sp == null)
                continue;

            bool isOccupied = false;
            foreach (GameObject activePU in _activePowerUps)
            {
                if (activePU != null && Vector3.Distance(activePU.transform.position, sp.position) < 1f)
                {
                    isOccupied = true;
                    break;
                }
            }
            if (!isOccupied)
                availableSpawnPoints.Add(sp);
        }

        if (availableSpawnPoints.Count == 0)
            return;

        Transform spawnPoint = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];

        if (!TryGetRandomPrefabByWeight(out GameObject prefabToSpawn))
            return;

        GameObject spawnedPU = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        _activePowerUps.Add(spawnedPU);
    }

    private bool TryGetRandomPrefabByWeight(out GameObject prefab)
    {
        prefab = null;
        _weightedPrefabs.Clear();

        if (!TryCollectSettingsPrefabs(out float totalWeight))
            TryCollectLegacyPrefabs(out totalWeight);

        if (totalWeight <= 0f || _weightedPrefabs.Count == 0)
            return false;

        float randomValue = Random.Range(0f, totalWeight);

        foreach (WeightedPrefab entry in _weightedPrefabs)
        {
            if (randomValue <= entry.CumulativeWeight)
            {
                prefab = entry.Prefab;
                return prefab != null;
            }
        }

        prefab = _weightedPrefabs[_weightedPrefabs.Count - 1].Prefab;
        return prefab != null;
    }

    private void ScheduleNextSpawn()
    {
        float minInterval = GetMinSpawnInterval();
        float maxInterval = Mathf.Max(minInterval, GetMaxSpawnInterval());
        _nextSpawnTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    private bool TryCollectSettingsPrefabs(out float totalWeight)
    {
        totalWeight = 0f;
        if (spawnSettings == null || spawnSettings.PowerUpPrefabs == null)
            return false;

        foreach (PowerUpSpawnSettings.PowerUpPrefabEntry entry in spawnSettings.PowerUpPrefabs)
        {
            if (entry == null)
                continue;

            AddWeightedPrefab(entry.prefab, entry.weight, ref totalWeight);
        }

        return totalWeight > 0f;
    }

    private bool TryCollectLegacyPrefabs(out float totalWeight)
    {
        totalWeight = 0f;
        if (powerUpPrefabs == null)
            return false;

        foreach (PowerUpPrefabEntry entry in powerUpPrefabs)
        {
            if (entry == null)
                continue;

            AddWeightedPrefab(entry.prefab, entry.weight, ref totalWeight);
        }

        return totalWeight > 0f;
    }

    private void AddWeightedPrefab(GameObject prefab, float weight, ref float totalWeight)
    {
        if (prefab == null || weight <= 0f)
            return;

        totalWeight += weight;
        _weightedPrefabs.Add(new WeightedPrefab(prefab, totalWeight));
    }

    private bool HasValidSpawnSettings()
    {
        if (spawnSettings == null || spawnSettings.PowerUpPrefabs == null)
            return false;

        foreach (PowerUpSpawnSettings.PowerUpPrefabEntry entry in spawnSettings.PowerUpPrefabs)
        {
            if (entry != null && entry.prefab != null && entry.weight > 0f)
                return true;
        }

        return false;
    }

    private float GetInitialSpawnDelay()
    {
        return HasValidSpawnSettings() ? spawnSettings.InitialSpawnDelay : Mathf.Max(0f, initialSpawnDelay);
    }

    private float GetMinSpawnInterval()
    {
        return HasValidSpawnSettings() ? spawnSettings.MinSpawnInterval : Mathf.Max(0f, minSpawnInterval);
    }

    private float GetMaxSpawnInterval()
    {
        return HasValidSpawnSettings() ? spawnSettings.MaxSpawnInterval : Mathf.Max(GetMinSpawnInterval(), maxSpawnInterval);
    }

    private int GetMaxPowerUpsOnMap()
    {
        return HasValidSpawnSettings() ? spawnSettings.MaxPowerUpsOnMap : Mathf.Max(0, maxPowerUpsOnMap);
    }
}
