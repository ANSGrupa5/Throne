using UnityEngine;

public sealed class LobbyState
{
    public LobbyMode LobbyMode;
    public string ArenaSceneName;

    public int HumanPlayerCount;
    public int BotCount;
    public int ParticipantCount => HumanPlayerCount + BotCount;

    public MatchMode MatchMode;
    public float MatchDurationSeconds;
    public bool SuddenDeath;
    public int TrailLength;

    public int SelectedTrailColorIndex;
    public Color SelectedTrailColor;
    public int SelectedPlayerModelIndex;

    public bool IsDirty;
}
