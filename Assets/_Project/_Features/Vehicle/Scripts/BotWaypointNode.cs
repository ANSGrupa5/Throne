using System.Collections.Generic;
using UnityEngine;

public class BotWaypointNode : MonoBehaviour
{
    public static readonly List<BotWaypointNode> Active = new List<BotWaypointNode>();

    [SerializeField] private bool drawGizmos = true;

    private void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    public static BotWaypointNode FindClosest(Vector3 position)
    {
        BotWaypointNode best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < Active.Count; i++)
        {
            BotWaypointNode node = Active[i];
            if (node == null)
                continue;

            float d = Vector3.SqrMagnitude(node.transform.position - position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = node;
            }
        }

        return best;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
    }
}
