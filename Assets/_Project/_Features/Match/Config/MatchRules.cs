using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Throne/Match/Match Rules")]
public sealed class MatchRules : ScriptableObject
{
    [SerializeField] private int minPlayerCount = 1;
    [SerializeField] private int maxPlayerCount = 8;

    [SerializeField] private int minBotCount = 0;
    [SerializeField] private int maxBotCount = 5;

    [SerializeField] private int minMatchDurationSeconds = 10;
    [SerializeField] private int maxMatchDurationSeconds = 1800;

    [SerializeField] private int minRespawnTimeSeconds = 0;
    [SerializeField] private int maxRespawnTimeSeconds = 30;

    [SerializeField] private int minSuddenDeathTimeSeconds = 10;
    [SerializeField] private int maxSuddenDeathTimeSeconds = 1800;

    [SerializeField] private int minTrailLength = 1;
    [SerializeField] private int maxTrailLength = 10;

    public int MinPlayerCount => minPlayerCount;
    public int MaxPlayerCount => maxPlayerCount;

    public int MinBotCount => minBotCount;
    public int MaxBotCount => maxBotCount;

    public int MinMatchDurationSeconds => minMatchDurationSeconds;
    public int MaxMatchDurationSeconds => maxMatchDurationSeconds;

    public int MinRespawnTimeSeconds => minRespawnTimeSeconds;
    public int MaxRespawnTimeSeconds => maxRespawnTimeSeconds;

    public int MinSuddenDeathTimeSeconds => minSuddenDeathTimeSeconds;
    public int MaxSuddenDeathTimeSeconds => maxSuddenDeathTimeSeconds;

    public int MinTrailLength => minTrailLength;
    public int MaxTrailLength => maxTrailLength;

    public MatchSettings Validate(MatchSettings settings)
    {
        if (settings == null)
        {
            Debug.LogWarning("[MatchRules] Cannot validate null settings. Returning a safe new MatchSettings instance.");
            settings = new MatchSettings();
        }

        if (!Enum.IsDefined(typeof(MatchMode), settings.MatchMode))
        {
            Debug.LogWarning($"[MatchRules] Match mode '{settings.MatchMode}' is invalid. Using {MatchMode.Deathmatch}.");
            settings.MatchMode = MatchMode.Deathmatch;
        }

        settings.PlayerCount = ClampWithWarning(nameof(settings.PlayerCount), settings.PlayerCount, minPlayerCount, maxPlayerCount);
        settings.BotCount = ClampWithWarning(nameof(settings.BotCount), settings.BotCount, minBotCount, maxBotCount);
        settings.MatchDurationSeconds = ClampWithWarning(nameof(settings.MatchDurationSeconds), settings.MatchDurationSeconds, minMatchDurationSeconds, maxMatchDurationSeconds);
        settings.RespawnTimeSeconds = ClampWithWarning(nameof(settings.RespawnTimeSeconds), settings.RespawnTimeSeconds, minRespawnTimeSeconds, maxRespawnTimeSeconds);
        settings.SuddenDeathTimeSeconds = ClampWithWarning(nameof(settings.SuddenDeathTimeSeconds), settings.SuddenDeathTimeSeconds, minSuddenDeathTimeSeconds, maxSuddenDeathTimeSeconds);
        settings.TrailLength = ClampWithWarning(nameof(settings.TrailLength), settings.TrailLength, minTrailLength, maxTrailLength);

        return settings;
    }

    private static int ClampWithWarning(string fieldName, int value, int min, int max)
    {
        int clamped = Mathf.Clamp(value, min, max);
        if (clamped != value)
            Debug.LogWarning($"[MatchRules] Clamped {fieldName} from {value} to {clamped}.");

        return clamped;
    }
}
