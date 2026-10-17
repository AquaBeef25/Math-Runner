# Math Runner — "Massive Improvements" Plan

Approved 2026-10-18. Work stage by stage (A → F); each stage ends with a playable build and one commit.
**Status:** Stage A done (2026-10-18), including A6 tap-to-answer and C4 star ratings. Next: Stage B (art source still to be
decided: Kenney packs or the Lost Crypt art already in the project).
- A1 PlayMode suite · A2 LevelConfig/LevelPlanner/LevelBuilder + `Game` scene + 10 level assets · A3 all 10 levels finishable ·
  A4 `SaveSystem` (JSON, best score/stars, unlocks) · A5 `MainMenu` → `LevelSelect` → `Game`, Next Level / Levels buttons ·
  A6 tap-to-answer (see below) · C4 `StarRating` (shown as text "2 of 3 stars" until star icons arrive in Stage B).
- Tests: 227 EditMode, 28 PlayMode, all passing.

**A6 — Tap-to-answer (added 2026-10-18 at the user's request).** The player picks an answer with one of 3 on-screen buttons
or the 1/2/3 keys instead of timing a jump; the runner then jumps onto that platform by itself (autopilot inside Question
Zones). No pick by the decision line = missed. The countdown now measures thinking speed, so the speed bonus is
`50 × time left` again. Details: GAME_DESIGN.md §2.1–2.3.

## Context
Phases 0–4 are done and verified: auto-run movement, math engine (75 tests), Question Zones, score/lives/HUD/overlays, and one
gray-box level (`Scenes/Sandbox_Level`). Everything works but looks like gray boxes, there is only one hand-built level, and
the game teaches only by showing the right answer.

The user wants big improvements in **all four areas** (feel & polish, learning, content & variety, mobile & accessibility),
has **no fixed deadline**, and chose **free CC0 asset packs (Kenney.nl)** for art. So this plan is a series of **stages that
each end in a better, playable, committed game**. Stopping after any stage still leaves a finished-feeling project.
Instructor-approved Section 1 of GAME_DESIGN.md stays untouched; everything here is additive.

Project root: `C:\Users\marvi\Documents\GameProgramming\Math_Runner\MATH_RUNNERV1`. Before executing, move the session into
that folder so CLAUDE.md loads again; it's currently one level up.

**Minimum to look impressive:** Stages A + B + C1–C4. Everything after that is upside.

---

## Stage A — Foundation (makes every later change safe and cheap) · size L
Big changes risk silently breaking what already works, so the safety net comes first.

- **A1. PlayMode test suite.** Move the frame-stepping bot I've been running through `Unity_RunCommand` into
  `Assets/_Project/Tests/PlayMode/` (new asmdef that references `MathRunner`). It drives `InputReader.NotifyJumpPressed/Released()`
  with `Time.captureDeltaTime = 0.02f`. Tests:
  - a win run that checks the exact score (2300 on Sandbox_Level)
  - a lose run (3→2→1→0 lives, then Game Over)
  - pause freezes time
  - correct / wrong / missed zone outcomes
- **A2. Data-driven levels (roadmap Phase 5).**
  - **`LevelConfig` ScriptableObject** (sketch already in GAME_DESIGN §3.4):
    - question rules, seeded from `LevelCurriculum.ForLevel(n)`
    - run speed, time limit, timed ratio, question count
    - chunk list and world/theme
  - **`LevelBuilder`** lays chunk prefabs end to end. It configures each zone with the existing
    `QuestionZone.Configure(runSpeed, timeLimit)` and auto-places checkpoints and the kill zone.
  - **Replace the hard-coded `_level` fields** in `QuestionDirector` and `ScoreSystem` (which reads `QuestionDirector.Level`)
    with the LevelConfig.
  - **Platform width scales with speed:** `width = runSpeed × 0.5 s`. This keeps the timing window the same at 8 u/s;
    today's fixed 3-unit platforms would shrink it to 0.375 s.
- **A3. Every level is beatable test.** For each of the 10 configs, build the level and run the bot answering correctly; it
  must reach the flag. This guards the known risk that jump distance scales with run speed (GAME_DESIGN Phase 1 note).
- **A4. SaveSystem.** JSON at `Application.persistentDataPath` via `JsonUtility`. Stores unlocked levels, best score, stars,
  settings, and learning stats. The plain data class gets EditMode tests.
- **A5. Scene flow.**
  - Scenes: `MainMenu` → `LevelSelect` → `Game` (one scene that loads the chosen LevelConfig).
  - Overlays gain "Next Level" and "Main Menu" (`OverlayController` already has the button pattern).
  - Build Settings updated (`GameManager.Restart` reloads by build index).

## Stage B — Look & feel with Kenney art · size L
The biggest jump in how the game looks and feels per hour of work. Almost entirely event listeners, with no gameplay changes,
because `GameEvents` already raises everything needed (`QuestionShown/Resolved`, `PlayerHit`, `ScoreChanged`, `LivesChanged`,
`GameStateChanged`).

- **B1. Art swap.**
  - **Getting the packs:** you download the chosen CC0 packs from kenney.nl into
    `Assets/_Project/Art/ThirdParty/Kenney/`. I'll suggest specific packs then; downloads need your OK.
  - **Import and swap:** I set import settings and a Sprite Atlas, then swap the gray boxes for real sprites (ground as tiled
    sprites, answer platforms as signboards, spikes, flag).
  - **Character animation:** Run / Jump / Fall / Hurt animations, driven by a new `PlayerAnimator` that reads
    `PlayerController.IsGrounded` and velocity. No logic goes into the controller.
  - **Credits:** a credits file listing the packs.
- **B2. Parallax background** (3 layers, `ParallaxLayer` component) per world theme.
- **B3. Juice (`FeedbackFX` listener):**
  - correct answer: confetti, a floating "+575" score popup, and a small Cinemachine impulse
  - wrong answer: screen shake and a red edge flash
  - landing dust, plus squash and stretch on jumps
  - question panel pop-in, and the timer pulsing in its last second
  - a tiny in-house tween helper in `Scripts/UI/` (no new package)
- **B4. Audio.** `AudioManager` plus an Audio Mixer with Music and SFX groups (`unity:audio-setup-mixers` skill), using CC0
  sounds:
  - jump, land, correct (pitch rises with the streak), wrong, crumble, button click, countdown tick
  - a looping music track per world
- **B5. Post-processing.** Subtle bloom and vignette via URP (`unity:urp-postprocessing` skill).

## Stage C — Learning features (the educational differentiator) · size M
- **C1. Step-by-step solution after a mistake.**
  - **New `SolutionExplainer`** in `MathRunner.Math` turns a `MathProblem` into steps, e.g. `455 ÷ 5 = 91 → 91 + 7 = 98`, or
    `4 + 6 × 3 → 6 × 3 = 18 first → 4 + 18 = 22`.
  - **Shown on screen:** `QuestionPanel` displays the steps on Wrong or Missed results.
  - **Room to read:** LevelBuilder guarantees at least 3 s of run-up after a zone so players have time to read the steps.
  - **Tests:** EditMode tests check the steps against the existing `ExpressionEvaluator` (Tests/EditMode).
- **C2. Missed questions come back.** A pure `ReviewQueue`, used by `QuestionDirector`, re-asks a missed fact 2–3 questions
  later with fresh wrong choices (spaced repetition). EditMode tests.
- **C3. Mastery stats.**
  - **What's tracked:** attempts and accuracy per `QuestionType`, plus the most-missed facts. Saved by A4.
  - **Where it shows:** a Stats screen in the main menu (bars per operation, plus "Facts to practice: 7×8, 6×7…").
  - **After each level:** Level Complete adds a line like "Your × accuracy went up".
- **C4. Star rating** (1–3 stars from accuracy and lives left) on Level Complete and in Level Select.
- **C5. Practice mode.** No lives and no timer; pick an operation. Reuses the `Game` scene with a practice LevelConfig.
- **C6. Tutorial (Level 0).** `TutorialPrompt` triggers show "Tap to jump!" and "Land on the right answer!"; very easy
  questions, and no lives lost.

## Stage D — Content & variety · size L
- **D1. Ten tuned levels in 3 themed worlds:** Addition Meadow (L1–4), Times-Table Mines (L5–7), Sky Castle (L8–10). Each
  world gets its own palette, background and music. Uses the speeds and time limits from GAME_DESIGN §2.4.
- **D2. New question format: missing number**, e.g. `5 + ? = 8` and `? × 6 = 42`.
  - **Engine:** a new `QuestionType` plus a formatter in `MathProblem`.
  - **Tests:** `ExpressionEvaluator` learns `?` operands; tests run across all levels.
- **D3. New hazards as chunk prefabs:** moving saw, crumbling floor, flying bird. Each uses the existing `Obstacle` and
  `KillZone` components.
- **D4. Power-ups** (GAME_DESIGN §5 #6):
  - Shield: the next wrong answer costs no life
  - Slow-Mo: run speed ×0.6 for one question, so the countdown and the distance both stretch
  - Hint: one wrong platform disappears
  - HUD icon for the active power-up
- **D5. Boss Gates at Levels 5 and 10.** Three questions back to back with shrinking time, plus a boss visual.
- **D6 (optional, late).**
  - coins and a cosmetic skin shop
  - Endless mode with a local high-score table
  - Daily Challenge: a date-based seed, so everyone gets the same questions that day

## Stage E — Mobile & accessibility · size M
- **E1. Phone build.**
  - Android build profile, locked to landscape, and a `SafeArea` HUD component.
  - Tested on a real phone.
  - Performance pass: sprite atlas, chunk pooling, and TMP atlas trimming (`unity:optimize-text-mesh-pro` skill).
- **E2. Settings screen:**
  - music and SFX volume
  - **Relaxed timer** (×1.5 time, which puts the platforms further away)
  - **Colorblind-safe mode** (✓/✗ icons and patterns on platforms instead of only green/red)
  - larger numbers
  - vibration on wrong answers
- **E3. English / Filipino.** UI strings only, using Unity Localization (`unity:localization` skill). I draft the Filipino
  text and you check it.
- **E4. Auto-pause when the app loses focus.** `OnApplicationPause` → `GameManager.TogglePause`.

## Stage F — Final polish & presentation · size S
- **Playtesting:** run the GAME_DESIGN §7 playtest checklist with real players and tune from the results.
- **Builds:** Windows and Android, with app icon, splash screen and credits screen.
- **For the instructor:**
  - a short README
  - a table mapping each approved feature (GAME_DESIGN §1) to where it's implemented, useful for grading
  - a demo video

---

## How each stage is executed
1. Write pure logic first, with EditMode tests: `SolutionExplainer`, `ReviewQueue`, save data, new question format.
2. Build scenes and prefabs through `Unity_RunCommand` (never by hand-editing YAML), and check the Console.
3. Verify with the PlayMode tests from A1/A3, plus `Unity_Camera_Capture` screenshots for visual changes.
4. Update the GAME_DESIGN checklist and CLAUDE.md progress, then make one git commit per stage.

## Existing code to reuse
- `GameEvents` (Scripts/Core): every feedback, audio and stats feature subscribes here.
- `QuestionZone.Configure(runSpeed, timeLimit)` (Scripts/Level): ready for LevelBuilder.
- `LevelCurriculum.ForLevel(n)` (Scripts/Math): seeds the 10 LevelConfig assets.
- `QuestionSettings.Validate()`: call it from `LevelConfig.OnValidate`.
- `InputReader.NotifyJumpPressed/Released()`, and the frame-stepping bot: the core of the PlayMode tests.
- `ExpressionEvaluator` (Tests/EditMode): checks solution steps and the new question formats.
- `ScoreCalculator`, `DifficultyTracker`: unchanged; stars and stats build on `ScoreSystem` counts.

## Verification
- **EditMode** (Test Runner or the `TestRunnerApi` command): all existing 78 tests plus the new Explainer, ReviewQueue,
  SaveData and question-format tests pass.
- **PlayMode:** win, lose and pause tests pass, and the "every level is beatable" test passes for all 10 LevelConfigs.
- **Visual:** a screenshot per stage (gameplay, Level Complete with stars, Stats screen, mistake explanation).
- **Manual (things Unity can't simulate while unfocused):**
  - Esc/P pause
  - clicking the pause button doesn't also jump
  - touch input and safe area on a real Android phone
  - audio levels
