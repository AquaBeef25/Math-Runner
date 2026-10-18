# CLAUDE.md — Math Runner

Educational 2D auto-runner platformer in **Unity (C#)**. The player jumps onto the platform showing the correct answer to a math question. Targets PC (keyboard) and Android/iOS (touch).

**Full design and roadmap: [GAME_DESIGN.md](GAME_DESIGN.md). Read it before starting any feature.**
**Improvement stages A–F (approved 2026-10-18, supersede roadmap Phases 5–8): [IMPROVEMENT_PLAN.md](IMPROVEMENT_PLAN.md).**

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
- Distance from a Question Zone's trigger to its decision line = `runSpeed × timeLimit`, so the countdown and the physical layout always match. The platforms start `landing air time × runSpeed` after the decision line (`JumpMath`, ~0.65 s).
- Answering is tap-to-answer: `AnswerButtons` / keys 1–3 → `InputReader.NotifyAnswerChosen` → `GameEvents.AnswerChosen` → the active `QuestionZone` accepts only the first pick (`AnswerLocked`), makes that platform solid and calls `PlayerController.QueueAutoJump`. While a question is open the player is on autopilot (manual jumps ignored), so never put hazards inside a Question Zone. Tests pick answers with `LevelBot` (`AnswerPlanFor`, `PickFor`, `PickDelaySeconds`).
- One `Game` scene loads a `LevelConfig`. Levels are built from chunk prefabs.

## Useful Claude Skills (Unity plugin)
`unity:new-unity-project`, `unity:2d-pixel-perfect`, `unity:ui-ugui` / `unity:ui-uitk`, `unity:optimize-text-mesh-pro`, `unity:localization`, `unity:sprite-editor`, `unity:tilemap-palette-create`

## Current Progress
<!-- Update this as phases finish -->
- **IMPROVEMENT_PLAN Stage A done** (incl. A6 tap-to-answer). **Stage B in progress (Kenney art chosen):** B3 juice + B5 post-processing done; B1 art / B2 parallax / B4 audio wait for the Kenney packs in `Assets/_Project/Art/ThirdParty/Kenney/`.
- **Project moved 2026-10-18** from `Documents\GameProgramming\Math_Runner` (backed up by Google Drive, whose sync kept locking `.meta`/scene files so Unity couldn't save them) to `C:\Dev\Math_Runner\MATH_RUNNERV1`. Don't put the project back under a Drive/OneDrive-synced folder. The old copy in Documents is kept until the user confirms this one works.
- **B1 art swap: done.** Kenney packs are in `Assets/_Project/Art/ThirdParty/Kenney/` (NewPlatformerPack 1.1, UIPack 2.0, InterfaceSounds 1.0; SVG `Vector` folders deleted on purpose). What was built:
  - `TiledStrip` (left cap + tiled middle + right cap SpriteRenderers, `SetSize(w,h)`, `Color`), `SpriteFlipbook` (flag wave), `PlayerAnimator` (+ pure `PickPose`, EditMode `PlayerAnimatorTests`; poses Idle/Run/Jump/Fall/Hurt, squash on `Landed`, stretch on `Jumped`, run frames advance with distance).
  - `GroundSegment` now sizes its **collider** (no transform scale) and two `TiledStrip`s: `_top` (grass row, 1 unit) and `_fill` (dirt). `AnswerPlatform._body` is now a `TiledStrip` (top aligned to the collider top, `_bodyHeight` 1); idle colour white so the gray stone tints cleanly.
  - Import settings applied (Single sprite, uncompressed, no mips, Clamp): characters `character_green_{idle,walk_a,walk_b}` PPU 100, pivot (0.5, 0) = feet; tiles `terrain_grass_block_{top_left,top_right,left,center,right}`, `terrain_stone_cloud_{left,middle,right}`, `spikes` PPU 64, pivot centre, **Full Rect** mesh (needed for Tiled draw mode); `Tiles/Double/flag_green_{a,b}` PPU 64 (2 units), pivot (0.5, 0).
  - Also applied 2026-10-18: `character_green_{jump,hit}` (PPU 100, pivot feet) and `terrain_grass_block_top` (PPU 64, Full Rect).
  - **Prefabs rebuilt 2026-10-18** (EditMode 238/238, PlayMode 30/30): `GroundSegment` (Top/Fill TiledStrips, root SpriteRenderer removed), `QuestionZone` platform `Body` → stone-cloud TiledStrip, `Spike` → `spikes` sprite, `FinishFlag` → one `flag_green_a/b` SpriteFlipbook at (0.7, 0). Building Tiled SpriteRenderers in prefab contents logs harmless "SendMessage cannot be called during Awake… OnSpriteTilingPropertyChange" warnings (so `Unity_RunCommand` reports failure); read the prefab back to confirm.
  - `Unity_SceneView_Capture2DScene` treats `worldX/worldY` as the **bottom-left** corner, not the centre. For prefab art checks, instantiate prefabs far away (e.g. x = 1000) in Edit mode, capture, then delete — no Play mode needed.
  - **Player art done 2026-10-18** in Game, Sandbox_Level, Sandbox_QuestionZone, Sandbox_Movement: child `Visual` (SpriteRenderer, y −0.5 = collider bottom, order 10), root SpriteRenderer disabled (kept), `PlayerAnimator` (idle, run walk_a/walk_b, jump, fall = jump, hurt = hit), `PlayerHealth._sprite` → Visual (Sandbox_QuestionZone has no PlayerHealth). EditMode 238/238, `LevelRunTests` 5/5. Opening any scene that contains TiledStrips logs the same harmless OnSpriteTilingPropertyChange warnings.
  - **Sandbox_Level ground art done 2026-10-18:** `Ground_0–3` + `EndWall` are scaled squares (scale = size, 1×1 collider) and were left untouched except their SpriteRenderer disabled; the art lives in unscaled `Level/GroundArt/<name>_Art` (Top grass + Fill dirt TiledStrips sized from the collider bounds). If that layout changes, rebuild GroundArt to match.
  - **B1 done 2026-10-18:** credits in `Assets/_Project/Art/ThirdParty/CREDITS.md` (all Kenney packs are CC0). UIPack is imported but not yet used on menus/HUD.
  - **Next:** B2 parallax, then B4 audio (InterfaceSounds). Keep PlayMode to one filtered run per task (the user's request: it is token-heavy).
  - `Assets/TextMesh Pro/.../LiberationSans SDF - Fallback.asset` shows as modified: TMP's dynamic atlas changes at runtime; revert it with `git checkout` before committing.
- Feedback lives in `FeedbackFX` (Game scene object `FeedbackFX`, with the `CinemachineImpulseSource` and the Confetti/Dust particle systems; `CM Follow` has the `CinemachineImpulseListener`; HUD has `Flash` and the `TopBar/ScorePopup` template). Gameplay must never depend on it. `PlayerController.Jumped/Landed` exist for feedback only.
- Post-processing: Main Camera `renderPostProcessing` on + `Global Volume` using `Assets/_Project/Settings/PostFX_Game.asset`. `Unity_Camera_Capture` needs the camera's **GameObject** instance ID (`Camera.main.gameObject.GetInstanceID()`), not the Camera component's, otherwise it fails with "Failed to render scene preview". Alternative: render `Camera.main` into a RenderTexture inside `Unity_RunCommand`, save a PNG to `Temp/` and Read it.
- A full PlayMode run once failed with levels stuck in `Paused`: a stray Esc/P keypress reached the focused Game view. Don't type in Unity while PlayMode tests run.
- Still to check by hand (Unity can't simulate input while unfocused): keys 1/2/3 and clicking the answer buttons in the Game view, and that the buttons don't cover the runner or platforms on a phone-shaped Game view.
- Scene flow: `MainMenu` (build index 0) → `LevelSelect` → `Game`, via `SceneFlow`. Sandboxes stay in Build Settings for tests. `GameManager.Restart` reloads by build index, so any playable scene must be in Build Settings.
- Progress: `SaveSystem` writes JSON to `Application.persistentDataPath/save.json`. Tests must call `SaveSystem.UseFile(tempPath)` in SetUp and `UseFile(null)` in TearDown so they never touch the real save.
- Every scene's EventSystem uses `InputSystemUIInputModule` with `Assets/InputSystem_Actions.inputactions` (UI map). Don't use `AssignDefaultActions()` in build scripts: its temporary asset isn't saved with the scene.
- `Unity_RunCommand` reports a command as failed if it logs a warning, even when the work succeeded (e.g. running tests that expect warnings).
- Playable gray-box sandbox: `Assets/_Project/Scenes/Sandbox_Level.unity` (hand-built, Level 5 questions; `LevelRunTests` expect its exact layout and the 2300 score).
- Still needs a manual check: Esc/P pause, and that clicking the pause button doesn't also jump
- Question sandbox: `Assets/_Project/Scenes/Sandbox_QuestionZone.unity` (4 zones: untimed, 4 s, 3 s, 2 s; director on Level 5)
- Player ground mask must include both `Ground` and `AnswerPlatform` layers
- Phase 1 movement still pending my feel-test with a real keyboard
- Assemblies: `MathRunner.Math` (Scripts/Math, pure math, no MonoBehaviours), `MathRunner` (all other Scripts, references Math + Input System), `MathRunner.Tests.EditMode` (Tests/EditMode). New script folders under `Scripts/` join `MathRunner` automatically.
- Run tests from `Unity_RunCommand` with `TestRunnerApi` + `ExecutionSettings { runSynchronously = true }` filtered to assembly `MathRunner.Tests.EditMode`, collecting results via `ICallbacks`. Or in the Editor: Window > General > Test Runner > EditMode > Run All.
- PlayMode tests (`Tests/PlayMode`, assembly `MathRunner.Tests.PlayMode`): start them with `TestRunnerApi.Execute` (not synchronous; it enters Play mode and reloads the domain), then wait for `Logs/TestResults-PlayMode.txt`, written by `Assets/_Project/Editor/TestResultsLog.cs`. The run takes ~15 s and isn't slowed down when the Editor is unfocused. `LevelBot` plays any loaded level (answer plan per question, which hazards to jump); reuse it for new level tests.
- Levels are data: `ScriptableObjects/Levels/Level01–10.asset` (`LevelConfig`) listed in `Resources/LevelCatalog.asset`. `Scenes/Game.unity` builds the level in `LevelSelection.Current` (falls back to catalog level 1) via `LevelBuilder` + pure `LevelPlanner` (spacing in seconds × run speed). `AllLevelsTests` proves every level is finishable; rerun it after any pacing, physics or layout change.
- Unity pitfall seen here: after writing several new scripts at once, some were silently left out of compilation (check `CompilationPipeline.GetAssemblies()` source lists). Moving them to a new path fixed it. Scene fields typed as a ScriptableObject then refused assignment through `SerializedProperty` (`ApplyModifiedProperties` returned false); setting the field through a public method + `EditorUtility.SetDirty` worked. Always read back saved references.
- Unity's Lost Crypt sample was imported 2026-10-18 and **deleted the same day** (not used; Kenney art chosen instead). Its import had overwritten ProjectSettings once; ours were restored from git. If layers 6–9 ever stop being Ground/Player/Obstacle/AnswerPlatform, check for a settings overwrite after any sample/asset import.
- Movement sandbox: `Assets/_Project/Scenes/Sandbox_Movement.unity` (`SampleScene` is the untouched template scene)
- Unity MCP notes: `Unity_RunCommand` blocks `System.Reflection` (use `SendMessage` / public APIs). Entering/exiting Play mode and script changes cause a domain reload, so the tools are unavailable for a few seconds. Play mode runs much slower than real time while the Editor is unfocused, so drive it deterministically instead of sampling in real time:
  - Start Play mode paused: set `EditorApplication.isPaused = true` *before* `isPlaying = true`.
  - Set `Time.captureDeltaTime = 0.02f`, then each `EditorApplication.Step()` runs exactly one frame synchronously inside the command. Simulate input with `InputReader.NotifyJumpPressed/Released()`.
  - Reset `captureDeltaTime = 0` and `isPaused = false` when done.
  - Wrap harness code in try/catch: an uncaught exception makes the tool revert, which exits Play mode.
  - `GameObject.Find` can't see inactive objects (e.g. the hidden question panel); use `transform.Find` from an active parent.
  - The command wrapper has an `Image` namespace; alias `using UIImage = UnityEngine.UI.Image;`.
  - Overlay canvases don't appear in `Unity_Camera_Capture`; switch the canvas to Screen Space - Camera in Play mode before capturing (world sprites may then draw over the UI in the capture only).
  - Simulated keyboard/mouse events (`InputSystem.QueueStateEvent`) are ignored while the Game view is unfocused; call `InputReader.Notify*`, `GameManager` methods, or `Button.onClick.Invoke()` instead.
  - `AssetDatabase.DeleteAsset` is blocked (needs a dialog); delete tracked files with `git rm` and refresh.
- UI rule: any HUD graphic that isn't a button must have `raycastTarget = false`, otherwise taps on it won't jump.
- Project: Unity 6000.3.11f1, URP, git repo at this folder (`MATH_RUNNERV1`)
- Layers: 6 Ground, 7 Player, 8 Obstacle, 9 AnswerPlatform
- Decisions made: none yet (see GAME_DESIGN.md §6)
