using System;
using UnityEngine;

public enum MatchMode
{
    Deathmatch,
    KingOfTheHill
}

[Serializable]
public sealed class MatchSettings
{
    [SerializeField] private MatchMode matchMode = MatchMode.Deathmatch;
    [SerializeField] private SceneReference arenaScene;

    [SerializeField] private int playerCount = 1;
    [SerializeField] private int botCount = 0;

    [SerializeField] private int matchDurationSeconds = 600;
    [SerializeField] private bool suddenDeathEnabled;
    [SerializeField] private int suddenDeathTimeSeconds = 300;

    [SerializeField] private int respawnTimeSeconds = 5;
    [SerializeField] private int trailLength = 1;

    public MatchMode MatchMode
    {
        get => matchMode;
        set => matchMode = value;
    }

    public SceneReference ArenaScene
    {
        get => arenaScene;
        set => arenaScene = value;
    }

    public string ArenaSceneName => arenaScene != null ? arenaScene.SceneName : string.Empty;

    public int PlayerCount
    {
        get => playerCount;
        set => playerCount = value;
    }

    public int BotCount
    {
        get => botCount;
        set => botCount = value;
    }

    public int MatchDurationSeconds
    {
        get => matchDurationSeconds;
        set => matchDurationSeconds = value;
    }

    public bool SuddenDeathEnabled
    {
        get => suddenDeathEnabled;
        set => suddenDeathEnabled = value;
    }

    public int SuddenDeathTimeSeconds
    {
        get => suddenDeathTimeSeconds;
        set => suddenDeathTimeSeconds = value;
    }

    public int RespawnTimeSeconds
    {
        get => respawnTimeSeconds;
        set => respawnTimeSeconds = value;
    }

    public int TrailLength
    {
        get => trailLength;
        set => trailLength = value;
    }

    public MatchSettings Clone()
    {
        return new MatchSettings
        {
            matchMode = matchMode,
            arenaScene = arenaScene != null ? arenaScene.Clone() : null,
            playerCount = playerCount,
            botCount = botCount,
            matchDurationSeconds = matchDurationSeconds,
            suddenDeathEnabled = suddenDeathEnabled,
            suddenDeathTimeSeconds = suddenDeathTimeSeconds,
            respawnTimeSeconds = respawnTimeSeconds,
            trailLength = trailLength
        };
    }
}
