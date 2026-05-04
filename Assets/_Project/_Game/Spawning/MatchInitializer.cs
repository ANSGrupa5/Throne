using System.Collections.Generic;
using UnityEngine;

public class MatchInitializer : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool requireClearSpawn = true;

    public bool TryGetSpawnSpot(out SpawnSpot spot, SpawnSpot exclude = null, Vector3? referencePosition = null)
    {
        if (requireClearSpawn)
        {
            return SpawnSpot.TryGetRandomAvailableSpot(out spot, obstacleMask, exclude, referencePosition);
        }

        spot = GetAnyAvailableSpot(exclude, referencePosition);
        return spot != null;
    }

    public bool TryReserveSpawnSpot(out SpawnSpot spot, SpawnSpot exclude = null, Vector3? referencePosition = null)
    {
        if (!TryGetSpawnSpot(out spot, exclude, referencePosition))
            return false;

        spot.SetAvailability(false);
        return true;
    }

    public void ReleaseSpawnSpot(SpawnSpot spot)
    {
        if (spot != null)
            spot.SetAvailability(true);
    }

    private static SpawnSpot GetAnyAvailableSpot(SpawnSpot exclude, Vector3? referencePosition)
    {
        IReadOnlyList<SpawnSpot> activeSpots = SpawnSpot.Active;
        if (activeSpots.Count == 0)
            return null;

        SpawnSpot chosen = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < activeSpots.Count; i++)
        {
            SpawnSpot current = activeSpots[i];
            if (current == null || !current.IsAvailable || current == exclude)
                continue;

            if (!referencePosition.HasValue)
                return current;

            float distance = (current.Position - referencePosition.Value).sqrMagnitude;
            if (distance < bestScore)
            {
                bestScore = distance;
                chosen = current;
            }
        }

        return chosen;
    }
}
