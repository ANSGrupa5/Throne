using UnityEditor;
using UnityEngine;

public static class Milestone7UiPrefabSceneMigrator
{
    [MenuItem("Throne/Tools/Migrations/Run Milestone 7 UI Cleanup (Retained Stub)")]
    public static void Run()
    {
        Debug.LogWarning(
            "The recovered Milestone 7 migrator fragment is retained as .old and is not safe to run directly. " +
            "Use the later milestone patchers for repeatable repairs.");
    }
}
