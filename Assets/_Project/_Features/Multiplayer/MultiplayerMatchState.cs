using UnityEngine;

public static class MultiplayerMatchState
{
    public enum CountdownDisplayState
    {
        Hidden,
        Count,
        Go
    }

    public static bool IsFrozen { get; private set; }
    public static bool IsCountdownActive { get; private set; }
    public static int CountdownValue { get; private set; }
    public static CountdownDisplayState CountdownState { get; private set; }
    public static bool IsTimerActive { get; private set; }
    public static float TimerDuration { get; private set; }
    public static float TimerStartedAtRealtime { get; private set; }
    public static int TimerGeneration { get; private set; }

    public static void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;
    }

    public static void SetCountdownActive(bool active)
    {
        if (active)
            SetCountdownCount(CountdownValue);
        else
            HideCountdown();
    }

    public static void SetCountdownValue(int value)
    {
        SetCountdownCount(value);
    }

    public static void SetCountdownCount(int value)
    {
        IsCountdownActive = true;
        CountdownState = CountdownDisplayState.Count;
        CountdownValue = value < 0 ? 0 : value;
    }

    public static void SetCountdownGo()
    {
        IsCountdownActive = true;
        CountdownState = CountdownDisplayState.Go;
        CountdownValue = 0;
    }

    public static void HideCountdown()
    {
        IsCountdownActive = false;
        CountdownState = CountdownDisplayState.Hidden;
        CountdownValue = 0;
    }

    public static void BeginTimer(float duration)
    {
        IsTimerActive = true;
        TimerDuration = Mathf.Max(0f, duration);
        TimerStartedAtRealtime = Time.realtimeSinceStartup;
        TimerGeneration++;
    }

    public static float GetTimerRemaining()
    {
        if (!IsTimerActive)
            return 0f;

        float elapsed = Time.realtimeSinceStartup - TimerStartedAtRealtime;
        return Mathf.Max(0f, TimerDuration - elapsed);
    }

    public static void Reset()
    {
        IsFrozen = false;
        IsCountdownActive = false;
        CountdownValue = 0;
        CountdownState = CountdownDisplayState.Hidden;
        IsTimerActive = false;
        TimerDuration = 0f;
        TimerStartedAtRealtime = 0f;
        TimerGeneration++;
    }
}

public static class MultiplayerHudBridge
{
    private static int _lastAppliedTimerGeneration = -1;

    public static void ApplyCountdownNow(string source)
    {
        GameStartTimer startTimer = Object.FindFirstObjectByType<GameStartTimer>(FindObjectsInactive.Include);
        if (startTimer == null)
        {
            Debug.Log($"[MultiplayerHudBridge] {source}: GameStartTimer not found.");
            return;
        }

        switch (MultiplayerMatchState.CountdownState)
        {
            case MultiplayerMatchState.CountdownDisplayState.Count:
                Debug.Log($"[MultiplayerHudBridge] {source}: ShowCount({MultiplayerMatchState.CountdownValue}).");
                startTimer.ShowCount(MultiplayerMatchState.CountdownValue);
                break;

            case MultiplayerMatchState.CountdownDisplayState.Go:
                Debug.Log($"[MultiplayerHudBridge] {source}: ShowGo().");
                startTimer.ShowGo();
                break;

            default:
                Debug.Log($"[MultiplayerHudBridge] {source}: Hide countdown.");
                startTimer.Hide();
                break;
        }
    }

    public static void ApplyTimerNow(string source)
    {
        if (!MultiplayerMatchState.IsTimerActive)
            return;

        if (_lastAppliedTimerGeneration == MultiplayerMatchState.TimerGeneration)
            return;

        GameTimer timer = Object.FindFirstObjectByType<GameTimer>(FindObjectsInactive.Include);
        if (timer == null)
        {
            Debug.Log($"[MultiplayerHudBridge] {source}: GameTimer not found.");
            return;
        }

        float remaining = MultiplayerMatchState.GetTimerRemaining();
        Debug.Log($"[MultiplayerHudBridge] {source}: Begin timer remaining={remaining:0.00}.");
        timer.Begin(remaining);

        _lastAppliedTimerGeneration = MultiplayerMatchState.TimerGeneration;
    }

    public static void ApplyAllNow(string source)
    {
        ApplyCountdownNow(source);
        ApplyTimerNow(source);
    }

    public static void ResetAppliedState()
    {
        _lastAppliedTimerGeneration = -1;
    }
}
