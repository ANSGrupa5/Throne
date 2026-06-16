# Decisions

## Multiplayer Runtime Bootstrap

- `MultiplayerRuntimeBootstrap` no longer auto-creates itself before scene load.
- FishNet `NetworkManager`, `DefaultPrefabObjects`, Tugboat, session-driver prefab, and main-menu scene are now expected to be configured explicitly in Unity.
- `MultiplayerRuntimeBootstrap` may find an existing `NetworkManager` as a fallback, but it no longer creates one or loads network resources from `Resources`.
- Multiplayer match countdown/end-game RPC bridge code remains absent from `MultiplayerSessionDriver`; match-level replication stays outside session-driver RPCs.

## GameOver Scene Loading

- `EndGameController` no longer stores the GameOver scene as a hard-coded string.
- Arena scenes must manually assign the GameOver scene through the `gameOverScene` `SceneReference` field.
