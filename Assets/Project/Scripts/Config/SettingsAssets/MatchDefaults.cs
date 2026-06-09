using UnityEngine;

[CreateAssetMenu(fileName = "MatchDefaults", menuName = "Game/Settings/MatchDefaults")]
public sealed class MatchDefaults : ScriptableObject
{
    [Header("Scene")]
    public string DefaultArenaSceneName = "Neon City XL";

    [Header("Match")]
    public MatchMode DefaultMatchMode = MatchMode.KingOfTheHill;
    public float DefaultMatchDurationSeconds = 600f;
    public bool DefaultSuddenDeath;
    public int DefaultTrailLength = 1;

    [Header("Runtime")]
    public float DefaultRespawnTime = 5f;
    public float DefaultVehicleSpeed = 20f;

    [Header("Trail Colors")]
    [SerializeField] private TrailColorPalette trailColorPalette;

    public TrailColorPalette TrailColorPalette => trailColorPalette;
}
