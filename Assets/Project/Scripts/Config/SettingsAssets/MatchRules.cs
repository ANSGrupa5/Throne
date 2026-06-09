using UnityEngine;

[CreateAssetMenu(fileName = "MatchRules", menuName = "Game/Settings/MatchRules")]
public sealed class MatchRules : ScriptableObject
{
    [Header("Players")]
    [Min(1)] public int MinPlayers = 2;
    [Min(1)] public int MaxPlayers = 6;

    [Header("Match")]
    [Min(0f)] public float MinMatchDurationSeconds = 10f;
    [Min(0f)] public float MaxMatchDurationSeconds = 600f;
    [Min(0)] public int MinTrailLength = 0;
    [Min(0)] public int MaxTrailLength = 3;

    [Header("Runtime")]
    [Min(0f)] public float MinRespawnTime = 1f;
    [Min(0f)] public float MaxRespawnTime = 5f;
    [Min(0f)] public float MinVehicleSpeed = 20f;
    [Min(0f)] public float MaxVehicleSpeed = 50f;

    public float ClampMatchDuration(float value)
    {
        return Mathf.Clamp(value, MinMatchDurationSeconds, MaxMatchDurationSeconds);
    }

    public int ClampTrailLength(int value)
    {
        return Mathf.Clamp(value, MinTrailLength, MaxTrailLength);
    }

    public int ClampParticipantCount(int value)
    {
        return Mathf.Clamp(value, MinPlayers, MaxPlayers);
    }
}
