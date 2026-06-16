using System.Collections;
using FishNet;
using UnityEngine;

public static class MatchStartSequence
{
    public static IEnumerator RunSingleplayer(MatchInitializationContext context)
    {
        yield return CountdownAndStart(context);

        if (context.SceneReferences.GameTimer != null)
            context.SceneReferences.GameTimer.Begin(context.Session.matchDuration);
    }

    public static IEnumerator RunMultiplayer(MatchInitializationContext context)
    {
        if (!InstanceFinder.IsServerStarted)
            yield break;

        MultiplayerMatchBroadcasts.SendFrozen(true);

        for (int i = context.PreMatchCountdownSeconds; i > 0; i--)
        {
            MultiplayerMatchBroadcasts.SendCountdownCount(i);

            if (context.SceneReferences.GameStartTimer != null)
                context.SceneReferences.GameStartTimer.ShowCount(i);

            yield return new WaitForSecondsRealtime(1f);
        }

        MultiplayerMatchBroadcasts.SendCountdownGo();

        if (context.SceneReferences.GameStartTimer != null)
            context.SceneReferences.GameStartTimer.ShowGo();

        yield return new WaitForSecondsRealtime(context.GoDisplayDuration);

        MultiplayerMatchBroadcasts.SendCountdownHide();

        if (context.SceneReferences.GameStartTimer != null)
            context.SceneReferences.GameStartTimer.Hide();

        MultiplayerMatchBroadcasts.SendTimerStarted(context.Session.matchDuration);

        if (context.SceneReferences.GameTimer != null)
            context.SceneReferences.GameTimer.Begin(context.Session.matchDuration);

        MultiplayerMatchBroadcasts.SendFrozen(false);
    }

    private static IEnumerator CountdownAndStart(MatchInitializationContext context)
    {
        MultiplayerMatchState.SetCountdownActive(true);
        MultiplayerMatchState.SetCountdownValue(context.PreMatchCountdownSeconds);

        for (int i = context.PreMatchCountdownSeconds; i > 0; i--)
        {
            MultiplayerMatchState.SetCountdownValue(i);
            if (context.SceneReferences.GameStartTimer != null)
                context.SceneReferences.GameStartTimer.ShowCount(i);
            yield return new WaitForSecondsRealtime(1f);
        }

        MultiplayerMatchState.SetCountdownValue(0);
        if (context.SceneReferences.GameStartTimer != null)
            context.SceneReferences.GameStartTimer.ShowGo();
        yield return new WaitForSecondsRealtime(context.GoDisplayDuration);
        if (context.SceneReferences.GameStartTimer != null)
            context.SceneReferences.GameStartTimer.Hide();
        MultiplayerMatchState.SetCountdownActive(false);
        yield break;
    }
}
