using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MatchDefaults", menuName = "Game/Settings/MatchDefaults")]
public sealed class MatchDefaults : ScriptableObject
{
    public string DefaultArenaSceneName = "Neon City XL";
    public MatchMode DefaultMatchMode = MatchMode.KingOfTheHill;
    public float DefaultMatchDurationSeconds = 600f;
    public bool DefaultSuddenDeath;
    public int DefaultTrailLength = 1;
    public float DefaultRespawnTime = 5f;
    public float DefaultVehicleSpeed = 20f;
    public List<Color> TrailColorPalette = new()
    {
        new Color(0.95f, 0.24f, 0.24f),
        new Color(0.25f, 0.55f, 0.98f),
        new Color(0.28f, 0.82f, 0.42f),
        new Color(0.98f, 0.82f, 0.25f),
        new Color(0.90f, 0.35f, 0.85f),
        new Color(0.22f, 0.88f, 0.88f)
    };

    private void OnValidate()
    {
        if (TrailColorPalette == null)
            TrailColorPalette = new List<Color>();
    }
}
