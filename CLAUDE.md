# CLAUDE.md — Math Runner

Educational 2D auto-runner platformer in **Unity (C#)**. The player jumps onto the platform showing the correct answer to a math question. Targets PC (keyboard) and Android/iOS (touch).

**Full design and roadmap: [GAME_DESIGN.md](GAME_DESIGN.md). Read it before starting any feature.**

## Rules for Claude
- **Section 1 of GAME_DESIGN.md is instructor-approved.** Never remove or contradict it. Suggest improvements separately and mark them as optional.
- Work phase by phase (GAME_DESIGN.md §4). Check off tasks there when they are finished.
- Claude can control the open Unity Editor through the `unity-mcp` tools (`Unity_RunCommand` runs C# in the Editor, `Unity_GetConsoleLogs` reads the Console, the capture tools take screenshots). Prefer doing Editor setup this way (components, Inspector fields, layers, tags, scenes, prefabs) instead of asking me to click through it. Only fall back to step-by-step instructions if the Editor isn't connected or the task needs manual work (e.g. importing art).
- After changing scripts or the scene, check the Console for errors before saying it works, and take a screenshot when the change is visual.
- Don't hand-edit `.unity`, `.prefab`, or `.asset` YAML files. Use the Editor (via `Unity_RunCommand`) so Unity keeps its own references consistent.
- Keep changes small and focused, one system at a time.

## Tech Stack
- Unity 6 LTS, 2D URP
- Packages: Input System (new), TextMeshPro, Cinemachine, 2D Animation, Unity Test Framework
- Physics: Rigidbody2D + Collider2D

## Code Conventions
- Namespace: `MathRunner.<Folder>` (e.g. `MathRunner.Math`, `MathRunner.Player`)
- Scripts live in `Assets/_Project/Scripts/<Folder>/`. File name = class name.
- `PascalCase` for types, methods, and properties; `_camelCase` for private fields
- Use `[SerializeField] private` instead of public fields
- Cache component references in `Awake`. No `GetComponent` / `Find*` calls in `Update`.
- Tunable values go in ScriptableObjects (`LevelConfig`), not hard-coded constants
- Systems communicate through `GameEvents` (C# events), not direct references to each other
- Use the **new Input System** only, through `InputReader`. Never use `Input.GetKey`.
- Use `Mathf`, not `System.Math` / `Math.`: inside `MathRunner.*` the name `Math` resolves to the `MathRunner.Math` namespace and won't compile.
- Physics in `FixedUpdate`, input reading in `Update`

## Architecture Notes
- `QuestionGenerator`, `DistractorGenerator`, and `DifficultyTracker` are **pure C# classes** (not MonoBehaviours). They take a `System.Random` so results can be reproduced, and they are covered by EditMode tests in `Assets/_Project/Tests/EditMode/`.
- Math answers are **whole numbers and never negative** (unless GAME_DESIGN.md §6 decides otherwise).
- Answer platform distance = `runSpeed × timeLimit`, so the timer and the physical layout always match.
- One `Game` scene loads a `LevelConfig`. Levels are built from chunk prefabs.

## Useful Claude Skills (Unity plugin)
`unity:new-unity-project`, `unity:2d-pixel-perfect`, `unity:ui-ugui` / `unity:ui-uitk`, `unity:optimize-text-mesh-pro`, `unity:localization`, `unity:sprite-editor`, `unity:tilemap-palette-create`

## Current Progress
<!-- Update this as phases finish -->
- Phase: **1 — Core Movement** (done, pending my feel-test with a real keyboard) → next: **Phase 2 — Math Engine**
- Movement sandbox: `Assets/_Project/Scenes/Sandbox_Movement.unity` (`SampleScene` is the untouched template scene)
- Unity MCP notes: `Unity_RunCommand` blocks `System.Reflection` (use `SendMessage` / public APIs). Entering/exiting Play mode and script changes cause a domain reload, so the tools are unavailable for a few seconds. Play mode runs much slower than real time while the Editor is unfocused, so test movement deterministically (`Physics2D.simulationMode = Script` + `Physics2D.Simulate`, restoring the mode afterwards) instead of with real-time sampling.
- Phase 2 needs asmdefs: EditMode tests can't reference `Assembly-CSharp`, so add a runtime asmdef under `Scripts/` and a test asmdef under `Tests/EditMode/`.
- Project: Unity 6000.3.11f1, URP, git repo at this folder (`MATH_RUNNERV1`)
- Layers: 6 Ground, 7 Player, 8 Obstacle, 9 AnswerPlatform
- Decisions made: none yet (see GAME_DESIGN.md §6)
