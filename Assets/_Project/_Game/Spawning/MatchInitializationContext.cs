using UnityEngine;

public sealed class MatchInitializationContext
{
    public MatchInitializationContext(
        MonoBehaviour coroutineHost,
        GameSessionRuntime session,
        MatchSceneReferences sceneReferences,
        LayerMask obstacleMask,
        float spawnInterval,
        int preMatchCountdownSeconds,
        float goDisplayDuration,
        LayerMask botMapBoundaryMask,
        LayerMask botSuddenDeathMask,
        LayerMask botTrailMask,
        LayerMask botPowerupMask)
    {
        CoroutineHost = coroutineHost;
        Session = session;
        SceneReferences = sceneReferences;
        ObstacleMask = obstacleMask;
        SpawnInterval = spawnInterval;
        PreMatchCountdownSeconds = preMatchCountdownSeconds;
        GoDisplayDuration = goDisplayDuration;
        BotMapBoundaryMask = botMapBoundaryMask;
        BotSuddenDeathMask = botSuddenDeathMask;
        BotTrailMask = botTrailMask;
        BotPowerupMask = botPowerupMask;
    }

    public MonoBehaviour CoroutineHost { get; }
    public GameSessionRuntime Session { get; }
    public MatchSceneReferences SceneReferences { get; }
    public LayerMask ObstacleMask { get; }
    public float SpawnInterval { get; }
    public int PreMatchCountdownSeconds { get; }
    public float GoDisplayDuration { get; }
    public LayerMask BotMapBoundaryMask { get; }
    public LayerMask BotSuddenDeathMask { get; }
    public LayerMask BotTrailMask { get; }
    public LayerMask BotPowerupMask { get; }
}
