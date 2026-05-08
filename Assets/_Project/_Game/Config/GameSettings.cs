using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Settings/GameSettings")]
public class GameSettings : ScriptableObject
{
    private const int MinPlayers = 2;
    private const int MaxPlayers = 6;
    private const int MinGameMode = 0;
    private const int MaxGameMode = 1;
    private const float MinMatchDuration = 60f;
    private const float MaxMatchDuration = 600f;
    private const float MinRespawnTime = 1f;
    private const float MaxRespawnTime = 5f;
    private const float MinSuddenDeathTime = 30f;
    private const float MaxSuddenDeathTime = 120f;
    private const float MinVehicleSpeed = 20f;
    private const float MaxVehicleSpeed = 50f;
    private const int MinTrailLength = 0;
    private const int MaxTrailLength = 3;

    private bool isSingleplayer = true; // 1-singleplayer, 2-multiplayer
    public bool IsSingleplayer
    {
        get { return isSingleplayer; }
        set { isSingleplayer = value; }
    }
    [Min(2)] public int maxPlayers = 5; // 2-6 players (host+1-5 clients)
    [Range(0,1)] public int gameMode = 0; // 0 - king of the hill, 1 - Team Deathmatch
    [Min(60f)] public float matchDuration = 600f; // 60-600 seconds
    [Min(1f)] public float respawnTime = 5f; // 1-5 seconds
    public bool isSuddenDeath = false; // Sudden Death mode: when enabled, players have only 1 life and gamez zone is shrinking after some time
    [Min(30f)] public float suddenDeathTime = 30f; // 30-120 seconds
    [Min(20)] public float vehicleSpeed = 20f; // 20-50 units per second
    [Range(0,3)] public int trailLength = 1; // 0-3 (0 - short, 1 - medium, 2 - long, 3 - permanent)

    private void OnValidate()
    {
        maxPlayers = Mathf.Clamp(maxPlayers, MinPlayers, MaxPlayers);
        gameMode = Mathf.Clamp(gameMode, MinGameMode, MaxGameMode);
        matchDuration = Mathf.Clamp(matchDuration, MinMatchDuration, MaxMatchDuration);
        respawnTime = Mathf.Clamp(respawnTime, MinRespawnTime, MaxRespawnTime);
        suddenDeathTime = Mathf.Clamp(suddenDeathTime, MinSuddenDeathTime, MaxSuddenDeathTime);
        vehicleSpeed = Mathf.Clamp(vehicleSpeed, MinVehicleSpeed, MaxVehicleSpeed);
        trailLength = Mathf.Clamp(trailLength, MinTrailLength, MaxTrailLength);
    }
}
