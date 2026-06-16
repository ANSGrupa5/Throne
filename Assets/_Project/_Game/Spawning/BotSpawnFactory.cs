using UnityEngine;

public sealed class BotSpawnFactory
{
    private readonly LayerMask _botMapBoundaryMask;
    private readonly LayerMask _botSuddenDeathMask;
    private readonly LayerMask _botTrailMask;
    private readonly LayerMask _botPowerupMask;
    private readonly Transform _botMapCenter;

    public BotSpawnFactory(MatchInitializationContext context)
    {
        _botMapBoundaryMask = context.BotMapBoundaryMask;
        _botSuddenDeathMask = context.BotSuddenDeathMask;
        _botTrailMask = context.BotTrailMask;
        _botPowerupMask = context.BotPowerupMask;
        _botMapCenter = context.SceneReferences.BotMapCenter;
    }

    public PlayerLook CreateFallbackBotLook(GameSessionRuntime session, int botIndex)
    {
        PlayerLook look = ScriptableObject.CreateInstance<PlayerLook>();
        look.hideFlags = HideFlags.DontSave;
        look.displayName = $"BOT{botIndex + 1}";
        look.ownerId = $"bot_{botIndex + 1}";
        return look;
    }

    public void EnsureBotLooks(GameSessionRuntime session)
    {
        if (session == null)
            return;

        if (session.botLooks.Count > 0)
            return;

        int totalBots = 0;
        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry == null || entry.prefab == null || entry.count <= 0)
                continue;

            totalBots += entry.count;
        }

        if (totalBots == 0)
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

    public GameObject ResolveBotPrefab(GameSessionRuntime session, int botIndex)
    {
        if (session == null)
            return null;

        int remainingIndex = botIndex;
        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry == null || entry.count <= 0)
                continue;

            if (remainingIndex < entry.count)
                return entry.prefab != null ? entry.prefab : session.botDefaultPrefab;

            remainingIndex -= entry.count;
        }

        return session.botDefaultPrefab != null ? session.botDefaultPrefab : session.playerPrefab;
    }

    public void ConfigureBotInput(GameObject vehicle)
    {
        if (vehicle == null)
            return;

        BotVehicleInput botInput = vehicle.GetComponent<BotVehicleInput>();
        if (botInput != null)
            botInput.ConfigureRuntime(_botMapBoundaryMask, _botSuddenDeathMask, _botTrailMask, _botPowerupMask, _botMapCenter);
    }
}
