Current task: Patch 4B - multiplayer match-start broadcasts.

Scope:
- Move multiplayer countdown, GO, timer-start, and frozen-state replication from MultiplayerSessionDriver ObserversRpc to FishNet broadcasts.
- Register broadcast handlers from the persistent MultiplayerRuntimeBootstrap before host/client connections start.
- Keep MultiplayerSessionDriver session creation and lobby match start responsibility unchanged.

Out of scope:
- Scene, prefab, ScriptableObject asset, ProjectSettings, package, and .meta edits.
- Vehicle ownership, respawn, camera, spawn placement, NetworkManager replacement, and singleplayer flow behavior changes.
