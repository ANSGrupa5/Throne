using System.Collections.Generic;
using UnityEngine;

// Runtime match state assembled from game settings, player selection, and bot configuration.
public class GameSessionRuntime
{
    [System.Serializable]
    // Describes a prefab and the number of times it should be spawned.
    public class BotSpawnEntry
    {
        public GameObject prefab;
        public int count;
    }

    [System.Serializable]
    // Stores the match stats for one participant.
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
    public string arenaSceneName;
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

    // Builds a runtime session from the provided assets and falls back to safe defaults when needed.
    public static GameSessionRuntime FromDefaults(
        GameSettings gameSettings,
        BotsSettings botsSettings,
        PlayerLook playerLook,
        int? desiredBotCount = null,
        int? overrideGameMode = null,
        string overrideArenaSceneName = null,
        float? overrideMatchDuration = null,
        bool? overrideSuddenDeath = null,
        int? overrideTrailLength = null,
        Color? overridePlayerTrailColor = null,
        bool allowZeroBots = false)
    {
        var session = new GameSessionRuntime();

        if (gameSettings != null)
        {
            session.isSingleplayer = gameSettings.IsSingleplayer;
            session.maxPlayers = gameSettings.maxPlayers;
            session.gameMode = gameSettings.gameMode;
            session.arenaSceneName = gameSettings.arenaSceneName;
            session.matchDuration = gameSettings.matchDuration;
            session.respawnTime = gameSettings.respawnTime;
            session.isSuddenDeath = gameSettings.isSuddenDeath;
            session.suddenDeathTime = gameSettings.suddenDeathTime;
            session.vehicleSpeed = gameSettings.vehicleSpeed;
            session.trailLength = gameSettings.trailLength;
        }
        else
        {
            // Use a minimal fallback configuration when no game settings asset is available.
            session.isSingleplayer = true;
            session.maxPlayers = 2;
            session.gameMode = 0;
            session.arenaSceneName = "Neon City XL";
            session.matchDuration = 120f;
            session.respawnTime = 3f;
            session.isSuddenDeath = true;
            session.suddenDeathTime = 30f;
            session.vehicleSpeed = 30f;
            session.trailLength = 1;
        }

        // Seed the local player from the selected look, or use a generic default identity.
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

        if (overrideGameMode.HasValue)
            session.gameMode = overrideGameMode.Value;
        if (!string.IsNullOrWhiteSpace(overrideArenaSceneName))
            session.arenaSceneName = overrideArenaSceneName.Trim();
        if (overrideMatchDuration.HasValue)
            session.matchDuration = overrideMatchDuration.Value;
        if (overrideSuddenDeath.HasValue)
            session.isSuddenDeath = overrideSuddenDeath.Value;
        if (overrideTrailLength.HasValue)
            session.trailLength = overrideTrailLength.Value;
        if (overridePlayerTrailColor.HasValue)
            session.playerTrailColor = overridePlayerTrailColor.Value;

        // Preserve the configured palette so bot colors can be picked from the same set.
        if (gameSettings != null && gameSettings.trailColorPalette != null)
            session.trailColorPalette.AddRange(gameSettings.trailColorPalette);

        session.botDefaultPrefab = ResolveDefaultBotPrefab(session, botsSettings);

        if (desiredBotCount.HasValue)
        {
            // Force an exact bot count when the caller requests one.
            int requestedBots = desiredBotCount.Value;
            if (requestedBots < 0)
                requestedBots = 0;
            if (requestedBots == 0 && !allowZeroBots)
                requestedBots = 1;
            session.maxPlayers = (requestedBots>5 ? 5 : requestedBots) + 1;
            PopulateBotsForCount(session, botsSettings, requestedBots, requestedBots);
        }
        else if (botsSettings != null && botsSettings.bots != null)
        {
            // Otherwise, clamp the configured bot list to the current player limit.
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

    // Expands the configured bot prefabs into a concrete spawn pattern for a fixed bot count.
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

    // Merges consecutive identical prefabs so the runtime bot list stays compact.
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

    // Builds transient PlayerLook instances so the match can consume bot appearance data directly.
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

    // Chooses a default bot prefab from settings first, then from the configured bot list, then from the player prefab.
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

    // Picks a bot color while trying to avoid reusing the player's trail color.
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

    // Returns an existing stats entry for this owner, or creates one if the match has not seen it yet.
    public PlayerMatchStats GetOrCreateStats(string ownerId, string displayName, Color? trailColor = null)
    {
        string normalizedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
        string normalizedDisplayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();

        for (int i = 0; i < playerStats.Count; i++)
        {
            PlayerMatchStats stats = playerStats[i];
            if (stats == null)
        // Returns an existing stats entry for this owner, or creates one if the match has not seen it yet.
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
