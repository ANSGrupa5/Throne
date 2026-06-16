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

    public static GameSessionRuntime FromSettings(
        MatchSettings settings,
        VehiclePrefabSet vehiclePrefabSet,
        TrailColorPalette trailColorPalette,
        bool isSingleplayer)
    {
        if (settings == null)
        {
            Debug.LogWarning("[GameSessionRuntime] Cannot create session from null MatchSettings. Using a new settings instance.");
            settings = new MatchSettings();
        }

        var session = new GameSessionRuntime
        {
            isSingleplayer = isSingleplayer,
            maxPlayers = Mathf.Max(1, Mathf.Max(0, settings.PlayerCount) + Mathf.Max(0, settings.BotCount)),
            gameMode = ToLegacyGameMode(settings.MatchMode),
            arenaSceneName = settings.ArenaSceneName,
            matchDuration = settings.MatchDurationSeconds,
            respawnTime = settings.RespawnTimeSeconds,
            isSuddenDeath = settings.SuddenDeathEnabled,
            suddenDeathTime = settings.SuddenDeathTimeSeconds,
            vehicleSpeed = 30f,
            trailLength = settings.TrailLength,
            playerPrefab = vehiclePrefabSet != null ? vehiclePrefabSet.PlayerVehiclePrefab : null,
            playerDisplayName = "Player",
            playerOwnerId = "player_1",
            playerTrailColor = TrailColorPalette.SanitizeColor(settings.PlayerTrailColor, Color.white),
            botDefaultPrefab = vehiclePrefabSet != null ? vehiclePrefabSet.BotVehiclePrefab : null
        };

        if (trailColorPalette != null && trailColorPalette.Colors != null)
        {
            for (int i = 0; i < trailColorPalette.Colors.Count; i++)
                session.trailColorPalette.Add(TrailColorPalette.SanitizeColor(trailColorPalette.Colors[i], Color.white));
        }

        if (session.trailColorPalette.Count == 0)
            session.trailColorPalette.Add(session.playerTrailColor);

        if (settings.BotCount > 0)
        {
            session.bots.Add(new BotSpawnEntry
            {
                prefab = session.botDefaultPrefab,
                count = settings.BotCount
            });
        }

        PopulateBotLooks(session, null);
        return session;
    }

    private static int ToLegacyGameMode(MatchMode matchMode)
    {
        return matchMode == MatchMode.KingOfTheHill ? 0 : 1;
    }

    // Builds a transitional runtime session from legacy assets and falls back to safe defaults when needed.
    [System.Obsolete("Use FromSettings(MatchSettings, VehiclePrefabSet, TrailColorPalette, bool) for new match/session setup.")]
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
            session.playerDisplayName = playerLook.displayName;
            session.playerOwnerId = string.IsNullOrWhiteSpace(playerLook.ownerId) ? "player_1" : playerLook.ownerId;
        }
        else
        {
            session.playerDisplayName = "Player";
            session.playerOwnerId = "player_1";
        }

        session.playerTrailColor = overridePlayerTrailColor ?? Color.white;

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

        for (int i = 0; i < totalBots; i++)
        {
            PlayerLook look = ScriptableObject.CreateInstance<PlayerLook>();
            look.hideFlags = HideFlags.DontSave;
            look.displayName = $"BOT{i + 1}";
            look.ownerId = $"bot_{i + 1}";
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

                if (trailColor.HasValue && !IsValidStoredColor(stats.trailColor))
                    stats.trailColor = TrailColorPalette.SanitizeColor(trailColor.Value, Color.white);

                return stats;
            }
        }

        PlayerMatchStats created = new PlayerMatchStats
        {
            ownerId = normalizedOwnerId,
            displayName = normalizedDisplayName,
            kills = 0,
            deaths = 0,
            trailColor = TrailColorPalette.SanitizeColor(trailColor ?? Color.white, Color.white)
        };

        playerStats.Add(created);
        return created;
    }

    public Color GetBotTrailColor(int botIndex)
    {
        Color fallback = CreateFallbackBotColor(botIndex, playerTrailColor);
        if (trailColorPalette == null || trailColorPalette.Count == 0)
            return fallback;

        List<Color> candidates = new List<Color>();
        for (int i = 0; i < trailColorPalette.Count; i++)
        {
            Color candidate = TrailColorPalette.SanitizeColor(trailColorPalette[i], fallback);
            if (!ApproximatelySameColor(candidate, playerTrailColor))
                candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            return fallback;

        int index = botIndex % candidates.Count;
        if (index < 0)
            index += candidates.Count;

        return candidates[index];
    }

    private static Color CreateFallbackBotColor(int botIndex, Color playerColor)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Color color = Color.HSVToRGB(((botIndex + attempt + 1) * 0.17f) % 1f, 0.85f, 1f);
            color.a = 1f;
            if (!ApproximatelySameColor(color, playerColor))
                return color;
        }

        return Color.white;
    }

    private static bool ApproximatelySameColor(Color a, Color b)
    {
        a = TrailColorPalette.SanitizeColor(a, Color.white);
        b = TrailColorPalette.SanitizeColor(b, Color.white);

        return Mathf.Abs(a.r - b.r) <= 0.01f &&
               Mathf.Abs(a.g - b.g) <= 0.01f &&
               Mathf.Abs(a.b - b.b) <= 0.01f;
    }

    private static bool IsValidStoredColor(Color color)
    {
        return color.a > 0.01f;
    }
}
