using UnityEngine;
using System.Collections.Generic;

public static class GameOverPayload
{
    public enum EndReason
    {
        Unknown,
        TimeUp,
        LastAlive,
        Manual
    }

    public class MatchResult
    {
        public string ownerId;
        public string displayName;
        public int kills;
        public int deaths;
        public Color trailColor = Color.white;
    }

    public static EndReason reason;
    public static readonly List<MatchResult> results = new List<MatchResult>();

    public static EndReason ParseReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return EndReason.Unknown;

        return System.Enum.TryParse(value, ignoreCase: true, out EndReason parsed)
            ? parsed
            : EndReason.Unknown;
    }

    public static string GetReasonText()
    {
        return GetReasonText(reason);
    }

    public static string GetReasonText(EndReason endReason)
    {
        return endReason switch
        {
            EndReason.TimeUp => "Time up.",
            EndReason.LastAlive => "Last alive.",
            EndReason.Manual => "Ended manually.",
            _ => "Match ended."
        };
    }

    public static void Clear()
    {
        reason = EndReason.Unknown;
        results.Clear();
    }
}
