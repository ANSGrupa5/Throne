using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public abstract class OpponentSlotView : LobbyComponent
{
    [Header("Summary")]
    [SerializeField] private TMP_Text botCountText;
    [SerializeField] private TMP_Text opponentHeading;

    [Header("Slots")]
    [SerializeField] private OpponentSlotEntryView[] opponentSlots;

    private bool _slotButtonsBound;
    private int _lastBotMutationFrame = -1;
    private UnityAction[] _addActions;
    private UnityAction[] _removeActions;

    protected bool[] BotSlots;

    public int BotCount { get; protected set; }
    public int SlotCount => opponentSlots != null ? opponentSlots.Length : 0;
    public bool HasSlots => SlotCount > 0;

    protected virtual bool CanEditBots => false;
    protected OpponentSlotEntryView[] Slots => opponentSlots;

    public void Validate(Lobby owner)
    {
        string ownerName = owner != null ? owner.name : nameof(Lobby);

        if (opponentHeading == null)
            Debug.LogError($"{nameof(Lobby)} on {ownerName} is missing scene reference '{nameof(opponentHeading)}'.", owner);

        if (opponentSlots == null || opponentSlots.Length == 0)
        {
            Debug.LogError($"{nameof(Lobby)} on {ownerName} has no opponent slots assigned.", owner);
            return;
        }

        for (int i = 0; i < opponentSlots.Length; i++)
            opponentSlots[i]?.Validate(owner, i);
    }

    public override void OnEnable()
    {
        BindLobbySlotButtons();
    }

    public override void OnDisable()
    {
        UnbindLobbySlotButtons();
    }

    public override void Refresh()
    {
        EnsureBotSlotBuffer();
        BindLobbySlotButtons();
        RefreshHeading();

        if (!HasSlots)
            return;

        int humanPlayers = GetHumanSlotCount();
        ClearHumanOccupiedBotSlots(humanPlayers);
        SyncBotCountFromSlots(humanPlayers);

        int maxBotCount = GetMaxBotCount();
        if (BotCount > maxBotCount)
            TrimBotsToMax(maxBotCount, humanPlayers);

        RefreshBotCountUI();
        ApplySlotViews(humanPlayers, CanEditBots);
    }

    public virtual void AddBot()
    {
        if (!CanEditBots || !TryBeginBotMutation())
            return;

        EnsureBotSlotBuffer();
        int humanPlayers = GetHumanSlotCount();
        if (BotSlots == null)
            return;

        for (int i = humanPlayers; i < BotSlots.Length; i++)
        {
            if (BotSlots[i])
                continue;

            SetBotSlotInternal(i, true);
            return;
        }
    }

    public virtual void RemoveBot()
    {
        if (!CanEditBots || !TryBeginBotMutation())
            return;

        EnsureBotSlotBuffer();
        int humanPlayers = GetHumanSlotCount();
        if (BotSlots == null)
            return;

        for (int i = BotSlots.Length - 1; i >= humanPlayers; i--)
        {
            if (!BotSlots[i])
                continue;

            SetBotSlotInternal(i, false);
            return;
        }
    }

    public virtual void SetBotSlot(int slotIndex, bool occupied)
    {
        if (!CanEditBots || !TryBeginBotMutation())
            return;

        SetBotSlotInternal(slotIndex, occupied);
    }

    public virtual int GetHumanSlotCount()
    {
        return 0;
    }

    public virtual int GetBotSlotMask()
    {
        if (BotSlots == null)
            return 0;

        int mask = 0;
        int limit = Mathf.Min(BotSlots.Length, 31);
        for (int i = 0; i < limit; i++)
        {
            if (BotSlots[i])
                mask |= 1 << i;
        }

        return mask;
    }

    protected virtual string HeadingText => "Bots";

    protected virtual int GetMaxBotCount()
    {
        int maxPlayers = HasSlots ? SlotCount : 6;
        return Mathf.Max(0, maxPlayers - GetHumanSlotCount());
    }

    protected virtual string GetSlotLabel(int slotIndex, bool isHuman, bool isBot)
    {
        if (isHuman)
            return slotIndex == 0 ? "HOST" : "PLAYER";

        return isBot ? "BOT" : string.Empty;
    }

    protected void ApplySlotViews(int humanPlayers, bool canEditBots)
    {
        OpponentSlotEntryView[] slots = Slots;
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            bool isHuman = i < humanPlayers;
            bool isBot = !isHuman && IsBotSlotOccupied(i);
            OpponentSlotOccupant occupant = OpponentSlotOccupant.Empty;

            if (isHuman)
                occupant = i == 0 ? OpponentSlotOccupant.Host : OpponentSlotOccupant.Player;
            else if (isBot)
                occupant = OpponentSlotOccupant.Bot;

            string text = GetSlotLabel(i, isHuman, isBot);
            bool canAddBot = canEditBots && !isHuman && !isBot;
            bool canRemoveBot = canEditBots && isBot;
            slots[i]?.ApplyState(text, occupant, canAddBot, canRemoveBot);
        }
    }

    protected bool IsBotSlotOccupied(int slotIndex)
    {
        return BotSlots != null && slotIndex >= 0 && slotIndex < BotSlots.Length && BotSlots[slotIndex];
    }

    protected void SyncBotCountFromSlots(int humanPlayers)
    {
        BotCount = 0;
        if (BotSlots == null)
            return;

        for (int i = humanPlayers; i < BotSlots.Length; i++)
        {
            if (BotSlots[i])
                BotCount++;
        }
    }

    protected void RefreshBotCountUI()
    {
        if (botCountText != null)
            botCountText.text = "Opponents";
    }

    protected void EnsureBotSlotBuffer()
    {
        int slotCount = SlotCount;
        if (BotSlots != null && BotSlots.Length == slotCount)
            return;

        BotSlots = new bool[slotCount];
        _slotButtonsBound = false;
    }

    protected void SetBotSlotInternal(int slotIndex, bool occupied)
    {
        EnsureBotSlotBuffer();

        int humanPlayers = GetHumanSlotCount();
        if (slotIndex < humanPlayers || BotSlots == null || slotIndex < 0 || slotIndex >= BotSlots.Length)
            return;

        if (BotSlots[slotIndex] == occupied)
            return;

        BotSlots[slotIndex] = occupied;
        SyncBotCountFromSlots(humanPlayers);
        RefreshBotCountUI();
        Lobby.MarkLobbyStateDirty();
        Refresh();
    }

    private void BindLobbySlotButtons()
    {
        if (_slotButtonsBound || opponentSlots == null)
            return;

        int slotCount = opponentSlots.Length;
        _addActions = new UnityAction[slotCount];
        _removeActions = new UnityAction[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            int slotIndex = i;
            _addActions[i] = () => SetBotSlot(slotIndex, true);
            _removeActions[i] = () => SetBotSlot(slotIndex, false);
            opponentSlots[i]?.Bind(_addActions[i], _removeActions[i]);
        }

        _slotButtonsBound = true;
    }

    private void UnbindLobbySlotButtons()
    {
        if (!_slotButtonsBound)
            return;

        if (opponentSlots != null)
        {
            for (int i = 0; i < opponentSlots.Length; i++)
                opponentSlots[i]?.Unbind();
        }

        _slotButtonsBound = false;
        _addActions = null;
        _removeActions = null;
    }

    protected void RefreshHeading()
    {
        if (opponentHeading != null)
            opponentHeading.text = HeadingText;
    }

    private bool TryBeginBotMutation()
    {
        if (_lastBotMutationFrame == Time.frameCount)
            return false;

        _lastBotMutationFrame = Time.frameCount;
        return true;
    }

    private void ClearHumanOccupiedBotSlots(int humanPlayers)
    {
        if (BotSlots == null)
            return;

        int limit = Mathf.Min(humanPlayers, BotSlots.Length);
        for (int i = 0; i < limit; i++)
            BotSlots[i] = false;
    }

    private void TrimBotsToMax(int maxBotCount, int humanPlayers)
    {
        if (BotSlots == null)
            return;

        int activeBots = 0;
        for (int i = humanPlayers; i < BotSlots.Length; i++)
        {
            if (!BotSlots[i])
                continue;

            activeBots++;
            if (activeBots > maxBotCount)
                BotSlots[i] = false;
        }

        SyncBotCountFromSlots(humanPlayers);
    }
}

[Serializable]
public sealed class SingleplayerOpponentSlotView : OpponentSlotView
{
    private const int FallbackDefaultBotCount = 3;

    private bool _seededDefaultBots;

    protected override bool CanEditBots => true;
    protected override string HeadingText => "Opponents";

    protected override void OnInitialize()
    {
        SeedDefaultBots();
    }

    protected override int GetMaxBotCount()
    {
        return SlotCount;
    }

    private void SeedDefaultBots()
    {
        if (_seededDefaultBots)
            return;

        EnsureBotSlotBuffer();
        if (BotSlots == null || BotSlots.Length == 0)
            return;

        _seededDefaultBots = true;

        int desiredBotCount = ResolveDefaultBotCount();
        int maxBotCount = GetMaxBotCount();
        int botsToSeed = Mathf.Clamp(desiredBotCount, 0, maxBotCount);

        for (int i = 0; i < BotSlots.Length; i++)
            BotSlots[i] = i < botsToSeed;

        SyncBotCountFromSlots(GetHumanSlotCount());
        RefreshBotCountUI();
    }

    private int ResolveDefaultBotCount()
    {
        BotsSettings botsSettings = Lobby.BotsSettings;
        if (botsSettings == null || botsSettings.bots == null || botsSettings.bots.Count == 0)
            return FallbackDefaultBotCount;

        int count = 0;
        for (int i = 0; i < botsSettings.bots.Count; i++)
        {
            BotsSettings.BotEntry entry = botsSettings.bots[i];
            if (entry != null && entry.prefab != null)
                count += Mathf.Max(0, entry.count);
        }

        return count > 0 ? count : FallbackDefaultBotCount;
    }
}

[Serializable]
public sealed class MultiplayerHostOpponentSlotView : OpponentSlotView
{
    protected override bool CanEditBots
    {
        get
        {
            MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
            return bootstrap != null && bootstrap.IsServerStarted;
        }
    }

    protected override string HeadingText => "Players";

    public override int GetHumanSlotCount()
    {
        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        if (bootstrap != null && bootstrap.IsServerStarted)
            return Mathf.Max(1, bootstrap.ConnectedPlayerCount);

        return 0;
    }

    public override void Refresh()
    {
        base.Refresh();
        if (CanEditBots)
            Lobby.MarkLobbyStateDirty();
    }

    protected override int GetMaxBotCount()
    {
        int maxPlayers = HasSlots ? SlotCount : 6;
        return Mathf.Max(0, maxPlayers - Mathf.Max(1, GetHumanSlotCount()));
    }
}

[Serializable]
public sealed class MultiplayerClientOpponentSlotView : OpponentSlotView
{
    protected override string HeadingText => "Players";

    public override void OnEnable()
    {
        base.OnEnable();
        MultiplayerSessionDriver.LobbyStateChanged += HandleLobbyStateChanged;
    }

    public override void OnDisable()
    {
        MultiplayerSessionDriver.LobbyStateChanged -= HandleLobbyStateChanged;
        base.OnDisable();
    }

    public override void AddBot()
    {
    }

    public override void RemoveBot()
    {
    }

    public override void SetBotSlot(int slotIndex, bool occupied)
    {
    }

    public override int GetHumanSlotCount()
    {
        if (MultiplayerSessionDriver.TryGetLobbyState(out LobbyStateSnapshot snapshot))
            return snapshot.HumanPlayers;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && bootstrap.IsClientStarted ? 1 : 0;
    }

    public override int GetBotSlotMask()
    {
        if (MultiplayerSessionDriver.TryGetLobbyState(out LobbyStateSnapshot snapshot))
            return snapshot.BotSlotMask;

        return 0;
    }

    public override void Refresh()
    {
        EnsureBotSlotBuffer();
        RefreshBotCountUI();
        RefreshHeading();

        OpponentSlotEntryView[] slots = Slots;
        if (slots == null)
            return;

        if (!MultiplayerSessionDriver.TryGetLobbyState(out LobbyStateSnapshot snapshot))
        {
            ApplySlotViews(GetHumanSlotCount(), false);
            return;
        }

        Lobby?.ApplySyncedLobbyStateSnapshot(snapshot);
        BotCount = CountBots(snapshot);
        RefreshBotCountUI();

        for (int i = 0; i < slots.Length; i++)
        {
            bool isHuman = i < snapshot.HumanPlayers;
            bool isBot = !isHuman && snapshot.IsBotSlotOccupied(i);
            OpponentSlotOccupant occupant = OpponentSlotOccupant.Empty;

            if (isHuman)
                occupant = i == 0 ? OpponentSlotOccupant.Host : OpponentSlotOccupant.Player;
            else if (isBot)
                occupant = OpponentSlotOccupant.Bot;

            string text = GetSlotLabel(i, isHuman, isBot);
            slots[i]?.ApplyState(text, occupant, false, false);
        }
    }

    private void HandleLobbyStateChanged()
    {
        Refresh();
    }

    private int CountBots(LobbyStateSnapshot snapshot)
    {
        int count = 0;
        int limit = Mathf.Min(snapshot.SlotCount, HasSlots ? SlotCount : snapshot.SlotCount);
        for (int i = snapshot.HumanPlayers; i < limit; i++)
        {
            if (snapshot.IsBotSlotOccupied(i))
                count++;
        }

        return count;
    }
}
