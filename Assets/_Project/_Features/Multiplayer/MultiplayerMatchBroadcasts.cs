using FishNet;
using FishNet.Broadcast;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

public static class MultiplayerMatchBroadcasts
{
    public struct FrozenState : IBroadcast
    {
        public bool Frozen;
    }

    public struct CountdownCount : IBroadcast
    {
        public int Value;
    }

    public struct CountdownGo : IBroadcast
    {
    }

    public struct CountdownHide : IBroadcast
    {
    }

    public struct TimerStarted : IBroadcast
    {
        public float Duration;
        public double ServerRealtime;
    }

    public struct EndGameResultSnapshot : IBroadcast
    {
        public string OwnerId;
        public string DisplayName;
        public int Kills;
        public int Deaths;
        public Color TrailColor;
    }

    public struct EndGamePayload : IBroadcast
    {
        public string Reason;
        public EndGameResultSnapshot[] Results;
    }

    private static bool _clientHandlersRegistered;

    public static void RegisterClientHandlers(NetworkManager networkManager)
    {
        if (networkManager == null || _clientHandlersRegistered)
            return;

        if (networkManager.ClientManager == null)
        {
            Debug.LogWarning("[MultiplayerMatchBroadcasts] Cannot register client broadcast handlers because ClientManager is null.");
            return;
        }

        networkManager.ClientManager.RegisterBroadcast<FrozenState>(OnFrozenState);
        networkManager.ClientManager.RegisterBroadcast<CountdownCount>(OnCountdownCount);
        networkManager.ClientManager.RegisterBroadcast<CountdownGo>(OnCountdownGo);
        networkManager.ClientManager.RegisterBroadcast<CountdownHide>(OnCountdownHide);
        networkManager.ClientManager.RegisterBroadcast<TimerStarted>(OnTimerStarted);
        networkManager.ClientManager.RegisterBroadcast<EndGamePayload>(OnEndGamePayload);

        _clientHandlersRegistered = true;
        Debug.Log("[MultiplayerMatchBroadcasts] Client broadcast handlers registered.");
    }

    public static void ResetClientRegistration()
    {
        _clientHandlersRegistered = false;
    }

    public static void SendFrozen(bool frozen)
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        MultiplayerMatchState.SetFrozen(frozen);
        InstanceFinder.ServerManager.Broadcast(new FrozenState { Frozen = frozen });
        Debug.Log($"[MultiplayerMatchBroadcasts] Broadcast FrozenState({frozen}).");
    }

    public static void SendCountdownCount(int value)
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        MultiplayerMatchState.SetCountdownCount(value);
        InstanceFinder.ServerManager.Broadcast(new CountdownCount { Value = value });
        Debug.Log($"[MultiplayerMatchBroadcasts] Broadcast CountdownCount({value}).");
    }

    public static void SendCountdownGo()
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        MultiplayerMatchState.SetCountdownGo();
        InstanceFinder.ServerManager.Broadcast(new CountdownGo());
        Debug.Log("[MultiplayerMatchBroadcasts] Broadcast CountdownGo.");
    }

    public static void SendCountdownHide()
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        MultiplayerMatchState.HideCountdown();
        InstanceFinder.ServerManager.Broadcast(new CountdownHide());
        Debug.Log("[MultiplayerMatchBroadcasts] Broadcast CountdownHide.");
    }

    public static void SendTimerStarted(float duration)
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        MultiplayerMatchState.BeginTimer(duration);
        InstanceFinder.ServerManager.Broadcast(new TimerStarted
        {
            Duration = duration,
            ServerRealtime = Time.realtimeSinceStartupAsDouble
        });

        Debug.Log($"[MultiplayerMatchBroadcasts] Broadcast TimerStarted({duration}).");
    }

    public static void SendEndGamePayload(string reason, EndGameResultSnapshot[] results)
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        ApplyEndGamePayload(reason, results);

        InstanceFinder.ServerManager.Broadcast(new EndGamePayload
        {
            Reason = reason,
            Results = results
        });

        Debug.Log($"[MultiplayerEndGame] Broadcast EndGamePayload reason={reason}, results={(results != null ? results.Length : 0)}.");
    }

    private static void OnFrozenState(FrozenState message, Channel channel)
    {
        MultiplayerMatchState.SetFrozen(message.Frozen);
        Debug.Log($"[MultiplayerMatchBroadcasts] Received FrozenState({message.Frozen}).");
    }

    private static void OnCountdownCount(CountdownCount message, Channel channel)
    {
        MultiplayerMatchState.SetCountdownCount(message.Value);
        Debug.Log($"[MultiplayerMatchBroadcasts] Received CountdownCount({message.Value}).");
        MultiplayerHudBridge.ApplyCountdownNow("Broadcast.CountdownCount");
    }

    private static void OnCountdownGo(CountdownGo message, Channel channel)
    {
        MultiplayerMatchState.SetCountdownGo();
        Debug.Log("[MultiplayerMatchBroadcasts] Received CountdownGo.");
        MultiplayerHudBridge.ApplyCountdownNow("Broadcast.CountdownGo");
    }

    private static void OnCountdownHide(CountdownHide message, Channel channel)
    {
        MultiplayerMatchState.HideCountdown();
        Debug.Log("[MultiplayerMatchBroadcasts] Received CountdownHide.");
        MultiplayerHudBridge.ApplyCountdownNow("Broadcast.CountdownHide");
    }

    private static void OnTimerStarted(TimerStarted message, Channel channel)
    {
        MultiplayerMatchState.BeginTimer(message.Duration);
        Debug.Log($"[MultiplayerMatchBroadcasts] Received TimerStarted({message.Duration}).");
        MultiplayerHudBridge.ApplyTimerNow("Broadcast.TimerStarted");
    }

    private static void OnEndGamePayload(EndGamePayload message, Channel channel)
    {
        ApplyEndGamePayload(message.Reason, message.Results);
        Debug.Log($"[MultiplayerEndGame] Received EndGamePayload reason={message.Reason}, results={(message.Results != null ? message.Results.Length : 0)}.");
    }

    private static void ApplyEndGamePayload(string reason, EndGameResultSnapshot[] results)
    {
        GameOverPayload.Clear();
        GameOverPayload.reason = GameOverPayload.ParseReason(reason);

        if (results == null)
            return;

        for (int i = 0; i < results.Length; i++)
        {
            EndGameResultSnapshot result = results[i];
            GameOverPayload.results.Add(new GameOverPayload.MatchResult
            {
                ownerId = result.OwnerId,
                displayName = result.DisplayName,
                kills = result.Kills,
                deaths = result.Deaths,
                trailColor = result.TrailColor
            });
        }
    }
}
