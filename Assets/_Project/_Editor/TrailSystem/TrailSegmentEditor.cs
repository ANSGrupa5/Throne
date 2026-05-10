using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrailSegment))]
public class TrailSegmentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TrailSegment segment = (TrailSegment)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ownership", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Owner Id", segment.OwnerId);
        }
    }
}
