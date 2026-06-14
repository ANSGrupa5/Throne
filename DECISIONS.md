Patch 1 decisions:
- New match config scripts live under Assets/_Project/_Features/Match/Config as requested, while legacy config scripts remain under Assets/_Project/_Game/Config for transitional compatibility.
- VehicleCollectionPrefab.cs was moved to Assets/_Project/_Features/Vehicle/Config/VehiclePrefabSet.cs with its .meta file preserved so Unity keeps the script GUID.
- GameSessionRuntime.FromSettings maps MatchMode to the existing legacy integer gameMode field until downstream systems can migrate to enum-based logic.
- MatchInitializer now requires GameSessionBootstrap to provide an explicit GameSessionRuntime and no longer creates fallback sessions from legacy assets.
- MultiplayerSessionDriver now requires serialized MatchDefaults, MatchRules, VehiclePrefabSet, and TrailColorPalette references and aborts with clear errors when they are missing.
