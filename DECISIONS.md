Patch 1 decisions:
- New match config scripts live under Assets/_Project/_Features/Match/Config as requested, while legacy config scripts remain under Assets/_Project/_Game/Config for transitional compatibility.
- VehicleCollectionPrefab.cs was moved to Assets/_Project/_Features/Vehicle/Config/VehiclePrefabSet.cs with its .meta file preserved so Unity keeps the script GUID.
- GameSessionRuntime.FromSettings maps MatchMode to the existing legacy integer gameMode field until downstream systems can migrate to enum-based logic.
- MatchInitializer now requires GameSessionBootstrap to provide an explicit GameSessionRuntime and no longer creates fallback sessions from legacy assets.
- MultiplayerSessionDriver now requires serialized MatchDefaults, MatchRules, VehiclePrefabSet, and TrailColorPalette references and aborts with clear errors when they are missing.

Patch 2 decisions:
- SingleplayerLobby now owns only new config references for match startup: MatchDefaults, MatchRules, TrailColorPalette, and VehiclePrefabSet.
- Singleplayer mode selection is backed by MatchMode enum values and the dropdown labels are display-only.
- SingleplayerLobby preserves existing public UI entry point names, including LoadScene(string), but match startup now loads the arena from validated MatchSettings.
- SingleplayerLobby no longer reads PlayerLook, BotsSettings, or GameSettings for match/session setup.
- Lobby time UI intentionally defaults to 1:00 as a UI selection even when MatchDefaults contains a longer template duration.
- StatsManager receives the spawned player GameObject from MatchInitializer so results/stat checks do not depend on old hardcoded prefab clone names.

Patch 3 decisions:
- MultiplayerLobby is the scene-facing controller for multiplayer buttons and delegates low-level FishNet startup to MultiplayerRuntimeBootstrap.
- MultiplayerSessionDriver remains the owner of multiplayer session creation and accepts explicit config overrides from MultiplayerLobby.
- Multiplayer sessions force BotCount to 0 after MatchRules validation and use the network VehiclePrefabSet.
- MultiplayerRuntimeBootstrap no longer owns scene UI panels, IMGUI join UI, or editor AssetDatabase fallback for the FishNet prefab collection.
- PlayerLook is now identity-only; runtime prefab ownership remains in VehiclePrefabSet/GameSessionRuntime bot entries and runtime trail colors remain in MatchSettings/GameSessionRuntime.

Patch 3A decisions:
- MultiplayerLobby owns explicit scene panel references so inactive host/join panels can be assigned in the Inspector and shown without GameObject.Find.
- HostGame shows the host/settings panel and enables Start Match if the button reference is assigned.
- JoinGame hides the connection chooser, starts the client using the address input or runtime default, and shows the optional join panel if assigned.
- MultiplayerMenuButtons is a compatibility wrapper and does not own multiplayer session logic.

Emergency Patch 3A-Revert decisions:
- MultiplayerMenuButtons no longer depends on a MultiplayerLobby reference because the current scene has the script on Canvas and the reference is null.
- MultiplayerMenuButtons directly controls Canvas child panels using serialized references with transform.Find fallback for direct children.
- MultiplayerLobby.cs remains in the project unused to avoid script deletion/GUID churn during the emergency fix.

Emergency Patch 3A.2 decisions:
- MultiplayerMenuButtons switches to the host panel before calling MultiplayerRuntimeBootstrap.HostGame so FishNet startup cannot block the visible panel change.
- Host and Join button handlers are idempotent for repeated clicks in the same lobby instance.
- MultiplayerRuntimeBootstrap remains out of Canvas panel visibility control.

Patch 3A.3 decisions:
- MultiplayerMenuButtons is the only owner of the multiplayer scene ConnectionType and Panel visibility.
- MultiplayerLobby is transitional only and no longer reads or writes scene panel active state from Awake or button methods.
- MultiplayerRuntimeBootstrap no longer auto-spawns MultiplayerSessionDriver when hosting the lobby or loading a multiplayer scene; StartMatch is the explicit spawn/use point.

Patch 3A.4 decisions:
- MultiplayerSessionDriver no longer searches the lobby scene for MatchInitializer during StartMatch.
- MultiplayerSessionDriver loads the configured session arena through FishNet scene loading, then waits briefly for MatchInitializer in the loaded arena before beginning initialization.
- MultiplayerRuntimeBootstrap remains responsible for spawning the session driver on StartMatch only, not on HostGame.

Patch 3B decisions:
- MatchInitializer does not auto-start while FishNet server/client state is active; MultiplayerSessionDriver remains the server-side multiplayer initialization entry point.
- Client-only calls to BeginMatchInitialization are ignored instead of creating fallback runtime sessions.
- NetworkPlayerVehicleInput disables local PlayerVehicleInput on network prefabs and only reads/submits local input for the owning client.
- Vehicle cameras and audio listeners are enabled only for the local owner; spectator camera/listener are disabled when a local owner vehicle activates.

Patch 4A decisions:
- MatchInitializer remains the only scene-facing component and keeps its serialized field names so existing scene references are preserved.
- Match initialization behavior is split into plain C# flow/helper classes under Assets/_Project/_Game/Spawning without changing MultiplayerSessionDriver or MultiplayerRuntimeBootstrap.
- Assembly-CSharp.csproj includes were updated mechanically because this repo's dotnet build path lists C# files explicitly.

Patch 4B decisions:
- Multiplayer countdown, GO, timer-start, and frozen-state replication now use FishNet broadcasts instead of MultiplayerSessionDriver ObserversRpc calls.
- MultiplayerRuntimeBootstrap registers client broadcast handlers before starting host/client connections and no longer respawns or waits for an active-arena MultiplayerSessionDriver before match initialization.
- MultiplayerSessionDriver still creates the multiplayer session and starts arena loading; its old match-start RPC method is left in place but no longer used by MatchStartSequence.
