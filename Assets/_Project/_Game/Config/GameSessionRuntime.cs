using System.Collections.Generic;
using UnityEngine;

// Runtime match state assembled from game settings, player selection, and bot configuration.
public class GameSessionRuntime
{
    public const int KingOfTheHillMode = 0;
    public const int DeathmatchMode = 1;

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
        bool isSingleplayer,
        Color selectedPlayerTrailColor)
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
            playerTrailColor = TrailColorPalette.SanitizeColor(selectedPlayerTrailColor, Color.white),
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

        PopulateBotLooks(session);
        return session;
    }

    private static int ToLegacyGameMode(MatchMode matchMode)
    {
        return matchMode == MatchMode.KingOfTheHill ? KingOfTheHillMode : DeathmatchMode;
    }

    // Builds transient PlayerLook instances so the match can consume bot appearance data directly.
    private static void PopulateBotLooks(GameSessionRuntime session)
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

    // Returns an existing stats entry for this owner, or creates one if the match has not seen it yet.
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
