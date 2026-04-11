using UnityEngine;

public class InputDiagnostic : MonoBehaviour
{
    // Logs keyboard and axis activity for quick input debugging.
    private void Update()
    {
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");

        if (v != 0f || h != 0f)
            Debug.Log($"[Input] Vertical={v:+0.##;-0.##;0}  Horizontal={h:+0.##;-0.##;0}");

        if (Input.GetKeyDown(KeyCode.W)) Debug.Log("[Input] W pressed");
        if (Input.GetKeyDown(KeyCode.S)) Debug.Log("[Input] S pressed");
        if (Input.GetKeyDown(KeyCode.A)) Debug.Log("[Input] A pressed");
        if (Input.GetKeyDown(KeyCode.D)) Debug.Log("[Input] D pressed");
        if (Input.GetKeyDown(KeyCode.R)) Debug.Log("[Input] R pressed");
    }

    // Draws a small runtime overlay with current axis values.
    private void OnGUI()
    {
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");

        GUI.color = Color.black;
        GUI.Label(new Rect(11, 11, 260, 80), GetDiagText(v, h));
        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, 10, 260, 80), GetDiagText(v, h));
    }

    // Formats the axis state into a compact multi-line string.
    private static string GetDiagText(float v, float h)
    {
        return $"[InputDiagnostic]\n" +
               $"  Vertical  (W/S): {v:+0.00;-0.00; 0.00}\n" +
               $"  Horizontal(A/D): {h:+0.00;-0.00; 0.00}";
    }
}
