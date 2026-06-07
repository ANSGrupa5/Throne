using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

public sealed class MultiplayerSessionDriver : NetworkBehaviour
{
    public readonly struct LobbyStateSnapshot
    {
        public readonly int HumanPlayers;
        public readonly int SlotCount;
        public readonly int BotSlotMask;
        public readonly int TrailLength;
        public readonly float MatchDuration;
        public readonly int GameModeIndex;
        public readonly bool SuddenDeath;

        public LobbyStateSnapshot(int humanPlayers, int slotCount, int botSlotMask, int trailLength, float matchDuration, int gameModeIndex, bool suddenDeath)
        {
            HumanPlayers = humanPlayers;
            SlotCount = slotCount;
            BotSlotMask = botSlotMask;
            TrailLength = trailLength;
            MatchDuration = matchDuration;
            GameModeIndex = gameModeIndex;
            SuddenDeath = suddenDeath;
        }

        public bool IsBotSlotOccupied(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < 31 && (BotSlotMask & (1 << slotIndex)) != 0;
        }
    }

    public struct MatchResultSnapshot
    {
        public string OwnerId;
        public string DisplayName;
        public int Kills;
        public int Deaths;
        public Color TrailColor;
    }

    public static MultiplayerSessionDriver Instance { get; private set; }
    public static event System.Action TrailColorSelectionsChanged;
    public static event System.Action LobbyStateChanged;

    public bool IsMatchRunning { get; private set; }

    private static readonly Dictionary<int, int> TrailColorSelections = new();
    private static LobbyStateSnapshot _lobbyStateSnapshot;
    private static bool _hasLobbyStateSnapshot;
    private static int _localPreferredTrailColorIndex;
    private static int _localPaletteColorCount = 1;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        SubmitLocalPreferredTrailColor();
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (Instance == this)
            Instance = null;

        IsMatchRunning = false;
        MultiplayerMatchState.SetFrozen(false);
    }

    public static void ClearTrailColorSelections()
    {
        TrailColorSelections.Clear();
        TrailColorSelectionsChanged?.Invoke();
    }

    public static void ClearLobbyState()
    {
        _hasLobbyStateSnapshot = false;
        _lobbyStateSnapshot = default;
        LobbyStateChanged?.Invoke();
    }

    public static bool TryGetLobbyState(out LobbyStateSnapshot snapshot)
    {
        snapshot = _lobbyStateSnapshot;
        return _hasLobbyStateSnapshot;
    }

    public static void PublishHostLobbyState(int humanPlayers, int slotCount, int botSlotMask, int trailLength, float matchDuration, int gameModeIndex, bool suddenDeath)
    {
        if (Instance == null || !Instance.IsServerStarted)
            return;

        slotCount = Mathf.Clamp(slotCount, 0, 31);
        int validSlotMask = slotCount <= 0 ? 0 : (1 << slotCount) - 1;
        Instance.RpcSyncLobbyState(
            Mathf.Clamp(humanPlayers, 0, slotCount),
            slotCount,
            botSlotMask & validSlotMask,
            trailLength,
            Mathf.Clamp(matchDuration, GameSettings.MinMatchDuration, GameSettings.MaxMatchDuration),
            Mathf.Max(0, gameModeIndex),
            suddenDeath);
    }

    public static void RequestLocalTrailColor(int colorIndex, int paletteColorCount)
    {
        _localPreferredTrailColorIndex = Mathf.Max(0, colorIndex);
        _localPaletteColorCount = Mathf.Max(1, paletteColorCount);

        if (Instance != null)
            Instance.SubmitLocalPreferredTrailColor();
    }

    public static bool IsTrailColorTakenByOtherLocalPlayer(int colorIndex)
    {
        if (Instance == null)
            return false;

        return Instance.IsTrailColorTakenByOther(colorIndex);
    }

    public static bool TryGetLocalTrailColorIndex(out int colorIndex)
    {
        colorIndex = _localPreferredTrailColorIndex;

        if (Instance == null)
            return false;

        int localClientId = Instance.GetLocalClientId();
        if (localClientId < 0)
            return false;

        return TrailColorSelections.TryGetValue(localClientId, out colorIndex);
    }

    public bool TryGetTrailColorIndex(int clientId, out int colorIndex)
    {
        return TrailColorSelections.TryGetValue(clientId, out colorIndex);
    }

    private void SubmitLocalPreferredTrailColor()
    {
        if (!IsClientStarted)
            return;

        int colorIndex = Mathf.Clamp(_localPreferredTrailColorIndex, 0, _localPaletteColorCount - 1);
        if (IsServerStarted)
        {
            int clientId = GetLocalClientId();
            if (clientId >= 0)
            {
                SetTrailColorSelection(clientId, colorIndex, _localPaletteColorCount);
                SyncTrailColorSelections();
            }
            return;
        }

        if (IsClientInitialized && IsSpawned)
            SubmitTrailColorServerRpc(colorIndex, _localPaletteColorCount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitTrailColorServerRpc(int colorIndex, int paletteColorCount, FishNet.Connection.NetworkConnection caller = null)
    {
        if (caller == null || !caller.IsValid)
            return;

        SetTrailColorSelection(caller.ClientId, colorIndex, paletteColorCount);
        SyncTrailColorSelections();
    }

    private void SetTrailColorSelection(int clientId, int requestedColorIndex, int paletteColorCount)
    {
        if (clientId < 0)
            return;

        int colorCount = Mathf.Max(1, paletteColorCount);
        int assignedColorIndex = GetAvailableTrailColorIndex(clientId, requestedColorIndex, colorCount);
        TrailColorSelections[clientId] = assignedColorIndex;
    }

    private int GetAvailableTrailColorIndex(int clientId, int requestedColorIndex, int paletteColorCount)
    {
        int colorCount = Mathf.Max(1, paletteColorCount);
        int startIndex = Mathf.Clamp(requestedColorIndex, 0, colorCount - 1);
        HashSet<int> usedByOthers = TrailColorSelections
            .Where(selection => selection.Key != clientId)
            .Select(selection => selection.Value)
            .ToHashSet();

        if (!usedByOthers.Contains(startIndex))
            return startIndex;

        for (int offset = 1; offset < colorCount; offset++)
        {
            int candidate = (startIndex + offset) % colorCount;
            if (!usedByOthers.Contains(candidate))
                return candidate;
        }

        return startIndex;
    }

    private void SyncTrailColorSelections()
    {
        var selections = TrailColorSelections.OrderBy(selection => selection.Key).ToArray();
        int[] clientIds = selections.Select(selection => selection.Key).ToArray();
        int[] colorIndices = selections.Select(selection => selection.Value).ToArray();
        RpcSyncTrailColorSelections(clientIds, colorIndices);
    }

    [ObserversRpc(RunLocally = true, BufferLast = true)]
    private void RpcSyncTrailColorSelections(int[] clientIds, int[] colorIndices)
    {
        TrailColorSelections.Clear();

        if (clientIds != null && colorIndices != null)
        {
            int count = Mathf.Min(clientIds.Length, colorIndices.Length);
            for (int i = 0; i < count; i++)
                TrailColorSelections[clientIds[i]] = colorIndices[i];
        }

        if (TryGetLocalTrailColorIndex(out int assignedColorIndex))
            _localPreferredTrailColorIndex = assignedColorIndex;

        TrailColorSelectionsChanged?.Invoke();
    }

    [ObserversRpc(RunLocally = true, BufferLast = true)]
    private void RpcSyncLobbyState(int humanPlayers, int slotCount, int botSlotMask, int trailLength, float matchDuration, int gameModeIndex, bool suddenDeath)
    {
        _lobbyStateSnapshot = new LobbyStateSnapshot(humanPlayers, slotCount, botSlotMask, trailLength, matchDuration, gameModeIndex, suddenDeath);
        _hasLobbyStateSnapshot = true;
        LobbyStateChanged?.Invoke();
    }

    private bool IsTrailColorTakenByOther(int colorIndex)
    {
        int localClientId = GetLocalClientId();
        if (localClientId < 0)
            return false;

        return TrailColorSelections.Any(selection => selection.Key != localClientId && selection.Value == colorIndex);
    }

    private int GetLocalClientId()
    {
        if (!IsClientStarted || ClientManager == null || ClientManager.Connection == null)
            return -1;

        return ClientManager.Connection.ClientId;
    }

    [Server]
    public void StartMatch()
    {
        if (IsMatchRunning)
            return;

        MatchInitializer initializer = FindFirstObjectByType<MatchInitializer>();
        if (initializer == null)
        {
            Debug.LogWarning("Unable to start multiplayer match because no MatchInitializer was found in the active scene.");
            return;
        }

        initializer.BeginMatchInitialization();
    }

    [Server]
    public IEnumerator RunMatchStartSequence(int seconds, float goDuration, GameStartTimer startTimer, GameTimer timer, float matchDuration)
    {
        IsMatchRunning = true;
        MultiplayerMatchState.SetFrozen(true);
        RpcSetFrozen(true);

        for (int i = seconds; i > 0; i--)
        {
            RpcShowCount(i);
            yield return new WaitForSecondsRealtime(1f);
        }

        RpcShowGo();
        yield return new WaitForSecondsRealtime(goDuration);

        RpcHideCountdown();
        RpcBeginTimer(matchDuration);
        RpcSetFrozen(false);
        MultiplayerMatchState.SetFrozen(false);
    }

    [Server]
    public void BeginNetworkEndSequence(string reason, float slowDownDuration, float postFreezeDelay, float finalTimescale, string gameOverSceneName)
    {
        StartCoroutine(RunNetworkEndSequence(reason, slowDownDuration, postFreezeDelay, finalTimescale, gameOverSceneName));
    }

    [Server]
    private IEnumerator RunNetworkEndSequence(string reason, float slowDownDuration, float postFreezeDelay, float finalTimescale, string gameOverSceneName)
    {
        RpcPlayEndSequence(slowDownDuration, finalTimescale);
        yield return new WaitForSecondsRealtime(slowDownDuration + postFreezeDelay);

        List<MatchResultSnapshot> results = BuildResults();
        RpcPrepareGameOver(reason, results.ToArray());

        SceneLoadData sceneLoadData = new(gameOverSceneName);
        sceneLoadData.ReplaceScenes = ReplaceOption.All;
        sceneLoadData.PreferredActiveScene = new PreferredScene(new SceneLookupData(gameOverSceneName));
        InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);
    }

    private List<MatchResultSnapshot> BuildResults()
    {
        List<MatchResultSnapshot> results = new();
        GameSessionRuntime session = GameSessionBootstrap.CurrentSession;
        if (session == null)
            return results;

        for (int i = 0; i < session.playerStats.Count; i++)
        {
            GameSessionRuntime.PlayerMatchStats stats = session.playerStats[i];
            if (stats == null)
                continue;

            results.Add(new MatchResultSnapshot
            {
                OwnerId = stats.ownerId,
                DisplayName = stats.displayName,
                Kills = stats.kills,
                Deaths = stats.deaths,
                TrailColor = stats.trailColor
            });
        }

        return results;
    }

    [ObserversRpc(RunLocally = true, BufferLast = true)]
    private void RpcShowCount(int seconds)
    {
        GameStartTimer startTimer = FindFirstObjectByType<GameStartTimer>(FindObjectsInactive.Include);
        if (startTimer != null)
            startTimer.ShowCount(seconds);
    }

    [ObserversRpc(RunLocally = true, BufferLast = true)]
    private void RpcShowGo()
    {
        GameStartTimer startTimer = FindFirstObjectByType<GameStartTimer>(FindObjectsInactive.Include);
        if (startTimer != null)
            startTimer.ShowGo();
    }

    [ObserversRpc(RunLocally = true, BufferLast = true)]
    private void RpcHideCountdown()
    {
        GameStartTimer startTimer = FindFirstObjectByType<GameStartTimer>(FindObjectsInactive.Include);
        if (startTimer != null)
            startTimer.Hide();
    }

    [ObserversRpc(RunLocally = true, BufferLast = true)]
    private void RpcBeginTimer(float matchDuration)
    {
        GameTimer timer = FindFirstObjectByType<GameTimer>(FindObjectsInactive.Include);
        if (timer != null)
            timer.Begin(matchDuration);
    }

    [ObserversRpc(RunLocally = true)]
    private void RpcSetFrozen(bool frozen)
    {
        MultiplayerMatchState.SetFrozen(frozen);
    }

    [ObserversRpc(RunLocally = true)]
    private void RpcPlayEndSequence(float slowDownDuration, float finalTimescale)
    {
        StartCoroutine(PlayEndSequenceLocally(slowDownDuration, finalTimescale));
    }

    [ObserversRpc(RunLocally = true)]
    private void RpcPrepareGameOver(string reason, MatchResultSnapshot[] results)
    {
        GameOverPayload.Clear();
        GameOverPayload.reason = reason;

        if (results == null)
            return;

        for (int i = 0; i < results.Length; i++)
        {
            MatchResultSnapshot result = results[i];
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

    private IEnumerator PlayEndSequenceLocally(float slowDownDuration, float finalTimescale)
    {
        float initialTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        float elapsed = 0f;

        while (elapsed < slowDownDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slowDownDuration);
            Time.timeScale = Mathf.Lerp(initialTimeScale, finalTimescale, t);
            yield return null;
        }

        Time.timeScale = finalTimescale;
    }
}
