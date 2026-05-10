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

    [System.Serializable]
    public class PlayerMatchStats
    {
        public string ownerId;
        public string displayName;
        public int kills;
        public int deaths;
        public Color trailColor = Color.white;
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
    public string playerOwnerId;
    public Color playerTrailColor;
    public GameObject botDefaultPrefab;

    public readonly List<PlayerLook> botLooks = new List<PlayerLook>();
    public readonly List<BotSpawnEntry> bots = new List<BotSpawnEntry>();
    public readonly List<Color> trailColorPalette = new List<Color>();
    public readonly List<PlayerMatchStats> playerStats = new List<PlayerMatchStats>();

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
            session.playerOwnerId = string.IsNullOrWhiteSpace(playerLook.ownerId) ? "player_1" : playerLook.ownerId;
            session.playerTrailColor = playerLook.trailColor;
        }
        else
        {
            session.playerDisplayName = "Player";
            session.playerOwnerId = "player_1";
            session.playerTrailColor = Color.white;
        }

        if (gameSettings != null && gameSettings.trailColorPalette != null)
            session.trailColorPalette.AddRange(gameSettings.trailColorPalette);

        session.botDefaultPrefab = ResolveDefaultBotPrefab(session, botsSettings);

        if (desiredBotCount.HasValue)
        {
            int requestedBots = desiredBotCount.Value;
            session.maxPlayers = requestedBots + (session.playerPrefab != null ? 1 : 0);
            PopulateBotsForCount(session, botsSettings, requestedBots, requestedBots);
        }
        else if (botsSettings != null && botsSettings.bots != null)
        {
            int maxBots = session.maxPlayers - (session.playerPrefab != null ? 1 : 0);
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

        PopulateBotLooks(session, botsSettings);

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

    private static void PopulateBotLooks(GameSessionRuntime session, BotsSettings botsSettings)
    {
        if (session == null)
            return;

        session.botLooks.Clear();

        int totalBots = 0;
        for (int i = 0; i < session.bots.Count; i++)
        {
            BotSpawnEntry entry = session.bots[i];
            if (entry != null)
                totalBots += Mathf.Max(0, entry.count);
        }

        if (totalBots <= 0)
            return;

        GameObject defaultBotPrefab = session != null ? session.botDefaultPrefab : ResolveDefaultBotPrefab(session, botsSettings);
        List<Color> availableColors = new List<Color>();
        for (int i = 0; i < session.trailColorPalette.Count; i++)
        {
            Color color = session.trailColorPalette[i];
            if (color == session.playerTrailColor)
                continue;

            availableColors.Add(color);
        }

        for (int i = 0; i < totalBots; i++)
        {
            Color color = PickBotColorForLook(availableColors, session);
            PlayerLook look = ScriptableObject.CreateInstance<PlayerLook>();
            look.hideFlags = HideFlags.DontSave;
            look.playerPrefab = defaultBotPrefab;
            look.displayName = $"BOT{i + 1}";
            look.ownerId = $"bot_{i + 1}";
            look.trailColor = color;
            session.botLooks.Add(look);
        }
    }

    private static GameObject ResolveDefaultBotPrefab(GameSessionRuntime session, BotsSettings botsSettings)
    {
        if (botsSettings != null && botsSettings.defaultBotPrefab != null)
            return botsSettings.defaultBotPrefab;

        if (botsSettings != null && botsSettings.bots != null)
        {
            for (int i = 0; i < botsSettings.bots.Count; i++)
            {
                var source = botsSettings.bots[i];
                if (source != null && source.prefab != null)
                    return source.prefab;
            }
        }

        if (session != null && session.playerPrefab != null)
            return session.playerPrefab;

        return null;
    }

    private static Color PickBotColorForLook(List<Color> availableColors, GameSessionRuntime session)
    {
        if (availableColors != null && availableColors.Count > 0)
        {
            int index = Random.Range(0, availableColors.Count);
            Color selected = availableColors[index];
            availableColors.RemoveAt(index);
            return selected;
        }

        if (session != null && session.trailColorPalette.Count > 0)
        {
            int index = Random.Range(0, session.trailColorPalette.Count);
            return session.trailColorPalette[index];
        }

        return Color.white;
    }

    public PlayerMatchStats GetOrCreateStats(string ownerId, string displayName, Color? trailColor = null)
    {
        string normalizedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
        string normalizedDisplayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();

        for (int i = 0; i < playerStats.Count; i++)
        {
            PlayerMatchStats stats = playerStats[i];
            if (stats == null)
                continue;

            if (!string.IsNullOrWhiteSpace(normalizedOwnerId) &&
                string.Equals(stats.ownerId, normalizedOwnerId, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(normalizedDisplayName))
                    stats.displayName = normalizedDisplayName;

                if (trailColor.HasValue)
                    stats.trailColor = trailColor.Value;

                return stats;
            }
        }

        PlayerMatchStats created = new PlayerMatchStats
        {
            ownerId = normalizedOwnerId,
            displayName = normalizedDisplayName,
            kills = 0,
            deaths = 0,
            trailColor = trailColor ?? Color.white
        };

        playerStats.Add(created);
        return created;
    }
}
