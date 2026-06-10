using UnityEngine;

public readonly struct LobbyStateSnapshot
{
    public readonly int HumanPlayers;
    public readonly int SlotCount;
    public readonly int BotSlotMask;
    public readonly int TrailLength;
    public readonly float MatchDurationSeconds;
    public readonly int MatchModeIndex;
    public readonly bool SuddenDeath;

    public MatchMode MatchMode => LobbyStateGameSettingsAdapter.ToMatchMode(MatchModeIndex);
    public int BotCount => CountBotSlots();

    public LobbyStateSnapshot(
        int humanPlayers,
        int slotCount,
        int botSlotMask,
        int trailLength,
        float matchDurationSeconds,
        int matchModeIndex,
        bool suddenDeath)
    {
        SlotCount = Mathf.Clamp(slotCount, 0, 31);
        HumanPlayers = Mathf.Clamp(humanPlayers, 0, SlotCount);

        int validSlotMask = SlotCount <= 0 ? 0 : (1 << SlotCount) - 1;
        BotSlotMask = botSlotMask & validSlotMask;

        TrailLength = Mathf.Clamp(trailLength, 0, 3);
        MatchDurationSeconds = Mathf.Clamp(
            matchDurationSeconds,
            GameSettings.MinMatchDuration,
            GameSettings.MaxMatchDuration);
        MatchModeIndex = Mathf.Max(0, matchModeIndex);
        SuddenDeath = suddenDeath;
    }

    public static LobbyStateSnapshot FromLobbyState(LobbyState state, int slotCount, int botSlotMask)
    {
        if (state == null)
            return default;

        return new LobbyStateSnapshot(
            state.HumanPlayerCount,
            slotCount,
            botSlotMask,
            state.TrailLength,
            state.MatchDurationSeconds,
            LobbyStateGameSettingsAdapter.ToGameSettingsMatchMode(state.MatchMode),
            state.SuddenDeath);
    }

    public static LobbyStateSnapshot FromNetworkValues(
        int humanPlayers,
        int slotCount,
        int botSlotMask,
        int trailLength,
        float matchDurationSeconds,
        int matchModeIndex,
        bool suddenDeath)
    {
        return new LobbyStateSnapshot(
            humanPlayers,
            slotCount,
            botSlotMask,
            trailLength,
            matchDurationSeconds,
            matchModeIndex,
            suddenDeath);
    }

    public bool IsBotSlotOccupied(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < 31 && (BotSlotMask & (1 << slotIndex)) != 0;
    }

    private int CountBotSlots()
    {
        int count = 0;
        for (int i = HumanPlayers; i < SlotCount; i++)
        {
            if (IsBotSlotOccupied(i))
                count++;
        }

        return count;
    }
}
