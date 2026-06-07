using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VehicleLife))]
public class VehicleLifeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VehicleLife life = (VehicleLife)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Owner Id", life.OwnerId);
        }
    }
}
