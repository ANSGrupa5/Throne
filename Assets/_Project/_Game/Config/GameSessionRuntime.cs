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
    public Color playerTrailColor;

    public readonly List<BotSpawnEntry> bots = new List<BotSpawnEntry>();
    public readonly List<Color> trailColorPalette = new List<Color>();

    public static GameSessionRuntime FromDefaults(GameSettings gameSettings, BotsSettings botsSettings, PlayerLook playerLook, int? desiredBotCount = null)
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
            session.playerTrailColor = playerLook.trailColor;
        }
        else
        {
            session.playerDisplayName = "Player";
            session.playerTrailColor = Color.white;
        }

        if (gameSettings != null && gameSettings.trailColorPalette != null)
            session.trailColorPalette.AddRange(gameSettings.trailColorPalette);

        int maxBots = Mathf.Max(0, session.maxPlayers - (session.playerPrefab != null ? 1 : 0));

        if (desiredBotCount.HasValue)
        {
            PopulateBotsForCount(session, botsSettings, maxBots, desiredBotCount.Value);
        }
        else if (botsSettings != null && botsSettings.bots != null)
        {
            int assignedBots = 0;

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

    private static void PopulateBotsForCount(GameSessionRuntime session, BotsSettings botsSettings, int maxBots, int desiredBotCount)
    {
        if (session == null || botsSettings == null || botsSettings.bots == null)
            return;

        int botCount = Mathf.Clamp(desiredBotCount, 0, maxBots);
        if (botCount <= 0)
            return;

        List<GameObject> pattern = new List<GameObject>();

        for (int i = 0; i < botsSettings.bots.Count; i++)
        {
            var source = botsSettings.bots[i];
            if (source == null || source.prefab == null || source.count <= 0)
                continue;

            int repeats = Mathf.Max(1, source.count);
            for (int repeat = 0; repeat < repeats; repeat++)
            {
                pattern.Add(source.prefab);
            }
        }

        if (pattern.Count == 0)
            return;

        for (int i = 0; i < botCount; i++)
        {
            AddBotEntry(session.bots, pattern[i % pattern.Count]);
        }
    }

    private static void AddBotEntry(List<BotSpawnEntry> target, GameObject prefab)
    {
        if (target == null || prefab == null)
            return;

        if (target.Count > 0)
        {
            BotSpawnEntry last = target[target.Count - 1];
            if (last != null && last.prefab == prefab)
            {
                last.count++;
                return;
            }
        }

        target.Add(new BotSpawnEntry
        {
            prefab = prefab,
            count = 1
        });
    }
}
