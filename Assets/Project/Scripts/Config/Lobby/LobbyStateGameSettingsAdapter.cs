using UnityEngine;

public static class LobbyStateGameSettingsAdapter
{
    private const string DefaultArenaSceneName = "Neon City XL";
    private const float FallbackMatchDurationSeconds = 120f;
    private const int FallbackTrailLength = 1;

    public static LobbyState CreateLobbyStateFromGameSettings(GameSettings gameSettings, LobbyMode mode)
    {
        LobbyState state = new()
        {
            LobbyMode = mode,
            ArenaSceneName = DefaultArenaSceneName,
            HumanPlayerCount = mode == LobbyMode.Singleplayer ? 1 : 0,
            BotCount = 0,
            MatchMode = MatchMode.KingOfTheHill,
            MatchDurationSeconds = FallbackMatchDurationSeconds,
            SuddenDeath = false,
            TrailLength = FallbackTrailLength,
            SelectedTrailColorIndex = 0,
            SelectedTrailColor = Color.white,
            SelectedPlayerModelIndex = 0,
            IsDirty = false
        };

        if (gameSettings == null)
            return state;

        state.ArenaSceneName = string.IsNullOrWhiteSpace(gameSettings.arenaSceneName)
            ? DefaultArenaSceneName
            : gameSettings.arenaSceneName;
        state.MatchMode = ToMatchMode(gameSettings.gameMode);
        state.MatchDurationSeconds = Mathf.Clamp(
            gameSettings.matchDuration,
            GameSettings.MinMatchDuration,
            GameSettings.MaxMatchDuration);
        state.SuddenDeath = gameSettings.isSuddenDeath;
        state.TrailLength = Mathf.Clamp(
            gameSettings.trailLength,
            gameSettings.GetMinTrailLength(),
            gameSettings.GetMaxTrailLength());

        return state;
    }

    public static void CopyLobbyStateToGameSettings(LobbyState state, GameSettings gameSettings)
    {
        if (state == null || gameSettings == null)
            return;

        gameSettings.IsSingleplayer = state.LobbyMode == LobbyMode.Singleplayer;
        gameSettings.maxPlayers = Mathf.Max(0, state.ParticipantCount);
        gameSettings.arenaSceneName = string.IsNullOrWhiteSpace(state.ArenaSceneName)
            ? DefaultArenaSceneName
            : state.ArenaSceneName;
        gameSettings.gameMode = ToGameSettingsMatchMode(state.MatchMode);
        gameSettings.matchDuration = Mathf.Clamp(
            state.MatchDurationSeconds,
            GameSettings.MinMatchDuration,
            GameSettings.MaxMatchDuration);
        gameSettings.isSuddenDeath = state.SuddenDeath;
        gameSettings.trailLength = Mathf.Clamp(
            state.TrailLength,
            gameSettings.GetMinTrailLength(),
            gameSettings.GetMaxTrailLength());
    }

    public static MatchMode ToMatchMode(int value)
    {
        return value switch
        {
            (int)MatchMode.Deathmatch => MatchMode.Deathmatch,
            _ => MatchMode.KingOfTheHill
        };
    }

    public static int ToGameSettingsMatchMode(MatchMode mode)
    {
        return mode switch
        {
            MatchMode.Deathmatch => (int)MatchMode.Deathmatch,
            _ => (int)MatchMode.KingOfTheHill
        };
    }
}
