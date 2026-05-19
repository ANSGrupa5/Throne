using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchInitializer : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Fallback assets for direct scene testing when no runtime session exists.")]
    [SerializeField] private BotsSettings botsSettings;
    [Tooltip("Fallback assets for direct scene testing when no runtime session exists.")]
    [SerializeField] private GameSettings gameSettings;
    [Tooltip("Fallback assets for direct scene testing when no runtime session exists.")]
    [SerializeField] private PlayerLook playerLook;

    [Header("Spawn")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Min(0f)] private float spawnInterval = 0.25f;
    [SerializeField, Min(1)] private int preMatchCountdownSeconds = 5;
    [SerializeField, Min(0f)] private float goDisplayDuration = 0.75f;
    [SerializeField] private GameStartTimer gameStartTimer;
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private EndGameController endGameController;

    [Header("Bot AI")]
    [SerializeField] private LayerMask botMapBoundaryMask;
    [SerializeField] private LayerMask botSuddenDeathMask;
    [SerializeField] private LayerMask botTrailMask;
    [SerializeField] private LayerMask botPowerupMask;
    [SerializeField] private Transform botMapCenter;

    public static event Action OnMatchStart;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private bool _isFreezeOwned;

    [Obsolete]
    private void Awake()
    {
        BindSceneReferences();
    }

    private void Start()
    {
        StartCoroutine(InitializeRoutine());
    }

    [Obsolete]
    private void BindSceneReferences()
    {
        if (endGameController == null)
            endGameController = FindObjectOfType<EndGameController>();

        if (endGameController == null)
            return;

        AreaBoundaryScript areaBoundary = FindObjectOfType<AreaBoundaryScript>();
        if (areaBoundary != null)
            endGameController.SetAreaBoundary(areaBoundary);
    }

    private IEnumerator InitializeRoutine()
    {
        GameSessionRuntime session = ResolveSession();

        if (!TryValidateSession(session, out string validationError))
        {
            Debug.LogError($"Match initialization aborted: {validationError}");
            yield break;
        }

        var spots = SpawnSpot.Active.ToList();
        if (spots.Count == 0)
        {
            Debug.LogWarning("No SpawnSpots found in scene.");
            yield break;
        }

        int totalBots = 0;
        EnsureBotLooks(session);
        totalBots = session.botLooks.Count;

        int totalToSpawn = totalBots + (session.playerPrefab != null ? 1 : 0);

        if (totalToSpawn > spots.Count)
        {
            Debug.LogError($"Match initialization aborted: requested {totalToSpawn} entities but only {spots.Count} SpawnSpots are available.");
            yield break;
        }

        List<SpawnSpot> chosen = SelectSpawnSpots(spots, totalToSpawn);
        if (chosen.Count < totalToSpawn)
        {
            Debug.LogError($"Match initialization aborted: could not reserve enough SpawnSpots ({chosen.Count}/{totalToSpawn}).");
            yield break;
        }

        int index = 0;
        SetFreeze(true);

        // Spawn player first
        if (session.playerPrefab != null)
        {
            SpawnAt(session, session.playerPrefab, chosen[index], session.playerDisplayName, session.playerOwnerId, session.playerTrailColor, false);
            index++;
            yield return new WaitForSecondsRealtime(spawnInterval);
        }

        // Spawn bots
        for (int i = 0; i < session.botLooks.Count; i++)
        {
            if (index >= chosen.Count) break;

            PlayerLook botLook = session.botLooks[i];
            SpawnAt(session, botLook.playerPrefab, chosen[index], botLook.displayName, botLook.ownerId, botLook.trailColor, true);
            index++;
            yield return new WaitForSecondsRealtime(spawnInterval);
        }

        // Wait one frame to ensure all Awake/Start run
        yield return null;

        // Start countdown after all spawns
        yield return StartCoroutine(CountdownAndStart(preMatchCountdownSeconds));

        SetFreeze(false);

        if (gameTimer != null)
            gameTimer.Begin(session.matchDuration);
        OnMatchStart?.Invoke();
    }

    private void SpawnAt(GameSessionRuntime session, GameObject prefab, SpawnSpot spot, string displayName, string ownerId, Color trailColor, bool isBot)
    {
        if (prefab == null || spot == null) return;

        Vector3 pos = spot.Position;
        Quaternion rot = spot.Rotation;
        GameObject go = Instantiate(prefab, pos, rot);
        _spawned.Add(go);

        VehicleColorApplier colorApplier = go.GetComponent<VehicleColorApplier>();
        if (colorApplier == null)
            colorApplier = go.AddComponent<VehicleColorApplier>();

        colorApplier.SetColor(trailColor);

        VehicleLife life = go.GetComponent<VehicleLife>();
        if (life == null)
            life = go.AddComponent<VehicleLife>();
        life.ConfigureSpawn(pos, rot);
        life.ConfigureIdentity(displayName, ownerId);
        session.GetOrCreateStats(ownerId, displayName, trailColor);

        TrailEmitter trailEmitter = go.GetComponent<TrailEmitter>();
        if (trailEmitter == null)
            trailEmitter = go.AddComponent<TrailEmitter>();
        trailEmitter.Configure(life, trailColor, session != null ? session.trailLength : 1);

        if (go.GetComponent<VehicleDeathSequence>() == null)
            go.AddComponent<VehicleDeathSequence>();

        if (isBot)
        {
            if (go.GetComponent<IVehicleCommandSource>() == null)
                go.AddComponent<BotVehicleInput>();

            BotVehicleInput botInput = go.GetComponent<BotVehicleInput>();
            if (botInput != null)
                botInput.ConfigureRuntime(botMapBoundaryMask, botSuddenDeathMask, botTrailMask, botPowerupMask, botMapCenter);

            if (go.GetComponent<BotRaycastDebugger>() == null)
                go.AddComponent<BotRaycastDebugger>();
            return;
        }

        if (go.GetComponent<IVehicleCommandSource>() == null)
            go.AddComponent<PlayerVehicleInput>();
    }

    private void EnsureBotLooks(GameSessionRuntime session)
    {
        if (session == null)
            return;

        if (session.botLooks.Count > 0)
            return;

        List<GameObject> prefabs = new List<GameObject>();
        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry == null || entry.prefab == null || entry.count <= 0)
                continue;

            for (int repeat = 0; repeat < entry.count; repeat++)
                prefabs.Add(entry.prefab);
        }

        if (prefabs.Count == 0)
            return;

        GameObject defaultBotPrefab = session.botDefaultPrefab;
        if (defaultBotPrefab == null)
            defaultBotPrefab = prefabs[0];

        List<Color> availableColors = new List<Color>();
        for (int i = 0; i < session.trailColorPalette.Count; i++)
        {
            Color color = session.trailColorPalette[i];
            if (color == session.playerTrailColor)
                continue;

            availableColors.Add(color);
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            Color color = availableColors.Count > 0
                ? PickAndRemoveColor(availableColors)
                : (session.trailColorPalette.Count > 0
                    ? session.trailColorPalette[UnityEngine.Random.Range(0, session.trailColorPalette.Count)]
                    : Color.white);

            PlayerLook look = ScriptableObject.CreateInstance<PlayerLook>();
            look.hideFlags = HideFlags.DontSave;
            look.playerPrefab = defaultBotPrefab;
            look.displayName = $"BOT{i + 1}";
            look.ownerId = $"bot_{i + 1}";
            look.trailColor = color;
            session.botLooks.Add(look);
        }
    }

    private Color PickAndRemoveColor(List<Color> availableColors)
    {
        int index = UnityEngine.Random.Range(0, availableColors.Count);
        Color selected = availableColors[index];
        availableColors.RemoveAt(index);
        return selected;
    }

    private GameSessionRuntime ResolveSession()
    {
        if (GameSessionBootstrap.TryGetSession(out var activeSession))
            return activeSession;

        Debug.LogWarning("No runtime session found. Falling back to default ScriptableObject assets.");
        return GameSessionRuntime.FromDefaults(gameSettings, botsSettings, playerLook);
    }

    private List<SpawnSpot> SelectSpawnSpots(List<SpawnSpot> available, int count)
    {
        List<SpawnSpot> candidates = new List<SpawnSpot>(available.Where(s => s.IsAvailable));
        List<SpawnSpot> result = new List<SpawnSpot>();

        if (count <= 0 || candidates.Count == 0) return result;

        // Filter by clear spots first
        candidates = candidates.Where(s => s.IsClear(obstacleMask)).ToList();

        if (candidates.Count == 0)
        {
            // fallback to any available
            candidates = new List<SpawnSpot>(available.Where(s => s.IsAvailable));
        }

        if (count >= candidates.Count)
        {
            result.AddRange(candidates);
            return result;
        }

        // Anti-clump selection: pick first random, then pick spots that maximize distance to existing selection
        System.Random rng = new System.Random();
        int firstIndex = rng.Next(0, candidates.Count);
        result.Add(candidates[firstIndex]);
        candidates.RemoveAt(firstIndex);

        while (result.Count < count && candidates.Count > 0)
        {
            SpawnSpot best = null;
            float bestMinDist = -1f;
            foreach (var c in candidates)
            {
                float minDist = float.MaxValue;
                foreach (var chosen in result)
                {
                    float d = Vector3.SqrMagnitude(c.Position - chosen.Position);
                    if (d < minDist) minDist = d;
                }
                if (minDist > bestMinDist)
                {
                    bestMinDist = minDist;
                    best = c;
                }
            }
            if (best == null) break;
            result.Add(best);
            candidates.Remove(best);
        }

        return result;
    }

    private IEnumerator CountdownAndStart(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            Debug.Log(i);
            if (gameStartTimer != null)
                gameStartTimer.ShowCount(i);
            yield return new WaitForSecondsRealtime(1f);
        }

        Debug.Log("GO");
        if (gameStartTimer != null)
            gameStartTimer.ShowGo();
        yield return new WaitForSecondsRealtime(goDisplayDuration);
        if (gameStartTimer != null)
            gameStartTimer.Hide();
        yield break;
    }

    private bool TryValidateSession(GameSessionRuntime session, out string error)
    {
        if (session == null)
        {
            error = "Runtime session is null.";
            return false;
        }

        if (session.playerPrefab == null)
        {
            error = "Player prefab is not configured.";
            return false;
        }

        if (session.maxPlayers < 2)
        {
            error = $"maxPlayers is invalid ({session.maxPlayers}).";
            return false;
        }

        int totalBots = 0;
        for (int i = 0; i < session.bots.Count; i++)
        {
            GameSessionRuntime.BotSpawnEntry entry = session.bots[i];
            if (entry == null)
                continue;

            if (entry.prefab == null)
            {
                error = $"Bot entry at index {i} has no prefab.";
                return false;
            }

            if (entry.count < 0)
            {
                error = $"Bot entry at index {i} has negative count ({entry.count}).";
                return false;
            }

            totalBots += entry.count;
        }

        int totalPlayers = totalBots + 1;
        if (totalPlayers > session.maxPlayers)
        {
            error = $"Total participants ({totalPlayers}) exceed maxPlayers ({session.maxPlayers}).";
            return false;
        }

        error = null;
        return true;
    }

    private void SetFreeze(bool freeze)
    {
        if (freeze)
        {
            if (_isFreezeOwned)
                return;

            Time.timeScale = 0f;
            _isFreezeOwned = true;
            return;
        }

        if (!_isFreezeOwned)
            return;

        Time.timeScale = 1f;
        _isFreezeOwned = false;
    }

    private void OnDisable()
    {
        SetFreeze(false);
        if (gameTimer != null)
            gameTimer.Hide();
    }
}
