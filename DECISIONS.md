## 2026-06-13 — Move from AI stack setup to multiplayer inspection

Decision:
- Treat the AI workflow files and initial `PROJECT_MAP.md` as usable enough for the first FishNet multiplayer inspection pass.
- Run Codex in inspection-only mode before implementation.

Reason:
- `PROJECT_MAP.md` identifies the likely gameplay, spawning, multiplayer, vehicle, UI, scene, prefab, and plugin areas.
- `PACKET.md` gives Codex a bounded no-edit task.
- `TASK.md` needed to move from Goal 1 to Goal 2.

Consequences:
- Codex may inspect selected C# files.
- Codex must not edit scenes, prefabs, assets, `.meta`, packages, ProjectSettings, or FishNet vendor files.
- First implementation patch will be created only after reviewing Codex’s inspection report.

Affected files/subsystems:
- `TASK.md`
- `PACKET.md`
- `PROJECT_MAP.md`
- FishNet multiplayer planning