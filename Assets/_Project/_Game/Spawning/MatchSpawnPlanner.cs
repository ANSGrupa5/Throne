using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class MatchSpawnPlanner
{
    private readonly LayerMask _obstacleMask;

    public MatchSpawnPlanner(LayerMask obstacleMask)
    {
        _obstacleMask = obstacleMask;
    }

    public bool TrySelectSpawnSpots(int totalToSpawn, out List<SpawnSpot> chosen)
    {
        chosen = null;

        var spots = SpawnSpot.Active.ToList();
        if (spots.Count == 0)
        {
            Debug.LogWarning("No SpawnSpots found in scene.");
            return false;
        }

        if (totalToSpawn > spots.Count)
        {
            Debug.LogError($"Match initialization aborted: requested {totalToSpawn} entities but only {spots.Count} SpawnSpots are available.");
            return false;
        }

        chosen = SelectSpawnSpots(spots, totalToSpawn);
        if (chosen.Count < totalToSpawn)
        {
            Debug.LogError($"Match initialization aborted: could not reserve enough SpawnSpots ({chosen.Count}/{totalToSpawn}).");
            return false;
        }

        return true;
    }

    private List<SpawnSpot> SelectSpawnSpots(List<SpawnSpot> available, int count)
    {
        List<SpawnSpot> candidates = new List<SpawnSpot>(available.Where(s => s.IsAvailable));
        List<SpawnSpot> result = new List<SpawnSpot>();

        if (count <= 0 || candidates.Count == 0) return result;

        // Filter by clear spots first
        candidates = candidates.Where(s => s.IsClear(_obstacleMask)).ToList();

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
}
