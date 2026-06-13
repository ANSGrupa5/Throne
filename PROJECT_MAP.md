PROJECT_MAP.md
Throne Project Map

Status: initial human/Aider-maintained map.

Top-level project folders
Assets/ — Unity assets, game code, plugins, scenes, prefabs, art.
Assets/_Project/ — main project-specific source-of-truth area.
Assets/Plugins/ — third-party/vendor assets and plugins.
Packages/ — Unity package manifest and lock data. Do not edit casually.
ProjectSettings/ — Unity project settings. High-risk.
Library/, Temp/, Logs/, UserSettings/ — generated/local Unity folders. Ignore for AI context.
Main project area
Assets/_Project/_Game/Config
Game/session config and runtime scripts.
Known relevant files:
GameSessionBootstrap.cs
GameSessionRuntime.cs
GameSettings.cs
BotsSettings.cs
PlayerLook.cs
Assets/_Project/_Game/Spawning
Match setup, spawn spots, timers, game over payload.
Known relevant files:
MatchInitializer.cs
SpawnSpot.cs
GameStartTimer.cs
GameTimer.cs
EndGameController.cs
Assets/_Project/_Features/Multiplayer
Current multiplayer-specific scripts.
Known relevant files:
MultiplayerRuntimeBootstrap.cs
MultiplayerSessionDriver.cs
MultiplayerMatchState.cs
Assets/_Project/_Features/Vehicle
Vehicle prefabs and movement/input scripts.
Known relevant files:
VehicleController.cs
PlayerVehicleInput.cs
BotVehicleInput.cs
IVehicleInput.cs
IVehicleCommandSource.cs
VehicleCommand.cs
Assets/_Project/_Features/TrailSystem
Trail, death, and vehicle life systems.
Known relevant files:
TrailEmitter.cs
TrailSegment.cs
VehicleLife.cs
VehicleDeathSequence.cs
VehicleColorApplier.cs
Assets/_Project/_Features/UI
Menu, lobby, settings, game-over UI scripts and prefabs.
Known relevant files:
Menu.cs
MultiplayerMenuButtons.cs
SingleplayerLobby.cs
GameOverScene.cs
SettingsManager.cs
Networking assets
Assets/_Project/Resources/Networking
Contains networking prefabs/assets.
High-risk because prefabs and serialized assets are involved.
Known files:
MultiplayerSessionDriver.prefab
DefaultPrefabObjects.asset
Scenes

High-risk. Do not blind-edit.

Assets/_Project/_Scenes/MainMenu.unity
Assets/_Project/_Scenes/SingleplayerLobby.unity
Assets/_Project/_Scenes/MultiplayerLobby.unity
Assets/_Project/_Scenes/GameOver.unity
Assets/_Project/_Scenes/TestEnvironment.unity
Assets/_Project/_Scenes/Arenas/*.unity
Plugins
Assets/Plugins/FishNet
FishNet runtime, generated support, demos, transports.
Treat as third-party/vendor code.
Do not edit unless explicitly scoped.
Assets/Plugins/ParrelSync
Local multiplayer testing helper.
Treat as plugin/vendor code.
Generated/noisy folders

Ignore for AI context:

Library/
Temp/
Logs/
UserSettings/
.vs/
generated *.csproj, *.sln, *.slnx, *.lscache
Current known priority
Finish AI stack files.
Use Aider to improve this map from actual file summaries.
Review map.
Only then plan FishNet multiplayer work.