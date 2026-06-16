using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public sealed class MatchSpawnService
{
    private const string MultiplayerSpawnLogPrefix = "[MatchInitializer:MultiplayerSpawn]";

    private readonly MatchSpawnPlanner _spawnPlanner;
    private readonly BotSpawnFactory _botSpawnFactory;
    private readonly MonoBehaviour _coroutineHost;
    private readonly List<GameObject> _spawned = new List<GameObject>();

    public MatchSpawnService(MatchSpawnPlanner spawnPlanner, BotSpawnFactory botSpawnFactory, MonoBehaviour coroutineHost)
    {
        _spawnPlanner = spawnPlanner;
        _botSpawnFactory = botSpawnFactory;
        _coroutineHost = coroutineHost;
    }

    public bool TrySelectSpawnSpots(int totalToSpawn, out List<SpawnSpot> chosen)
    {
        return _spawnPlanner.TrySelectSpawnSpots(totalToSpawn, out chosen);
    }

    public GameObject SpawnLocalAt(GameSessionRuntime session, GameObject prefab, SpawnSpot spot, string displayName, string ownerId, Color trailColor, bool isBot)
    {
        if (prefab == null || spot == null) return null;

        Vector3 pos = spot.Position;
        Quaternion rot = spot.Rotation;
        GameObject go = Object.Instantiate(prefab, pos, rot);
        _spawned.Add(go);

        if (!ConfigureSpawnedVehicle(session, go, prefab, pos, rot, displayName, ownerId, trailColor, isBot))
            return null;

        return go;
    }

    public GameObject SpawnNetworkAt(GameSessionRuntime session, GameObject prefab, SpawnSpot spot, string displayName, string ownerId, Color trailColor, bool isBot, NetworkConnection ownerConnection)
    {
        if (prefab == null)
        {
            Debug.LogError($"{MultiplayerSpawnLogPrefix} Cannot spawn network vehicle for '{displayName}' because prefab is null.");
            return null;
        }

        if (spot == null)
        {
            Debug.LogError($"{MultiplayerSpawnLogPrefix} Cannot spawn '{prefab.name}' for '{displayName}' because spawn spot is null.");
            return null;
        }

        if (!isBot && ownerConnection == null)
        {
            Debug.LogError(
                $"{MultiplayerSpawnLogPrefix} Refusing to spawn human player prefab '{prefab.name}' " +
                "because ownerConnection is null. Human network player vehicles must be owned.");
            return null;
        }

        Vector3 pos = spot.Position;
        Quaternion rot = spot.Rotation;
        GameObject go = Object.Instantiate(prefab, pos, rot);
        _spawned.Add(go);

        if (!ConfigureSpawnedVehicle(session, go, prefab, pos, rot, displayName, ownerId, trailColor, isBot))
            return null;

        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"{MultiplayerSpawnLogPrefix} Prefab '{prefab.name}' has no NetworkObject.");
            Object.Destroy(go);
            _spawned.Remove(go);
            return null;
        }

        if (InstanceFinder.IsServerStarted)
        {
            Debug.Log(
                $"{MultiplayerSpawnLogPrefix} Spawning '{go.name}' " +
                $"participantId='{ownerId}' displayName='{displayName}' " +
                $"ownerClientId={(ownerConnection != null ? ownerConnection.ClientId.ToString() : "<null>")} " +
                $"isBot={isBot} position={pos}");

            InstanceFinder.ServerManager.Spawn(go, ownerConnection);

            Debug.Log(
                $"{MultiplayerSpawnLogPrefix} Spawn submitted '{go.name}' " +
                $"ObjectId={networkObject.ObjectId} " +
                $"Owner={(networkObject.Owner != null ? networkObject.Owner.ClientId.ToString() : "<null>")} " +
                $"ExpectedOwner={(ownerConnection != null ? ownerConnection.ClientId.ToString() : "<null>")}");

            if (ownerConnection != null && _coroutineHost != null)
                _coroutineHost.StartCoroutine(LogOwnershipNextFrame(networkObject, ownerConnection.ClientId));
        }

        return go;
    }

    private bool ConfigureSpawnedVehicle(GameSessionRuntime session, GameObject go, GameObject prefab, Vector3 pos, Quaternion rot, string displayName, string ownerId, Color trailColor, bool isBot)
    {
        trailColor = TrailColorPalette.SanitizeColor(trailColor, Color.white);

        VehicleColorApplier colorApplier = go.GetComponent<VehicleColorApplier>();
        if (colorApplier != null)
            colorApplier.SetColor(trailColor);

        Color spawnedVehicleColor = colorApplier != null
            ? TrailColorPalette.SanitizeColor(colorApplier.GetColor(), trailColor)
            : trailColor;

        VehicleLife life = go.GetComponent<VehicleLife>();
        if (life == null)
        {
            Debug.LogError($"Match initialization aborted: spawned prefab '{prefab.name}' has no VehicleLife component.");
            Object.Destroy(go);
            _spawned.Remove(go);
            return false;
        }

        life.ConfigureSpawn(pos, rot);
        life.ConfigureIdentity(displayName, ownerId);
        GameSessionRuntime.PlayerMatchStats stats = session.GetOrCreateStats(ownerId, displayName, spawnedVehicleColor);
        stats.trailColor = spawnedVehicleColor;

        TrailEmitter trailEmitter = go.GetComponent<TrailEmitter>();
        if (trailEmitter != null)
            trailEmitter.Configure(life, spawnedVehicleColor, session != null ? session.trailLength : 1);

        if (isBot)
            _botSpawnFactory.ConfigureBotInput(go);

        return true;
    }

    private static IEnumerator LogOwnershipNextFrame(NetworkObject networkObject, int expectedOwnerClientId)
    {
        yield return null;

        if (networkObject == null)
            yield break;

        Debug.Log(
            $"{MultiplayerSpawnLogPrefix} Post-spawn ownership '{networkObject.name}' " +
            $"ObjectId={networkObject.ObjectId} " +
            $"Owner={(networkObject.Owner != null ? networkObject.Owner.ClientId.ToString() : "<null>")} " +
            $"ExpectedOwner={expectedOwnerClientId}");
    }
}
