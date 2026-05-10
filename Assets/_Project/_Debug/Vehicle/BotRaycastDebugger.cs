using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BotRaycastDebugger : MonoBehaviour
{
    [SerializeField] private BotVehicleInput bot;
    [SerializeField] private bool drawOnlyWhenSelected = false;
    [SerializeField] private float hitSphereRadius = 0.18f;
    [SerializeField] private Color targetColor = new Color(0.3f, 1f, 0.55f, 0.9f);
    [SerializeField] private Color labelColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color modeColor = new Color(1f, 0.95f, 0.35f, 0.98f);
    [SerializeField, Min(0.01f)] private float labelOffset = 0.35f;

    private void Reset()
    {
        bot = GetComponent<BotVehicleInput>();
    }

    private void OnDrawGizmos()
    {
        if (drawOnlyWhenSelected)
            return;

        DrawSnapshot();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawOnlyWhenSelected)
            return;

        DrawSnapshot();
    }

    private void DrawSnapshot()
    {
        if (bot == null)
            bot = GetComponent<BotVehicleInput>();

        if (bot == null || !bot.TryGetDebugSnapshot(out BotVehicleInput.DebugSnapshot snapshot))
            return;

        if (snapshot.rays != null)
        {
            for (int i = 0; i < snapshot.rays.Length; i++)
            {
                var ray = snapshot.rays[i];
                Color color = ray.hit ? ray.color : new Color(ray.color.r, ray.color.g, ray.color.b, 0.35f);
                Gizmos.color = color;
                Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * ray.length);

                if (ray.hit)
                {
                    Gizmos.DrawSphere(ray.hitPoint, hitSphereRadius);
                    Gizmos.DrawRay(ray.hitPoint, ray.hitNormal * 0.8f);
                }

#if UNITY_EDITOR
                DrawRayLabel(ray, i);
#endif
            }
        }

        if (snapshot.hasTarget)
        {
            Gizmos.color = targetColor;
            Gizmos.DrawLine(transform.position, snapshot.targetPoint);
            Gizmos.DrawSphere(snapshot.targetPoint, 0.22f);
        }

#if UNITY_EDITOR
        DrawModeLabel(snapshot);
#endif
    }

#if UNITY_EDITOR
    private void DrawRayLabel(BotVehicleInput.RayDebugSample ray, int index)
    {
        Vector3 labelPosition = ray.origin + ray.direction * Mathf.Min(ray.length, ray.hit ? Vector3.Distance(ray.origin, ray.hitPoint) : ray.length * 0.65f);
        labelPosition += Vector3.up * labelOffset;

        string status = ray.hit ? "HIT" : "clear";
        string text = $"{ray.label} [{status}]";

        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = labelColor },
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10
        };

        Handles.Label(labelPosition, text, style);
    }

    private void DrawModeLabel(BotVehicleInput.DebugSnapshot snapshot)
    {
        Vector3 labelPosition = transform.position + Vector3.up * 2.5f;
        string text = $"Mode: {snapshot.mode}\nTurn: {snapshot.turn:0.00}";

        if (snapshot.hasTarget)
            text += $"\nTarget: {snapshot.targetPoint}";

        GUIStyle style = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = modeColor },
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            richText = true
        };

        Handles.Label(labelPosition, text, style);
    }
#endif
}
