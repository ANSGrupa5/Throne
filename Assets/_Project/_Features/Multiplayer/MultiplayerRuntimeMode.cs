using FishNet;

public static class MultiplayerRuntimeMode
{
    public static bool IsFishNetServerStarted =>
        InstanceFinder.IsServerStarted;

    public static bool IsFishNetClientStarted =>
        InstanceFinder.IsClientStarted;

    public static bool IsFishNetActive =>
        IsFishNetServerStarted || IsFishNetClientStarted;

    public static bool IsClientOnly =>
        IsFishNetClientStarted && !IsFishNetServerStarted;

    public static bool IsServerOrSingleplayerAuthority =>
        !IsFishNetActive || IsFishNetServerStarted;

    public static bool HasAuthoritativeSession =>
        GameSessionBootstrap.CurrentSession != null;

    public static bool HasMultiplayerSessionOnThisPeer
    {
        get
        {
            GameSessionRuntime session = GameSessionBootstrap.CurrentSession;
            return session != null && !session.isSingleplayer;
        }
    }
}
