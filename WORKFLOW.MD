Throne AI Workflow
Normal process
Human describes a problem or goal.
Project Advisor scopes the task.
Aider creates or updates PROJECT_MAP.md when repository context is needed.
Project Advisor reviews the repo map and writes a task packet.
Codex performs bounded code changes.
Human validates in Unity.
Important decisions are added to DECISIONS.md.
Current active work is tracked in TASK.md.
Unity safety boundary

Manual Unity Editor work is required for:

Scenes.
Prefabs.
Hierarchy changes.
Inspector references.
Serialized references.
Prefab overrides.
ScriptableObject asset edits.
ProjectSettings and Packages changes.

Agents may describe intended Editor operations, but should not blindly patch serialized Unity YAML.

Task packet rules

Each Codex task should include:

Goal.
Context.
Files in scope.
Files out of scope.
Hard constraints.
Required changes.
Validation steps.
Do not do.
Report back with.
Repo map rules

PROJECT_MAP.md should summarize the repo, not duplicate it.

It should focus on:

Source-of-truth folders.
Gameplay systems.
UI/menu/lobby files.
FishNet/networking files.
Risky Unity asset areas.
Generated/noisy folders.
Files that should not be manually edited.
Current priority

Do not start multiplayer refactoring until the AI stack and repo map are usable.