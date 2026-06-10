using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Settings/GameSettings")]
public class GameSettings : ScriptableObject
{
    private const int MinPlayers = 2;
    private const int MaxPlayers = 6;
    private const int MinGameMode = 0;
    private const int MaxGameMode = 1;
    public const float MinMatchDuration = 10f;
    public const float MaxMatchDuration = 600f;
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
    public string arenaSceneName = "Neon City XL"; // Only from Assets/Project/Scenes/Arenas/
    [Min(MinMatchDuration)] public float matchDuration = 600f; // 10-600 seconds
    [Min(1f)] public float respawnTime = 5f; // 1-5 seconds
    public bool isSuddenDeath = false; // Sudden Death mode: when enabled, players have only 1 life and gamez zone is shrinking after some time
    [Min(30f)] public float suddenDeathTime = 30f; // 30-120 seconds
    [Min(20)] public float vehicleSpeed = 20f; // 20-50 units per second
    [Range(0,3)] public int trailLength = 1; // 0-3 (0 - short, 1 - medium, 2 - long, 3 - permanent)
    public List<Color> trailColorPalette = new List<Color>
    {
        new Color(0.95f, 0.24f, 0.24f), // Red
        new Color(0.25f, 0.55f, 0.98f), // Blue
        new Color(0.28f, 0.82f, 0.42f), // Green
        new Color(0.98f, 0.82f, 0.25f), // Yellow
        new Color(0.90f, 0.35f, 0.85f), // Magenta
        new Color(0.22f, 0.88f, 0.88f)  // Cyan
    };

    private void OnValidate()
    {
        maxPlayers = Mathf.Clamp(maxPlayers, MinPlayers, MaxPlayers);
        gameMode = Mathf.Clamp(gameMode, MinGameMode, MaxGameMode);
        matchDuration = Mathf.Clamp(matchDuration, MinMatchDuration, MaxMatchDuration);
        respawnTime = Mathf.Clamp(respawnTime, MinRespawnTime, MaxRespawnTime);
        suddenDeathTime = Mathf.Clamp(suddenDeathTime, MinSuddenDeathTime, MaxSuddenDeathTime);
        vehicleSpeed = Mathf.Clamp(vehicleSpeed, MinVehicleSpeed, MaxVehicleSpeed);
        trailLength = Mathf.Clamp(trailLength, MinTrailLength, MaxTrailLength);

        string[] availableSceneNames = GetSceneNamesFromBuildSettings();
        if (availableSceneNames.Length == 0)
        {
            Debug.LogWarning("[GameSettings] No scenes found in Build Settings.");
            return;
        }
        if (!availableSceneNames.Contains(arenaSceneName))
        {
            switch (arenaSceneName)
            {
                case "GameOver":
                case "MainMenu":
                case "LobbyScene":
                case "TestEnvironment":
                    Debug.LogWarning($"[GameSettings] Scene '{arenaSceneName}' is not an arena scene. Please choose a valid arena from Build Settings.");
                    return;
            }

            string previousName = arenaSceneName;
            arenaSceneName = "Neon City XL";
            Debug.LogWarning(
                $"[GameSettings] Scene '{previousName}' was not found in Build Settings. " +
                $"Falling back to '{arenaSceneName}'."
            );
        }

        if (trailColorPalette == null)
            trailColorPalette = new List<Color>();
    }
    private static string[] GetSceneNamesFromBuildSettings()
    {
        int count = SceneManager.sceneCountInBuildSettings;

        string[] sceneNames = new string[count];

        for (int i = 0; i < count; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            sceneNames[i] = Path.GetFileNameWithoutExtension(scenePath);
        }

        return sceneNames;
    }

    public int GetMinTrailLength()
    {
        return MinTrailLength;
    }

    public int GetMaxTrailLength()
    {
        return MaxTrailLength;
    }
}
