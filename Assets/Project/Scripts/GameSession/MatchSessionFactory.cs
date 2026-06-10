using System.Collections.Generic;
using UnityEngine;

public sealed class MatchSessionFactory
{
    private const float FallbackRespawnTime = 5f;
    private const float FallbackSuddenDeathTime = 30f;
    private const float FallbackVehicleSpeed = 20f;
    private const float FallbackMatchDurationSeconds = 120f;
    private const int FallbackTrailLength = 1;

    public GameSessionRuntime Create(
        LobbyState lobbyState,
        MatchDefaults defaults,
        GameSettings legacyFallback,
        BotsSettings botsSettings,
        PlayerLook playerLook)
    {
        GameSessionRuntime session = new();
        LobbyMode lobbyMode = lobbyState != null ? lobbyState.LobbyMode : LobbyMode.Singleplayer;

        session.isSingleplayer = lobbyMode == LobbyMode.Singleplayer;
        // Compatibility field: currently stores the selected participant count.
        session.maxPlayers = lobbyState != null
            ? Mathf.Max(0, lobbyState.ParticipantCount)
            : ResolveFallbackParticipantCount(legacyFallback);
        session.gameMode = lobbyState != null
            ? LobbyStateGameSettingsAdapter.ToGameSettingsMatchMode(lobbyState.MatchMode)
            : ResolveFallbackGameMode(defaults, legacyFallback);
        session.matchDuration = lobbyState != null
            ? ClampMatchDuration(lobbyState.MatchDurationSeconds)
            : ResolveFallbackMatchDuration(defaults, legacyFallback);
        session.respawnTime = ResolveRespawnTime(defaults, legacyFallback);
        session.isSuddenDeath = lobbyState != null
            ? lobbyState.SuddenDeath
            : ResolveFallbackSuddenDeath(defaults, legacyFallback);
        session.suddenDeathTime = ResolveSuddenDeathTime(legacyFallback);
        session.vehicleSpeed = ResolveVehicleSpeed(defaults, legacyFallback);
        session.trailLength = lobbyState != null
            ? Mathf.Clamp(lobbyState.TrailLength, 0, 3)
            : ResolveFallbackTrailLength(defaults, legacyFallback);

        ApplyPlayerLook(session, lobbyState, playerLook);
        PopulateTrailColorPalette(session, defaults, legacyFallback);

        session.botDefaultPrefab = ResolveDefaultBotPrefab(session, botsSettings);
        PopulateBotsForCount(session, botsSettings, lobbyState != null ? lobbyState.BotCount : 0);
        PopulateBotLooks(session);

        return session;
    }

    private static void ApplyPlayerLook(GameSessionRuntime session, LobbyState lobbyState, PlayerLook playerLook)
    {
        if (playerLook != null)
        {
            session.playerPrefab = playerLook.playerPrefab;
            session.playerDisplayName = playerLook.displayName;
            session.playerOwnerId = string.IsNullOrWhiteSpace(playerLook.ownerId) ? "player_1" : playerLook.ownerId;
            session.playerTrailColor = lobbyState != null ? lobbyState.SelectedTrailColor : playerLook.trailColor;
            return;
        }

        session.playerDisplayName = "Player";
        session.playerOwnerId = "player_1";
        session.playerTrailColor = lobbyState != null ? lobbyState.SelectedTrailColor : Color.white;
    }

    private static void PopulateTrailColorPalette(
        GameSessionRuntime session,
        MatchDefaults defaults,
        GameSettings legacyFallback)
    {
        if (defaults != null && defaults.TrailColorPalette != null && defaults.TrailColorPalette.Count > 0)
        {
            IReadOnlyList<Color> colors = defaults.TrailColorPalette.Colors;
            for (int i = 0; i < colors.Count; i++)
                session.trailColorPalette.Add(colors[i]);
            return;
        }

        if (legacyFallback != null && legacyFallback.trailColorPalette != null)
            session.trailColorPalette.AddRange(legacyFallback.trailColorPalette);
    }

    private static void PopulateBotsForCount(
        GameSessionRuntime session,
        BotsSettings botsSettings,
        int desiredBotCount)
    {
        int botCount = Mathf.Max(0, desiredBotCount);
        if (botCount <= 0)
            return;

        List<GameObject> pattern = new();
        if (botsSettings != null && botsSettings.bots != null)
        {
            for (int i = 0; i < botsSettings.bots.Count; i++)
            {
                BotsSettings.BotEntry source = botsSettings.bots[i];
                if (source == null || source.prefab == null || source.count <= 0)
                    continue;

                int repeats = Mathf.Max(1, source.count);
                for (int repeat = 0; repeat < repeats; repeat++)
                    pattern.Add(source.prefab);
            }
        }

        if (pattern.Count == 0 && session.botDefaultPrefab != null)
            pattern.Add(session.botDefaultPrefab);

        if (pattern.Count == 0)
            return;

        for (int i = 0; i < botCount; i++)
            AddBotEntry(session.bots, pattern[i % pattern.Count]);
    }

    private static void AddBotEntry(List<GameSessionRuntime.BotSpawnEntry> target, GameObject prefab)
    {
        if (target == null || prefab == null)
            return;

        if (target.Count > 0)
        {
            GameSessionRuntime.BotSpawnEntry last = target[target.Count - 1];
            if (last != null && last.prefab == prefab)
            {
                last.count++;
                return;
            }
        }

        target.Add(new GameSessionRuntime.BotSpawnEntry
        {
            prefab = prefab,
            count = 1
        });
    }

    private static void PopulateBotLooks(GameSessionRuntime session)
    {
        int totalBots = 0;
        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry != null)
                totalBots += Mathf.Max(0, entry.count);
        }

        if (totalBots <= 0)
            return;

        List<Color> availableColors = new();
        for (int i = 0; i < session.trailColorPalette.Count; i++)
        {
            Color color = session.trailColorPalette[i];
            if (color != session.playerTrailColor)
                availableColors.Add(color);
        }

        for (int i = 0; i < totalBots; i++)
        {
            PlayerLook look = ScriptableObject.CreateInstance<PlayerLook>();
            look.hideFlags = HideFlags.DontSave;
            look.playerPrefab = session.botDefaultPrefab;
            look.displayName = $"BOT{i + 1}";
            look.ownerId = $"bot_{i + 1}";
            look.trailColor = PickBotColorForLook(availableColors, session);
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
                BotsSettings.BotEntry source = botsSettings.bots[i];
                if (source != null && source.prefab != null)
                    return source.prefab;
            }
        }

        return session.playerPrefab;
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

        if (session.trailColorPalette.Count > 0)
            return session.trailColorPalette[Random.Range(0, session.trailColorPalette.Count)];

        return Color.white;
    }

    private static int ResolveFallbackParticipantCount(GameSettings legacyFallback)
    {
        return legacyFallback != null ? Mathf.Max(0, legacyFallback.maxPlayers) : 2;
    }

    private static int ResolveFallbackGameMode(MatchDefaults defaults, GameSettings legacyFallback)
    {
        if (defaults != null)
            return LobbyStateGameSettingsAdapter.ToGameSettingsMatchMode(defaults.DefaultMatchMode);

        return legacyFallback != null ? legacyFallback.gameMode : 0;
    }

    private static float ResolveFallbackMatchDuration(MatchDefaults defaults, GameSettings legacyFallback)
    {
        if (defaults != null)
            return ClampMatchDuration(defaults.DefaultMatchDurationSeconds);

        return legacyFallback != null
            ? ClampMatchDuration(legacyFallback.matchDuration)
            : FallbackMatchDurationSeconds;
    }

    private static bool ResolveFallbackSuddenDeath(MatchDefaults defaults, GameSettings legacyFallback)
    {
        if (defaults != null)
            return defaults.DefaultSuddenDeath;

        return legacyFallback != null && legacyFallback.isSuddenDeath;
    }

    private static int ResolveFallbackTrailLength(MatchDefaults defaults, GameSettings legacyFallback)
    {
        if (defaults != null)
            return Mathf.Clamp(defaults.DefaultTrailLength, 0, 3);

        return legacyFallback != null
            ? Mathf.Clamp(legacyFallback.trailLength, legacyFallback.GetMinTrailLength(), legacyFallback.GetMaxTrailLength())
            : FallbackTrailLength;
    }

    private static float ResolveRespawnTime(MatchDefaults defaults, GameSettings legacyFallback)
    {
        if (defaults != null)
            return Mathf.Max(0f, defaults.DefaultRespawnTime);

        return legacyFallback != null ? Mathf.Max(0f, legacyFallback.respawnTime) : FallbackRespawnTime;
    }

    private static float ResolveSuddenDeathTime(GameSettings legacyFallback)
    {
        return legacyFallback != null ? Mathf.Max(0f, legacyFallback.suddenDeathTime) : FallbackSuddenDeathTime;
    }

    private static float ResolveVehicleSpeed(MatchDefaults defaults, GameSettings legacyFallback)
    {
        if (defaults != null)
            return Mathf.Max(0f, defaults.DefaultVehicleSpeed);

        return legacyFallback != null ? Mathf.Max(0f, legacyFallback.vehicleSpeed) : FallbackVehicleSpeed;
    }

    private static float ClampMatchDuration(float duration)
    {
        return Mathf.Clamp(duration, GameSettings.MinMatchDuration, GameSettings.MaxMatchDuration);
    }
}
