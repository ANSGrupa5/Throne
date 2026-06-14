Current task: Patch 1 - asset/config model cleanup and explicit session requirement.

Scope:
- Add MatchSettings, MatchDefaults, MatchRules, SceneReference, TrailColorPalette, and VehiclePrefabSet.
- Add GameSessionRuntime creation from MatchSettings + VehiclePrefabSet.
- Remove production fallback session creation from MatchInitializer.
- Remove MultiplayerSessionDriver hardcoded AssetDatabase config fallbacks.

Out of scope:
- Scene, prefab, ScriptableObject asset, ProjectSettings, and package edits.
- Full SingleplayerLobby migration, MultiplayerLobby introduction, MatchInitializer split, and network bot work.
