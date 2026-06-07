using UnityEngine;
using System.Collections.Generic;

public static class GameOverPayload
{
    public class MatchResult
    {
        public string ownerId;
        public string displayName;
        public int kills;
        public int deaths;
        public Color trailColor = Color.white;
    }

    public static string reason;
    public static readonly List<MatchResult> results = new List<MatchResult>();

    public static void Clear()
    {
        reason = string.Empty;
        results.Clear();
    }
}
