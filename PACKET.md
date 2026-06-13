# Codex Task Packet: Inspect FishNet Multiplayer Flow

## Goal

Inspect the current singleplayer and multiplayer match flow in Throne and report the smallest safe code seam for making basic FishNet multiplayer work.

This is an inspection task only. Do not edit files.

## Context

Throne is a small Unity C# game using FishNet.

Current project priorities:

1. AI stack is now established.
2. Next goal is basic working FishNet multiplayer.
3. Lobby polish is out of scope until multiplayer works.

Aider identified these likely ownership areas:

- `MatchInitializer.cs` handles match initialization and appears mixed between singleplayer and multiplayer.
- `EndGameController.cs` handles match end and appears mixed between modes.
- `GameSessionRuntime.cs` stores session data and has an `isSingleplayer`-style mode flag.
- `MultiplayerRuntimeBootstrap.cs` appears responsible for FishNet startup.
- `MultiplayerSessionDriver.cs` appears responsible for networked match start/end flow.
- `PlayerVehicleInput.cs` appears involved in local input and FishNet ownership/RPC behavior.

## Files in scope

Read these files:

- `Assets/_Project/_Game/Config/GameSessionRuntime.cs`
- `Assets/_Project/_Game/Config/GameSessionBootstrap.cs`
- `Assets/_Project/_Game/Config/GameSettings.cs`

- `Assets/_Project/_Game/Spawning/MatchInitializer.cs`
- `Assets/_Project/_Game/Spawning/EndGameController.cs`
- `Assets/_Project/_Game/Spawning/GameStartTimer.cs`
- `Assets/_Project/_Game/Spawning/GameTimer.cs`
- `Assets/_Project/_Game/Spawning/SpawnSpot.cs`
- `Assets/_Project/_Game/Spawning/GameOverPayload.cs`

- `Assets/_Project/_Features/Multiplayer/MultiplayerRuntimeBootstrap.cs`
- `Assets/_Project/_Features/Multiplayer/MultiplayerSessionDriver.cs`
- `Assets/_Project/_Features/Multiplayer/MultiplayerMatchState.cs`

- `Assets/_Project/_Features/Vehicle/Scripts/VehicleController.cs`
- `Assets/_Project/_Features/Vehicle/Scripts/PlayerVehicleInput.cs`
- `Assets/_Project/_Features/Vehicle/Scripts/BotVehicleInput.cs`
- `Assets/_Project/_Features/Vehicle/Scripts/IVehicleCommandSource.cs`
- `Assets/_Project/_Features/Vehicle/Scripts/VehicleCommand.cs`

## Files out of scope

Do not edit or inspect deeply unless absolutely required:

- `*.unity`
- `*.prefab`
- `*.asset`
- `*.meta`
- `Packages/**`
- `ProjectSettings/**`
- `Assets/Plugins/FishNet/**`
- `Assets/Plugins/ParrelSync/**`

## Hard constraints

- Do not edit files.
- Do not create new abstractions.
- Do not introduce new managers, services, or singletons.
- Do not propose a rewrite.
- Do not propose blind scene or prefab edits.
- Do not touch FishNet vendor code.
- Keep singleplayer working.
- Focus on first playable multiplayer loop.

## Required report

Report the actual current flow:

1. Singleplayer start flow:
   - how settings are created
   - how session runtime is populated
   - how player/bots spawn
   - how countdown starts
   - how match ends

2. Multiplayer host flow:
   - how FishNet starts
   - how session runtime is populated
   - how network objects are spawned
   - how match start is synchronized
   - how match end is synchronized

3. Multiplayer client flow:
   - how client connects
   - whether client spawns anything locally
   - where ownership is assigned
   - where input authority is checked

4. Mixed responsibilities:
   - exact methods where singleplayer and multiplayer logic are mixed
   - exact methods where `Time.timeScale`, frozen state, spawning, or game over handling are mode-dependent

5. Minimal patch recommendation:
   - list exact files that should change
   - list exact files that should not change
   - explain the smallest code seam
   - explain any manual Unity Editor validation needed

## Do not do

- Do not implement.
- Do not rename files.
- Do not move files.
- Do not edit scenes, prefabs, assets, or meta files.
- Do not add interfaces like `IMatchInitializer`, `IEndGameHandler`, or `IGameStateFreezer` unless you explicitly justify why a simpler local branch cannot work.

## Report back with

- Summary
- Current flow
- Problems found
- Smallest safe seam
- Files to change in first patch
- Unity/manual validation risks
- Proposed next Codex patch packet