using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchInitializer : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BotsSettings botsSettings;
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private PlayerLook playerLook;

    [Header("Spawn")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Min(0f)] private float spawnInterval = 0.25f;
    [SerializeField] private bool spawnPlayerLast = true;

    public static event Action OnMatchStart;

    private readonly List<GameObject> _spawned = new List<GameObject>();

    private void Start()
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        var spots = SpawnSpot.Active.ToList();
        if (spots.Count == 0)
        {
            Debug.LogWarning("No SpawnSpots found in scene.");
            yield break;
        }

        int totalBots = 0;
        if (botsSettings != null)
        {
            foreach (var e in botsSettings.bots)
                totalBots += Mathf.Max(0, e.count);
        }

        int totalToSpawn = totalBots + (playerLook != null && playerLook.playerPrefab != null ? 1 : 0);

        List<SpawnSpot> chosen = SelectSpawnSpots(spots, totalToSpawn);

        int index = 0;

        // Spawn bots
        if (botsSettings != null)
        {
            foreach (var entry in botsSettings.bots)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    if (index >= chosen.Count) break;
                    SpawnAt(entry.prefab, chosen[index]);
                    index++;
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
        }

        // Spawn player
        if (!spawnPlayerLast)
        {
            // If player should be spawned earlier, implementation would go here.
        }

        if (playerLook != null && playerLook.playerPrefab != null)
        {
            if (index < chosen.Count)
            {
                SpawnAt(playerLook.playerPrefab, chosen[index]);
                index++;
            }
        }

        // Wait one frame to ensure all Awake/Start run
        yield return null;

        // Start countdown after all spawns
        yield return StartCoroutine(CountdownAndStart(5));
    }

    private void SpawnAt(GameObject prefab, SpawnSpot spot)
    {
        if (prefab == null || spot == null) return;

        Vector3 pos = spot.Position;
        Quaternion rot = spot.Rotation;
        GameObject go = Instantiate(prefab, pos, rot);
        _spawned.Add(go);
    }

    private List<SpawnSpot> SelectSpawnSpots(List<SpawnSpot> available, int count)
    {
        List<SpawnSpot> candidates = new List<SpawnSpot>(available.Where(s => s.IsAvailable));
        List<SpawnSpot> result = new List<SpawnSpot>();

        if (count <= 0 || candidates.Count == 0) return result;

        // Filter by clear spots first
        candidates = candidates.Where(s => s.IsClear(obstacleMask)).ToList();

        if (candidates.Count == 0)
        {
            // fallback to any available
            candidates = new List<SpawnSpot>(available.Where(s => s.IsAvailable));
        }

        if (count >= candidates.Count)
        {
            result.AddRange(candidates);
            return result;
        }

        // Anti-clump selection: pick first random, then pick spots that maximize distance to existing selection
        System.Random rng = new System.Random();
        int firstIndex = rng.Next(0, candidates.Count);
        result.Add(candidates[firstIndex]);
        candidates.RemoveAt(firstIndex);

        while (result.Count < count && candidates.Count > 0)
        {
            SpawnSpot best = null;
            float bestMinDist = -1f;
            foreach (var c in candidates)
            {
                float minDist = float.MaxValue;
                foreach (var chosen in result)
                {
                    float d = Vector3.SqrMagnitude(c.Position - chosen.Position);
                    if (d < minDist) minDist = d;
                }
                if (minDist > bestMinDist)
                {
                    bestMinDist = minDist;
                    best = c;
                }
            }
            if (best == null) break;
            result.Add(best);
            candidates.Remove(best);
        }

        return result;
    }

    private IEnumerator CountdownAndStart(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            Debug.Log(i);
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("GO");
        OnMatchStart?.Invoke();
        yield break;
    }
}
