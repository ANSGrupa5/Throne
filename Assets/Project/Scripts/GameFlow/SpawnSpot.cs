using System.Collections.Generic;
using UnityEngine;

public class SpawnSpot : MonoBehaviour
{
	private static readonly List<SpawnSpot> ActiveSpots = new List<SpawnSpot>();

	[SerializeField, Min(0f)] private float occupancyRadius = 2f;
	[SerializeField] private bool isAvailable = true;

	public Vector3 Position => transform.position;
	public Quaternion Rotation => transform.rotation;
	public float OccupancyRadius => occupancyRadius;
	public bool IsAvailable => isAvailable;

	private void OnEnable()
	{
		if (!ActiveSpots.Contains(this))
			ActiveSpots.Add(this);
	}

	private void OnDisable()
	{
		ActiveSpots.Remove(this);
	}

	public void SetAvailability(bool available)
	{
		isAvailable = available;
	}

	public bool IsClear(LayerMask obstacleMask)
	{
		return !Physics.CheckSphere(Position, occupancyRadius, obstacleMask, QueryTriggerInteraction.Ignore);
	}

	public static IReadOnlyList<SpawnSpot> Active => ActiveSpots;

	public static bool TryGetRandomAvailableSpot(out SpawnSpot spot, LayerMask obstacleMask, SpawnSpot exclude = null, Vector3? referencePosition = null)
	{
		spot = null;

		if (ActiveSpots.Count == 0)
			return false;

		List<SpawnSpot> candidates = new List<SpawnSpot>();
		for (int i = 0; i < ActiveSpots.Count; i++)
		{
			SpawnSpot current = ActiveSpots[i];
			if (current == null || !current.isAvailable || current == exclude)
				continue;

			if (!current.IsClear(obstacleMask))
				continue;

			candidates.Add(current);
		}

		if (candidates.Count == 0)
			return false;

		if (referencePosition.HasValue && candidates.Count > 1)
		{
			candidates.Sort((left, right) =>
			{
				float leftDistance = (left.Position - referencePosition.Value).sqrMagnitude;
				float rightDistance = (right.Position - referencePosition.Value).sqrMagnitude;
				return leftDistance.CompareTo(rightDistance);
			});

			int halfCount = Mathf.Max(1, candidates.Count / 2);
			int index = Random.Range(0, halfCount);
			spot = candidates[index];
			return true;
		}

		spot = candidates[Random.Range(0, candidates.Count)];
		return true;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = isAvailable ? Color.green : Color.red;
		Gizmos.DrawWireSphere(Position, occupancyRadius);
		Gizmos.DrawRay(Position, transform.forward * 2f);
	}
}
