using UnityEngine;
using System.Collections.Generic;

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
    public List<PowerUpPrefabEntry> powerUpPrefabs;
    public Transform[] spawnPoints;
    
    [SerializeField] private float initialSpawnDelay = 2f;
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 15f;
    [SerializeField] private int maxPowerUpsOnMap = 3;

    private float _nextSpawnTime;
    private List<GameObject> _activePowerUps = new List<GameObject>();

    private void Start()
    {
        _nextSpawnTime = Time.time + initialSpawnDelay;
    }

    private void Update()
    {
        // Oczyszczanie zebranych/zniszczonych powerupów z listy
        _activePowerUps.RemoveAll(item => item == null);

        if (Time.time >= _nextSpawnTime && _activePowerUps.Count < maxPowerUpsOnMap)
        {
            SpawnRandomPowerUp();
            ScheduleNextSpawn();
        }
    }

    private void SpawnRandomPowerUp()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Count == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return;

        List<Transform> availableSpawnPoints = new List<Transform>();
        foreach (Transform sp in spawnPoints)
        {
            bool isOccupied = false;
            foreach(GameObject activePU in _activePowerUps)
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

        GameObject prefabToSpawn = GetRandomPrefabByWeight();
        if (prefabToSpawn != null)
        {
            GameObject spawnedPU = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            _activePowerUps.Add(spawnedPU);
        }
    }

    private GameObject GetRandomPrefabByWeight()
    {
        float totalWeight = 0;
        foreach (var entry in powerUpPrefabs)
            totalWeight += entry.weight;

        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (var entry in powerUpPrefabs)
        {
            currentWeight += entry.weight;
            if (randomValue <= currentWeight)
                return entry.prefab;
        }

        return powerUpPrefabs[0].prefab;
    }

    private void ScheduleNextSpawn()
    {
        _nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}
