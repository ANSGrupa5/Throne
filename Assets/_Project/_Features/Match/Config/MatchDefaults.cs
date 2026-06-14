using UnityEngine;

[CreateAssetMenu(menuName = "Throne/Match/Match Defaults")]
public sealed class MatchDefaults : ScriptableObject
{
    [SerializeField] private MatchSettings settings;

    public MatchSettings CreateSettings()
    {
        if (settings == null)
        {
            Debug.LogWarning("[MatchDefaults] No settings template is assigned. Returning a new MatchSettings instance.");
            return new MatchSettings();
        }

        return settings.Clone();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        settings?.ArenaScene?.SyncSceneNameFromAsset();
    }
#endif
}