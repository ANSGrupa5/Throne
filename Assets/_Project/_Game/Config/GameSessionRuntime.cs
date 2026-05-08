using System.Collections.Generic;
using UnityEngine;

public class GameSessionRuntime
{
    [System.Serializable]
    public class BotSpawnEntry
    {
        public GameObject prefab;
        public int count;
    }

    public bool isSingleplayer;
    public int maxPlayers;
    public int gameMode;
    public float matchDuration;
    public float respawnTime;
    public bool isSuddenDeath;
    public float suddenDeathTime;
    public float vehicleSpeed;
    public int trailLength;

    public GameObject playerPrefab;
    public string playerDisplayName;

    public readonly List<BotSpawnEntry> bots = new List<BotSpawnEntry>();

    public static GameSessionRuntime FromDefaults(GameSettings gameSettings, BotsSettings botsSettings, PlayerLook playerLook)
    {
        var session = new GameSessionRuntime();

        if (gameSettings != null)
        {
            session.isSingleplayer = gameSettings.IsSingleplayer;
            session.maxPlayers = gameSettings.maxPlayers;
            session.gameMode = gameSettings.gameMode;
            session.matchDuration = gameSettings.matchDuration;
            session.respawnTime = gameSettings.respawnTime;
            session.isSuddenDeath = gameSettings.isSuddenDeath;
            session.suddenDeathTime = gameSettings.suddenDeathTime;
            session.vehicleSpeed = gameSettings.vehicleSpeed;
            session.trailLength = gameSettings.trailLength;
        }
        else
        {
            session.isSingleplayer = true;
            session.maxPlayers = 2;
            session.gameMode = 0;
            session.matchDuration = 120f;
            session.respawnTime = 3f;
            session.isSuddenDeath = true;
            session.suddenDeathTime = 30f;
            session.vehicleSpeed = 30f;
            session.trailLength = 1;
        }

        if (playerLook != null)
        {
            session.playerPrefab = playerLook.playerPrefab;
            session.playerDisplayName = playerLook.displayName;
        }
        else
        {
            session.playerDisplayName = "Player";
        }

        int maxBots = Mathf.Max(0, session.maxPlayers - (session.playerPrefab != null ? 1 : 0));
        int assignedBots = 0;

        if (botsSettings != null && botsSettings.bots != null)
        {
            for (int i = 0; i < botsSettings.bots.Count; i++)
            {
                var source = botsSettings.bots[i];
                if (source == null || source.prefab == null || source.count <= 0)
                    continue;

                if (assignedBots >= maxBots)
                    break;

                int allowedCount = Mathf.Min(source.count, maxBots - assignedBots);
                if (allowedCount <= 0)
                    break;

                session.bots.Add(new BotSpawnEntry
                {
                    prefab = source.prefab,
                    count = allowedCount
                });
                assignedBots += allowedCount;
            }
        }

        return session;
    }
}
