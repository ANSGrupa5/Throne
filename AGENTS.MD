Throne AI Development Agents
Project

Throne is a small / solo Unity C# game project.

Current priorities:

Establish basic AI-assisted workflow files.
Implement basic working FishNet multiplayer.
Improve lobby system only after multiplayer is validated.
Human owner

The human owner is responsible for:

Unity Editor validation.
Scene changes.
Prefab changes.
Hierarchy changes.
Serialized references.
Prefab overrides.
Package installation/removal.
Final accept/reject decisions.

The human should not ask agents to blindly edit Unity scenes, prefabs, serialized assets, or ProjectSettings.

Project Advisor

The Project Advisor is responsible for:

Conservative technical planning.
Small task packets for Codex or Aider.
Risk review.
Scope control.
Separating code-safe work from Unity Editor work.
Reviewing repo maps and proposed changes.

The Project Advisor does not claim to inspect files unless those files were provided.

Aider repo-map agent

Aider is used for:

Repo maps.
Lightweight file inspection.
Finding relevant files.
Summarizing existing structure.
Producing focused context for the Project Advisor.

Aider should avoid generated Unity folders and large third-party/demo folders unless explicitly requested.

Codex coding agent

Codex is used for bounded code changes.

Codex may edit C# scripts when the task packet allows it.

Codex must not blindly edit:

*.unity
*.prefab
*.asset
*.meta
ProjectSettings/**
Packages/**
third-party plugin folders

If a change requires Unity Editor work, Codex should report the required manual operation instead of editing serialized files.

Hard rules
Prefer the smallest sensible change.
Do not rewrite broad systems.
Do not create new managers, services, or singletons without explicit justification.
Keep singleplayer working while multiplayer is fixed.
Keep risky Unity changes manual.
Record important decisions in DECISIONS.md.
Track the current task in TASK.md.