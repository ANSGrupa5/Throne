using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SingleplayerMatchFlow
{
    private readonly MatchInitializationContext _context;
    private readonly MatchSpawnService _spawnService;
    private readonly BotSpawnFactory _botSpawnFactory;
    private readonly Action<bool> _setFreeze;
    private readonly Action _matchStarted;

    public SingleplayerMatchFlow(
        MatchInitializationContext context,
        MatchSpawnService spawnService,
        BotSpawnFactory botSpawnFactory,
        Action<bool> setFreeze,
        Action matchStarted)
    {
        _context = context;
        _spawnService = spawnService;
        _botSpawnFactory = botSpawnFactory;
        _setFreeze = setFreeze;
        _matchStarted = matchStarted;
    }

    public IEnumerator Run()
    {
        GameSessionRuntime session = _context.Session;
        _botSpawnFactory.EnsureBotLooks(session);
        int totalHumanPlayers = 1;
        int totalBots = Mathf.Max(0, session.maxPlayers - totalHumanPlayers);
        int totalToSpawn = totalHumanPlayers + totalBots;

        if (!_spawnService.TrySelectSpawnSpots(totalToSpawn, out List<SpawnSpot> chosen))
            yield break;

        int index = 0;
        _setFreeze(true);

        GameObject playerVehicle = _spawnService.SpawnLocalAt(session, session.playerPrefab, chosen[index], session.playerDisplayName, session.playerOwnerId, session.playerTrailColor, false);
        index++;
        StatsManager.Instance.SetPlayerVehicle(playerVehicle, session.playerDisplayName, session.playerOwnerId);

        if (DistanceTracker.Instance != null && playerVehicle != null)
            DistanceTracker.Instance.SetTarget(playerVehicle.transform);

        yield return new WaitForSecondsRealtime(_context.SpawnInterval);

        for (int i = 0; i < totalBots; i++)
        {
            if (index >= chosen.Count) break;

            PlayerLook botLook = i < session.botLooks.Count ? session.botLooks[i] : _botSpawnFactory.CreateFallbackBotLook(session, i);
            Color botColor = session.GetBotTrailColor(i);
            GameObject botPrefab = _botSpawnFactory.ResolveBotPrefab(session, i);
            _spawnService.SpawnLocalAt(session, botPrefab, chosen[index], botLook.displayName, botLook.ownerId, botColor, true);
            index++;
            yield return new WaitForSecondsRealtime(_context.SpawnInterval);
        }

        yield return null;
        yield return MatchStartSequence.RunSingleplayer(_context);

        _setFreeze(false);
        _matchStarted?.Invoke();
    }
}
