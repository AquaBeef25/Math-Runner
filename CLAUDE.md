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
- Never write bare `Math.`: inside `MathRunner.*` the name `Math` resolves to the `MathRunner.Math` namespace and won't compile. Use `Mathf` in gameplay code and fully qualified `System.Math` / `System.Random` in the math engine.
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
- Phase: **3 — Question Zone** (done, verified in Play mode) → next: **Phase 4 — Game Systems & HUD**
- Question sandbox: `Assets/_Project/Scenes/Sandbox_QuestionZone.unity` (4 zones: untimed, 4 s, 3 s, 2 s; director on Level 5)
- Player ground mask must include both `Ground` and `AnswerPlatform` layers
- Phase 1 movement still pending my feel-test with a real keyboard
- Assemblies: `MathRunner.Math` (Scripts/Math, pure math, no MonoBehaviours), `MathRunner` (all other Scripts, references Math + Input System), `MathRunner.Tests.EditMode` (Tests/EditMode). New script folders under `Scripts/` join `MathRunner` automatically.
- Run tests from `Unity_RunCommand` with `TestRunnerApi` + `ExecutionSettings { runSynchronously = true }` filtered to assembly `MathRunner.Tests.EditMode`, collecting results via `ICallbacks`. Or in the Editor: Window > General > Test Runner > EditMode > Run All.
- Movement sandbox: `Assets/_Project/Scenes/Sandbox_Movement.unity` (`SampleScene` is the untouched template scene)
- Unity MCP notes: `Unity_RunCommand` blocks `System.Reflection` (use `SendMessage` / public APIs). Entering/exiting Play mode and script changes cause a domain reload, so the tools are unavailable for a few seconds. Play mode runs much slower than real time while the Editor is unfocused, so drive it deterministically instead of sampling in real time:
  - Start Play mode paused: set `EditorApplication.isPaused = true` *before* `isPlaying = true`.
  - Set `Time.captureDeltaTime = 0.02f`, then each `EditorApplication.Step()` runs exactly one frame synchronously inside the command. Simulate input with `InputReader.NotifyJumpPressed/Released()`.
  - Reset `captureDeltaTime = 0` and `isPaused = false` when done.
  - Wrap harness code in try/catch: an uncaught exception makes the tool revert, which exits Play mode.
  - `GameObject.Find` can't see inactive objects (e.g. the hidden question panel); use `transform.Find` from an active parent.
  - The command wrapper has an `Image` namespace; alias `using UIImage = UnityEngine.UI.Image;`.
  - Overlay canvases don't appear in `Unity_Camera_Capture`; switch the canvas to Screen Space - Camera in Play mode before capturing.
- Project: Unity 6000.3.11f1, URP, git repo at this folder (`MATH_RUNNERV1`)
- Layers: 6 Ground, 7 Player, 8 Obstacle, 9 AnswerPlatform
- Decisions made: none yet (see GAME_DESIGN.md §6)
