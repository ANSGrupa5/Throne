public static class LobbyLaunchContext
{
    public const string SharedLobbySceneName = "LobbyScene";

    private static bool _hasRequestedMode;
    private static LobbyMode _requestedMode = LobbyMode.Singleplayer;

    public static void RequestSingleplayer()
    {
        _requestedMode = LobbyMode.Singleplayer;
        _hasRequestedMode = true;
    }

    public static void RequestMultiplayer()
    {
        _requestedMode = LobbyMode.MultiplayerClient;
        _hasRequestedMode = true;
    }

    public static LobbyMode ResolveMode(LobbyMode fallbackMode)
    {
        return _hasRequestedMode ? _requestedMode : fallbackMode;
    }

    public static LobbyMode ConsumeMode(LobbyMode fallbackMode)
    {
        if (!_hasRequestedMode)
            return fallbackMode;

        LobbyMode mode = _requestedMode;
        _requestedMode = LobbyMode.Singleplayer;
        _hasRequestedMode = false;
        return mode;
    }

    public static bool IsSharedLobbySceneName(string sceneName)
    {
        return string.Equals(sceneName?.Trim(), SharedLobbySceneName, System.StringComparison.OrdinalIgnoreCase);
    }
}
