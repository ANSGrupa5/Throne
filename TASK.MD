Current Task
Current goal

Goal 1 — Add basic working AI stack support.

Files in scope
AGENTS.md
WORKFLOW.md
PROJECT_MAP.md
DECISIONS.md
TASK.md
.aiderignore
Files out of scope
Unity scenes: *.unity
Prefabs: *.prefab
Serialized assets: *.asset# Current Task

## Current goal

Goal 2 — Inspect current FishNet multiplayer flow before implementation.

The AI workflow stack now exists and `PROJECT_MAP.md` is usable enough for a first multiplayer inspection pass.

## Files in scope

* `PACKET.md`
* `PROJECT_MAP.md`

Codex inspection files:

* `Assets/_Project/_Game/Config/GameSessionRuntime.cs`

* `Assets/_Project/_Game/Config/GameSessionBootstrap.cs`

* `Assets/_Project/_Game/Config/GameSettings.cs`

* `Assets/_Project/_Game/Spawning/MatchInitializer.cs`

* `Assets/_Project/_Game/Spawning/EndGameController.cs`

* `Assets/_Project/_Game/Spawning/GameStartTimer.cs`

* `Assets/_Project/_Game/Spawning/GameTimer.cs`

* `Assets/_Project/_Game/Spawning/SpawnSpot.cs`

* `Assets/_Project/_Game/Spawning/GameOverPayload.cs`

* `Assets/_Project/_Features/Multiplayer/MultiplayerRuntimeBootstrap.cs`

* `Assets/_Project/_Features/Multiplayer/MultiplayerSessionDriver.cs`

* `Assets/_Project/_Features/Multiplayer/MultiplayerMatchState.cs`

* `Assets/_Project/_Features/Vehicle/Scripts/VehicleController.cs`

* `Assets/_Project/_Features/Vehicle/Scripts/PlayerVehicleInput.cs`

* `Assets/_Project/_Features/Vehicle/Scripts/BotVehicleInput.cs`

* `Assets/_Project/_Features/Vehicle/Scripts/IVehicleCommandSource.cs`

* `Assets/_Project/_Features/Vehicle/Scripts/VehicleCommand.cs`

## Files out of scope

* Unity scenes: `*.unity`
* Prefabs: `*.prefab`
* Serialized assets: `*.asset`
* Unity metadata: `*.meta`
* `Packages/**`
* `ProjectSettings/**`
* `Assets/Plugins/FishNet/**`
* `Assets/Plugins/ParrelSync/**`

## Constraints

* This is inspection only.
* Codex must not edit files in this pass.
* Codex must not start implementation.
* Codex must not introduce new managers, services, singletons, or broad abstractions.
* Keep singleplayer working.
* Do not start lobby polish yet.

## Plan

1. Run Codex using `PACKET.md`.
2. Let Codex inspect only the files listed in the packet.
3. Codex reports current singleplayer flow, multiplayer host flow, multiplayer client flow, mixed responsibilities, and smallest safe seam.
4. Project Advisor reviews Codex report.
5. Only then create the first implementation patch packet.

## Validation steps

After Codex inspection:

* Confirm no files were changed.
* Confirm Codex reported exact methods and files, not vague architecture.
* Confirm Codex did not propose scene/prefab/asset edits as automated work.
* Confirm proposed first patch is small and code-only, or clearly marks Unity Editor work as manual.

## Current status

Ready to run Codex inspection using `PACKET.md`.

## Open questions

* Which FishNet transport is active?
* Which arena scene is intended for first multiplayer validation?
* Which vehicle prefab is intended as the network player prefab?
* Is `MultiplayerSessionDriver.prefab` loaded or referenced correctly?

These are not blockers for inspection.
