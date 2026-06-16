using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using UnityEngine;

public sealed class MultiplayerMatchFlow
{
    private const string MultiplayerSpawnLogPrefix = "[MatchInitializer:MultiplayerSpawn]";

    private readonly MatchInitializationContext _context;
    private readonly MatchSpawnService _spawnService;
    private readonly BotSpawnFactory _botSpawnFactory;
    private readonly Action<bool> _setFreeze;
    private readonly Action _matchStarted;

    public MultiplayerMatchFlow(
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
        List<NetworkConnection> connections = GetAuthenticatedServerConnectionsSnapshot();
        if (connections.Count <= 0)
        {
            Debug.LogError($"{MultiplayerSpawnLogPrefix} Cannot initialize multiplayer match: no authenticated server connections.");
            yield break;
        }

        int totalHumanPlayers = connections.Count;
        _botSpawnFactory.EnsureBotLooks(session);
        int totalBots = Mathf.Max(0, session.maxPlayers - totalHumanPlayers);
        int totalToSpawn = totalHumanPlayers + totalBots;

        if (!_spawnService.TrySelectSpawnSpots(totalToSpawn, out List<SpawnSpot> chosen))
            yield break;

        int index = 0;
        int spawnedHumanCount = 0;
        _setFreeze(true);

        for (int playerIndex = 0; playerIndex < totalHumanPlayers; playerIndex++)
        {
            NetworkConnection ownerConnection = connections[playerIndex];
            if (ownerConnection == null || !ownerConnection.IsAuthenticated)
            {
                Debug.LogWarning($"{MultiplayerSpawnLogPrefix} Skipping invalid connection at index {playerIndex}.");
                continue;
            }

            string ownerId = $"player_{ownerConnection.ClientId}";
            string displayName = ownerConnection.IsLocalClient ? session.playerDisplayName : $"Player {ownerConnection.ClientId}";
            Color trailColor = ResolvePlayerColor(session, playerIndex);
            GameObject playerVehicle = _spawnService.SpawnNetworkAt(session, session.playerPrefab, chosen[index], displayName, ownerId, trailColor, false, ownerConnection);
            index++;
            if (playerVehicle != null)
                spawnedHumanCount++;
            if (playerIndex == 0 && playerVehicle != null)
                StatsManager.Instance.SetPlayerVehicle(playerVehicle, displayName, ownerId);
            //Temporary disable: DistanceTracker uses string to find player, unsafe.
            //DistanceTracker.Instance.GetTarget();
            yield return new WaitForSecondsRealtime(_context.SpawnInterval);
        }

        if (spawnedHumanCount <= 0)
            Debug.LogError($"{MultiplayerSpawnLogPrefix} No human network players were spawned.");

        for (int i = 0; i < totalBots; i++)
        {
            if (index >= chosen.Count) break;

            PlayerLook botLook = i < session.botLooks.Count ? session.botLooks[i] : _botSpawnFactory.CreateFallbackBotLook(session, i);
            Color botColor = session.GetBotTrailColor(i);
            GameObject botPrefab = _botSpawnFactory.ResolveBotPrefab(session, i);
            _spawnService.SpawnNetworkAt(session, botPrefab, chosen[index], botLook.displayName, botLook.ownerId, botColor, true, null);
            index++;
            yield return new WaitForSecondsRealtime(_context.SpawnInterval);
        }

        yield return null;
        yield return MatchStartSequence.RunMultiplayer(_context);

        _setFreeze(false);
        _matchStarted?.Invoke();
    }

    private static List<NetworkConnection> GetAuthenticatedServerConnectionsSnapshot()
    {
        List<NetworkConnection> result = new List<NetworkConnection>();

        if (!InstanceFinder.IsServerStarted || InstanceFinder.ServerManager == null)
            return result;

        foreach (NetworkConnection connection in InstanceFinder.ServerManager.Clients.Values)
        {
            if (connection == null)
                continue;

            if (!connection.IsAuthenticated)
                continue;

            result.Add(connection);
        }

        result.Sort((a, b) => a.ClientId.CompareTo(b.ClientId));
        return result;
    }

    private static Color ResolvePlayerColor(GameSessionRuntime session, int playerIndex)
    {
        if (session == null || session.trailColorPalette == null || session.trailColorPalette.Count == 0)
            return Color.white;

        if (playerIndex == 0)
            return TrailColorPalette.SanitizeColor(session.playerTrailColor, Color.white);

        return TrailColorPalette.SanitizeColor(session.trailColorPalette[playerIndex % session.trailColorPalette.Count], Color.white);
    }
}
