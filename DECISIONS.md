DECISIONS.md
Throne Decision Log

Keep entries short.

Template
YYYY-MM-DD — Decision title

Decision:

...

Reason:

...

Consequences:

...

Affected files/subsystems:

...
2026-06-13 — Establish AI-assisted workflow before multiplayer refactor

Decision:

Add root workflow files before starting FishNet multiplayer changes.

Reason:

The project has risky Unity scenes, prefabs, serialized assets, and mixed singleplayer/multiplayer logic.
Codex needs bounded task packets and clear no-touch rules.

Consequences:

First task is documentation and repo map.
Multiplayer refactoring waits until the map is reviewed.

Affected files/subsystems:

AGENTS.md
WORKFLOW.md
PROJECT_MAP.md
DECISIONS.md
TASK.md
.aiderignore
