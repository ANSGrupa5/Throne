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
Serialized assets: *.asset
Unity metadata: *.meta
Packages/**
ProjectSettings/**
third-party plugin folders unless explicitly requested
Constraints
Keep workflow simple.
Do not start multiplayer refactoring yet.
Do not edit risky Unity serialized files.
Keep Aider context focused on project code, not generated Unity folders.
Plan
Create root AI stack files.
Add .aiderignore.
Run Aider to update PROJECT_MAP.md.
Project Advisor reviews the map.
Prepare first Codex task packet for FishNet multiplayer inspection only.
Validation steps
Confirm all six files exist at repo root.
Confirm .aiderignore does not ignore Assets/**/*.meta, Packages/, or ProjectSettings/.
Run Aider repo map / ask flow.
Review whether PROJECT_MAP.md names the main gameplay, UI, vehicle, spawning, and networking files.
Do not proceed to Codex implementation until the map is reviewed.
Open questions
Which FishNet version is installed?
Which current multiplayer scene is the intended first validation scene?
Which player prefab is intended for network spawning?
Current status

Draft AI stack proposed. Awaiting file creation and Aider map update.