# Enemy Encounter & Focus Zone Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace answer-platform Question Zones with enemy encounters: a flying enemy fires, the world slows to 0.2×, the player answers one of four choices before the bullet lands, and a shockwave (right) or a bullet hit (wrong/too slow) follows.

**Architecture:** `GameManager` becomes the single owner of `Time.timeScale` (pause × focus factor, via the pure `WorldTime.Combine`). A new `EnemyEncounter` zone runs the sequence and talks to everything else through `GameEvents`, which keep their meaning, so score, stars, difficulty, FX and audio keep working. `LevelPlanner` stays pure and reserves a hazard-free stretch sized from `EncounterSettings` (a ScriptableObject holding every encounter tunable).

**Tech Stack:** Unity 6000.3.11f1, C#, URP 2D, Input System via `InputReader`, Cinemachine 3, Unity Test Framework (EditMode + PlayMode). Editor work via `Unity_RunCommand`.

**Spec:** `docs/superpowers/specs/2026-10-18-enemy-encounter-design.md`

## Global Constraints

- Follow CLAUDE.md: namespaces `MathRunner.<Folder>`, `[SerializeField] private`, cache components in `Awake`, tunables in ScriptableObjects, systems talk through `GameEvents`, input only through `InputReader`, never bare `Math.`.
- Don't hand-edit `.unity` / `.prefab` / `.asset` YAML; do Editor setup with `Unity_RunCommand`. Assign ScriptableObject references in scenes through a public setter + `EditorUtility.SetDirty`, then read the saved file back.
- Focus Zone world speed: **0.2×**, tunable. Answers: **4** choices, keys **1–4**. Too slow = same as wrong.
- One heart per wrong/missed answer, taken **when the bullet hits**; the bullet damages once, then is removed.
- No spikes/gaps inside an encounter stretch; the player can't jump during an encounter (autopilot).
- PlayMode tests: at most **one filtered run per task** (user's request); `AllLevelsTests` once, in the last task.
- After each task: check the Console for errors; screenshot visual changes. Revert `LiberationSans SDF - Fallback.asset` before committing. Commits end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Pausing during the Focus Zone:** unpausing must return to 0.2×, not 1× → Task 8 `PauseDuringFocusResumesSlowMotion`.
2. **A spike hit just before an encounter** (player still invincible): the bullet must still cost a heart → Task 8 `BulletHitIgnoresInvincibility`.
3. **The last heart lost to the bullet:** state is GameOver and `Time.timeScale` stays 0 even though focus ends right after → Task 8 `LastHeartLostToBulletEndsTheGameFrozen`.
4. **Restart or leaving the scene mid-focus:** the next scene must run at timeScale 1 with the default `fixedDeltaTime` → Task 8 `RestartDuringFocusRestoresNormalTime`.
5. **Key and button pressed in the same frame** (or a double tap): only the first pick counts → Task 8 `FirstPickLocksAndLaterPicksAreIgnored`.

---

### Task 1: Time-scale ownership (pause × focus)

**Files:**
- Create: `Assets/_Project/Scripts/Core/WorldTime.cs`
- Modify: `Assets/_Project/Scripts/Core/GameEvents.cs` (new events + `HitCause.Bullet`)
- Modify: `Assets/_Project/Scripts/Core/GameManager.cs:27-64`
- Test: `Assets/_Project/Tests/EditMode/WorldTimeTests.cs`

**Interfaces:**
- Produces: `static class WorldTime { float FocusFactor { get; internal set; } (default 1); static float Combine(bool paused, float focusFactor); static float RealDeltaTime { get; } }` — `RealDeltaTime = FocusFactor > 0 ? Time.deltaTime / FocusFactor : 0` (0 while paused because `deltaTime` is 0).
- Produces in `GameEvents`: `event Action<float> FocusStarted` + `RaiseFocusStarted(float factor)`, `event Action FocusEnded` + `RaiseFocusEnded()`, `event Action<Vector3> EnemyDefeated` + `RaiseEnemyDefeated(Vector3)`; `HitCause.Bullet`.
- Produces in `GameManager`: subscribes `FocusStarted/FocusEnded`; private `ApplyTimeScale()` sets `Time.timeScale = WorldTime.Combine(State != GameState.Playing, focus)` and `Time.fixedDeltaTime = _baseFixedDelta * max(focus, 0.01f)`. `SetState` calls it instead of writing `timeScale`. `Restart()` and `OnDestroy()` reset focus to 1, `timeScale` to 1 and `fixedDeltaTime` to its base value.

- [ ] **Step 1: Write the failing tests** `WorldTimeTests`: `Combine(true, 0.2f) == 0`, `Combine(true, 1f) == 0`, `Combine(false, 0.2f) == 0.2f`, `Combine(false, 1f) == 1f`.
- [ ] **Step 2: Run EditMode tests** — expect a compile failure (`WorldTime` missing).
- [ ] **Step 3: Implement** `WorldTime`, the `GameEvents` additions and the `GameManager` changes above.
- [ ] **Step 4: Run all EditMode tests** — expect all pass (previous total + 4).
- [ ] **Step 5: Commit** — `Encounter 1: time-scale ownership (pause x focus)`.

### Task 2: Four answer buttons and keys 1–4

**Files:**
- Modify: `Assets/_Project/Scripts/Input/InputReader.cs:13` (`AnswerKeyCount = 4`, header comment "1/2/3/4")
- Modify: `Assets/_Project/Scripts/UI/AnswerButtons.cs` (arrays sized 4; a button shows only if `i < question.Choices.Count`; comment no longer mentions platforms)
- Scenes (via `Unity_RunCommand`): `Game`, `Sandbox_Level`, `Sandbox_QuestionZone` — duplicate the third answer button as `Answer4`, lay out the 4 buttons in equal widths across the existing row, wire `_buttons[3]` / `_labels[3]`.
- Test: `Assets/_Project/Tests/EditMode/QuestionGeneratorTests.cs`

**Interfaces:**
- Produces: `AnswerButtons` handles 3 or 4 choices (hides unused buttons), so it works before and after the switch-over.

- [ ] **Step 1: Write the failing test** `EveryLevelGivesFourDistinctNonNegativeChoices`: for each `LevelCatalog.Load().Levels` config, `new QuestionGenerator(new System.Random(1), 4)` generates 300 questions at each `DifficultyTier`; assert `Choices.Count == 4`, all distinct, all `>= 0`, `Choices[CorrectIndex] == Answer`.
- [ ] **Step 2: Run EditMode tests** — if it passes already, keep it as a guard; if it fails, fix `DistractorGenerator` so small answers still get 3 distinct non-negative distractors.
- [ ] **Step 3: Implement** the `InputReader` / `AnswerButtons` changes, then add `Answer4` in the 3 scenes. Read back that `_buttons[3]` and `_labels[3]` are set in each saved scene.
- [ ] **Step 4: Run all EditMode tests** — all pass. Console has no errors. Screenshot the HUD row in a phone-shaped (e.g. 1080×2340 landscape) Game view capture.
- [ ] **Step 5: Commit** — `Encounter 2: four answer buttons, keys 1-4`.

### Task 3: Encounter settings and planner spacing

**Files:**
- Create: `Assets/_Project/Scripts/Enemies/EncounterSettings.cs` (namespace `MathRunner.Enemies`)
- Create asset: `Assets/_Project/ScriptableObjects/Encounters/EncounterSettings.asset`
- Modify: `Assets/_Project/Scripts/Level/Layout/LevelPlanner.cs` (add `FocusScale` to `LevelPlanSettings`)
- Test: `Assets/_Project/Tests/EditMode/LevelPlannerTests.cs`

**Interfaces:**
- Produces: `EncounterSettings : ScriptableObject` with read-only properties and defaults: `FocusScale 0.2`, `FlyInSeconds 1.2`, `WindUpSeconds 0.4`, `ExitBufferSeconds 1.0`, `SpanSeconds => FlyInSeconds + WindUpSeconds + ExitBufferSeconds`, `HoverOffset (7, 4)`, `BobAmplitude 0.25`, `BobHz 1.5`, `RetreatSpeed 12`, `BulletSpinDegreesPerSecond 360`, `ShockwaveRadius 9`, `ShockwaveSeconds 0.45`.
- Produces: `LevelPlanSettings.FocusScale` (float). Encounter stretch: `ZoneLength(i, s) = (Zones[i].Lead * s.FocusScale + s.ZoneSpanSeconds) * s.RunSpeed`, and `Create` advances `x` by the same amount. `From(LevelConfig config, float zoneSpanSeconds, float focusScale)`.
- Until Task 8, `LevelBuilder` calls `From(config, airTime + _zonePrefab.PlatformSpanSeconds, 1f)` so the old zones keep their layout.

- [ ] **Step 1: Update the test helper** `Settings(...)` to set `FocusScale = 0.2f`, and add the failing test `EncounterStretchUsesSlowedFocusTime`: with `limit 4, speed 6, ZoneSpanSeconds 2, ratio 1, gapChance 0`, `plan.ZoneLength(0, s) == (4 * 0.2 + 2) * 6` (16.8), and `Zones[1].X - Zones[0].X == ZoneLength(0, s) + (s.AfterZoneSeconds + (s.HazardsBetweenQuestions + 1) * s.HazardSpacingSeconds) * 6`.
- [ ] **Step 2: Run EditMode tests** — the new test fails.
- [ ] **Step 3: Implement** the settings field, `ZoneLength`, `Create` and `From`; create `EncounterSettings.cs` and the asset (via `Unity_RunCommand`, `AssetDatabase.CreateAsset`).
- [ ] **Step 4: Run all EditMode tests** — all pass, including the existing `EveryZoneSitsOnSolidGroundWithNoSpikes`.
- [ ] **Step 5: Commit** — `Encounter 3: encounter settings and planner spacing`.

### Task 4: Enemy bullet

**Files:**
- Create: `Assets/_Project/Scripts/Enemies/EnemyBullet.cs`
- Create prefab: `Assets/_Project/Prefabs/Enemies/EnemyBullet.prefab` (Kenney `Tiles/Default/fireball.png`, PPU 64, sorting order 12; no collider)
- Test: `Assets/_Project/Tests/EditMode/EnemyBulletTests.cs`

**Interfaces:**
- Produces: `EnemyBullet : MonoBehaviour` — `void Launch(Transform target, float flightSeconds)` (flight in **scaled** seconds), `float RemainingFraction { get; }` (1 at launch → 0 at impact), `event Action Arrived` (fires once, then the bullet destroys itself), `void Fizzle()` (removes it without firing `Arrived`), `static Vector2 Step(Vector2 position, Vector2 target, float remainingSeconds, float deltaSeconds)`. Spins by `EncounterSettings.BulletSpinDegreesPerSecond`; moves in `Update` with `Time.deltaTime`, aiming at the target's centre each frame.

- [ ] **Step 1: Write the failing tests** for `Step`: `deltaSeconds >= remainingSeconds` returns `target` exactly; `deltaSeconds == remainingSeconds / 2` with a still target returns the midpoint; the result is never farther from `position` than `target` is (no overshoot) for 50 random inputs.
- [ ] **Step 2: Run EditMode tests** — they fail (type missing).
- [ ] **Step 3: Implement** `EnemyBullet` and make the prefab (via `Unity_RunCommand`; set the fireball import to Single / PPU 64 / Full Rect if not already).
- [ ] **Step 4: Run all EditMode tests** — all pass.
- [ ] **Step 5: Commit** — `Encounter 4: enemy bullet`.

### Task 5: Flying enemy

**Files:**
- Create: `Assets/_Project/Scripts/Enemies/FlyingEnemy.cs`
- Create prefab: `Assets/_Project/Prefabs/Enemies/FlyingEnemy.prefab` — child `Visual` (SpriteRenderer, a `character_purple_*` sprite, `flipX` so it faces left, order 11, imported PPU 100 like the player), child `Muzzle` (empty, at its hand), child `JetFlame` (small orange particle system under its feet).
- Test: `Assets/_Project/Tests/EditMode/FlyingEnemyTests.cs`

**Interfaces:**
- Consumes: `EncounterSettings` (Task 3).
- Produces: `FlyingEnemy : MonoBehaviour` — `void Appear(Transform player, EncounterSettings settings)` (flies in from off-screen top-right to the hover point over `FlyInSeconds`), `void WindUp()` (shakes for `WindUpSeconds`), `Vector3 MuzzlePosition { get; }`, `void Retreat()` (flies off top-right at `RetreatSpeed`, destroys itself off-screen), `void Defeat()` (puff particle + destroy). `static Vector2 HoverPoint(Vector2 player, Vector2 offset, float time, float amplitude, float hz)` = player + offset + (0, amplitude·sin(2π·hz·time)). All motion on scaled time, so it slows in the Focus Zone.

- [ ] **Step 1: Write the failing tests** for `HoverPoint`: at `time 0` it equals `player + offset`; over a sampled second its y stays within `±amplitude` of `player.y + offset.y`; x is always `player.x + offset.x`.
- [ ] **Step 2: Run EditMode tests** — they fail.
- [ ] **Step 3: Implement** `FlyingEnemy` and the prefab (via `Unity_RunCommand`). Edit-mode capture of the prefab placed at x = 1000 for a screenshot.
- [ ] **Step 4: Run all EditMode tests** — all pass. Console clean.
- [ ] **Step 5: Commit** — `Encounter 5: flying enemy`.

### Task 6: Shockwave

**Files:**
- Create: `Assets/_Project/Scripts/Enemies/Shockwave.cs`
- Create prefab: `Assets/_Project/Prefabs/Enemies/Shockwave.prefab` (a `LineRenderer` circle, 64 segments, white→gold gradient, order 13)
- Test: `Assets/_Project/Tests/EditMode/ShockwaveTests.cs`

**Interfaces:**
- Produces: `Shockwave : MonoBehaviour` — `void Play(Vector3 centre, float radius, float seconds)` (expands from 0 to `radius` with `Ease.OutCubic`, line width and alpha fade to 0, then destroys itself); `static float RadiusAt(float t01, float radius)`.

- [ ] **Step 1: Write the failing tests** for `RadiusAt`: 0 at t = 0, `radius` at t = 1, strictly increasing over 20 samples, and clamped outside [0, 1].
- [ ] **Step 2: Run EditMode tests** — they fail.
- [ ] **Step 3: Implement** `Shockwave` (reuse `MathRunner.UI.Ease`) and the prefab.
- [ ] **Step 4: Run all EditMode tests** — all pass.
- [ ] **Step 5: Commit** — `Encounter 6: shockwave`.

### Task 7: Player bullet hit

**Files:**
- Modify: `Assets/_Project/Scripts/Player/PlayerHealth.cs`
- Test: covered by Task 8's PlayMode tests (no pure logic to test here)

**Interfaces:**
- Produces: `PlayerHealth.HitByBullet()` — ignores `IsInvincible`, sets the invincible window, raises `GameEvents.RaisePlayerHit(HitCause.Bullet)`, does **not** respawn.

- [ ] **Step 1: Implement** `HitByBullet`.
- [ ] **Step 2: Run all EditMode tests** — all pass (compile check).
- [ ] **Step 3: Commit** — `Encounter 7: bullet hit on the player`.

### Task 8: Enemy encounter and switch-over (every level)

One task on purpose: the 4-choice questions, bullet-based life loss and removal of the platforms only make sense together, so no commit leaves the Game scene half-converted.

**Files:**
- Create: `Assets/_Project/Scripts/Enemies/EnemyEncounter.cs`
- Create prefab: `Assets/_Project/Prefabs/Chunks/EnemyEncounter.prefab` (trigger `BoxCollider2D` size (0.5, 10), offset (0, 5); references the `FlyingEnemy`, `EnemyBullet` and `Shockwave` prefabs and `EncounterSettings`)
- Modify: `Assets/_Project/Scripts/Systems/LivesSystem.cs` (drop the `QuestionResolved` life loss; `PlayerHit` covers bullets)
- Modify: `Assets/_Project/Scripts/Systems/QuestionDirector.cs` (`[SerializeField, Range(2, 4)] private int _choiceCount = 4;` passed to `QuestionGenerator`)
- Modify: `Assets/_Project/Scripts/Level/Layout/LevelBuilder.cs` (`EnemyEncounter _encounterPrefab`, `EncounterSettings _encounter` with a public setter; `From(config, _encounter.SpanSeconds, _encounter.FocusScale)`; `Configure(_director, zonePlan.Lead, zonePlan.Timed)`)
- Modify: `Assets/_Project/Scripts/UI/QuestionPanel.cs`, `Assets/_Project/Scripts/Audio/AudioManager.cs` (countdowns use `WorldTime.RealDeltaTime`)
- Modify: `Assets/_Project/Tests/PlayMode/LevelBot.cs` (pick delay counted with `WorldTime.RealDeltaTime`; wrong pick stays `(CorrectIndex + 1) % count`)
- Modify: `Assets/_Project/Tests/PlayMode/LevelRunTests.cs` (losing run expects `Hits == { Bullet, Fall, Obstacle }`; missed run expects one `Bullet` hit; keep `2300` if it still holds, else recompute it from the bonuses and update the comment)
- Scenes: `Game` (LevelBuilder → encounter prefab + settings); `Sandbox_Level` and `Sandbox_QuestionZone` (replace each `QuestionZone` instance with an `EnemyEncounter` at the same x; flight 6/4/3/2 s, speed bonus off/on/on/on; Sandbox_QuestionZone's player also gets a `PlayerHealth` whose `_sprite` is its `Visual`)
- Delete (`git rm`): `Scripts/Level/QuestionZone.cs`, `Scripts/Level/AnswerPlatform.cs`, `Prefabs/Chunks/QuestionZone.prefab`, `Tests/PlayMode/AnswerChoiceTests.cs` (+ `.meta`); drop `AirTimeToAnswerPlatformMatchesTheMeasuredJump` if nothing uses that air time any more.
- Test: `Assets/_Project/Tests/PlayMode/EncounterTests.cs` (scene `Sandbox_Level`, `Time.captureDeltaTime = 0.02f`, picks via `InputReader.NotifyAnswerChosen`, real time measured by summing `Time.unscaledDeltaTime`)

**Interfaces:**
- Consumes: `WorldTime` + focus events (Task 1); `EncounterSettings` (3); `EnemyBullet` (4); `FlyingEnemy` (5); `Shockwave` (6); `PlayerHealth.HitByBullet` (7); `QuestionDirector.NextQuestion()`.
- Produces: `EnemyEncounter : MonoBehaviour` — `void Configure(QuestionDirector director, float flightRealSeconds, bool speedBonus)`; `bool IsFocusActive { get; }`; `FlyingEnemy Enemy { get; }`; `EnemyBullet Bullet { get; }`. Sequence: player enters → `BeginAutopilot` → enemy `Appear` → after `FlyInSeconds` `WindUp` → after `WindUpSeconds` spawn the bullet at `MuzzlePosition`, `Launch(player, flightRealSeconds * FocusScale)`, `RaiseFocusStarted(FocusScale)`, subscribe `AnswerChosen`, `RaiseQuestionShown(question, flightRealSeconds)`.
  - **First pick only:** `RaiseAnswerLocked(index)`, fraction = `speedBonus ? Bullet.RemainingFraction : 0`, `RaiseFocusEnded()`. Correct → `Shockwave.Play`, `Enemy.Defeat()`, `Bullet.Fizzle()`, `RaiseEnemyDefeated`, resolve Correct. Wrong → resolve Wrong, then on `Bullet.Arrived`: `HitByBullet()`, `Enemy.Retreat()`.
  - **No pick:** on `Bullet.Arrived`: `HitByBullet()`, `RaiseFocusEnded()`, resolve Missed, `Enemy.Retreat()`.
  - Resolve = unsubscribe, `EndAutopilot()`, `RaiseQuestionResolved(new QuestionResult(question, outcome, index, flightRealSeconds, fraction))`.

- [ ] **Step 1: Write the failing PlayMode tests** in `EncounterTests`:
  - `CorrectPickDestroysEnemyAndBullet`: outcome Correct; no `HitCause.Bullet`; lives unchanged; enemy and bullet destroyed; `Time.timeScale == 1` right after.
  - `WrongPickLetsTheBulletHitOnce`: outcome Wrong; exactly one `HitCause.Bullet`; lives −1 exactly once; bullet gone; enemy destroyed off-screen within 3 s.
  - `NoPickIsMissedWhenTheBulletLands`: the 4 s encounter, no pick → outcome Missed, one Bullet hit, lives −1, real seconds from `QuestionShown` to the hit = 4 ± 0.04.
  - `TimeSlowsDuringFocus`: between `FocusStarted` and the pick, `Time.timeScale == 0.2f` and `Time.fixedDeltaTime == 0.004f` (±1e-5).
  - `FirstPickLocksAndLaterPicksAreIgnored`: picks 1 then 2 in the same frame → one `AnswerLocked(1)`, result index 1.
  - `PauseDuringFocusResumesSlowMotion`: `TogglePause()` twice during focus → timeScale 0, then 0.2.
  - `BulletHitIgnoresInvincibility`: `PlayerHealth.HitByObstacle()` (lives 3→2) just before a no-pick encounter fires → the bullet still takes lives to 1.
  - `LastHeartLostToBulletEndsTheGameFrozen`: enter an encounter with 1 life, wrong pick → `GameState.GameOver` and `Time.timeScale == 0` after the hit.
  - `RestartDuringFocusRestoresNormalTime`: `GameManager.Restart()` during focus → after reload `Time.timeScale == 1`, `Time.fixedDeltaTime == 0.02f`, `WorldTime.FocusFactor == 1`.
- [ ] **Step 2: Implement** the code, prefab, scene changes and deletions above. Read back saved references. Run all EditMode tests — all pass.
- [ ] **Step 3: Run one filtered PlayMode run** covering `EncounterTests`, `LevelRunTests`, `FeedbackFxTests` and `MenuFlowTests` — all pass. Console clean. Deterministic Play-mode screenshot of the Focus Zone mid-flight.
- [ ] **Step 4: Commit** — `Encounter 8: enemy encounters replace answer platforms`.

### Task 9: Focus look, sounds and hit feedback

**Files:**
- Create: `Assets/_Project/Scripts/UI/FocusLook.cs` (on a new `Focus Volume` in Game, Sandbox_Level, Sandbox_QuestionZone: URP `Volume`, profile `Assets/_Project/Settings/PostFX_Focus.asset` with vignette 0.45 and colour adjustments saturation −35; weight eases to 1 on `FocusStarted` and to 0 on `FocusEnded` over 0.2 s of **unscaled** time; also toggles a soft glow sprite child on the player)
- Modify: `Assets/_Project/Scripts/Audio/SoundSet.cs` (+ `Fire`, `Shockwave`, `EnemyPop`, `FocusIn`, `FocusOut`), `AudioManager.cs` (play them on `FocusStarted` / `FocusEnded` / `EnemyDefeated`; `Fire` with `FocusIn`), `SoundSet.asset` (clips from Kenney Interface Sounds: fire = `glass`, shockwave = `maximize`, pop = `pluck`, focus in = `switch`, focus out = `toggle`)
- Modify: `Assets/_Project/Scripts/UI/FeedbackFX.cs` only if a bullet hit needs a different shake than obstacles (`HitCause.Bullet` is already routed through `OnPlayerHit`)
- Test: `Assets/_Project/Tests/EditMode/AudioMathTests.cs` (`ShippedSoundSetHasAClipForEverySound` covers the new sounds through `SoundSet.All`)

- [ ] **Step 1: Extend `SoundSet.All`** with the new sounds and run EditMode tests — `ShippedSoundSetHasAClipForEverySound` fails.
- [ ] **Step 2: Implement** `FocusLook`, the volume profile, the sound fields and clips, and the AudioManager hooks.
- [ ] **Step 3: Run all EditMode tests** — all pass. Console clean. Deterministic Play-mode screenshot: Focus Zone (dark edges, glow) and the shockwave frame.
- [ ] **Step 4: Commit** — `Encounter 9: focus look, sounds and feedback`.

### Task 10: Docs and the full-level check

**Files:**
- Modify: `GAME_DESIGN.md` §1 exactly as the spec's §9 says (gameplay line, Math Choices row, core loop, an approval note dated 2026-10-18), §2.1 Question Zone description, and §4 checkboxes.
- Modify: `IMPROVEMENT_PLAN.md` (note the encounter rework), `CLAUDE.md` (Architecture Notes: replace the tap-to-answer/platform bullet with the encounter flow, the time-scale owner and `EncounterSettings`; Current Progress entry).

- [ ] **Step 1: Run `AllLevelsTests`** (one filtered PlayMode run, ~4–5 min) — all 10 levels finishable with all answers right.
- [ ] **Step 2: Update the docs** above.
- [ ] **Step 3: Commit** — `Encounter 10: docs and full-level check`.
