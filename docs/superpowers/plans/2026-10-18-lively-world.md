# Lively World Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sparse, harmless moving life (flyers, critters, frogs, flags, torches, swinging crates, leaping fish, emote bubbles) in all five levels, and thinner ground props and backdrop.

**Architecture:** `LevelPlanner` gains a `Life` list and a `Fish` list placed with their own random stream (never changing terrain, hazards, jumps or coins). A per-world `LifeSet` asset holds the weights and sprites; a static `LifeFactory` turns plan entries into collider-free GameObjects driven by small motion components whose maths are pure static functions.

**Tech Stack:** Unity 6000.3.11f1, 2D URP, C#, Unity Test Framework (EditMode + PlayMode), Kenney New Platformer Pack + vector Emotes.

**Spec:** `docs/superpowers/specs/2026-10-18-lively-world-design.md`

## Global Constraints

- Nothing added here may hurt, block, push or slow the runner: life GameObjects have **no Collider2D and no `Obstacle`**, and stay on layer `Default`.
- No harmless bee or slime anywhere (they are hazards in this game).
- Art only from the New Platformer Pack (`Kenney/NewPlatformerPack/`) and `Kenney/Emotes/PNG/Vector/Style 1/`; never `Kenney/Enemies/` or `Kenney/Additional_Pack/` (and don't commit those folders).
- Life uses its own `System.Random(seed * 41 + 13)`; terrain, hazards, `Jumps`, `Coins`, `Decorations` and `Backdrop` must be identical with and without life.
- Ground props: `_decorationDensity` 1.0 on Levels 1–5 (was 2.5). Backdrop spacing 6–12 units (was 4–9).
- Life density about one item per 15–20 units: `_lifeDensity` 0.6 per 10 units of ground, minimum 8 units between life items.
- Conventions from CLAUDE.md: `[SerializeField] private`, namespace `MathRunner.Level`, no bare `Math.`, Editor setup via `Unity_RunCommand` with public setters + `SetDirty` for ScriptableObject references, revert the TMP fallback asset before committing, PlayMode at most one filtered run per task.

## Review Focus

1. **A life item drawn over a hazard or coin on a phone-sized view** → reads as clutter. Covered by `GroundLifeKeepsClearOfHazards` and `FlyersKeepClearOfJumpArcsAndCoins` (Task 2).
2. **A life item that accidentally gets physics** (a `Collider2D` added by a prefab or sprite import) → could bump the runner. Covered by `BuiltLifeHasNoColliders` (Task 3) and `AllLevelsTests` unchanged (Task 5).
3. **Slow-motion focus** → life must slow with the world, never run at real speed. Motion components use `Time.deltaTime` (scaled); checked by reading the components in review, and `SpriteFlipbook` already uses scaled time.
4. **A world whose LifeSet has zero weight for a kind** (e.g. no fish in the desert) → that kind never appears. Covered by `KindsWithZeroWeightNeverAppear` (Task 2).
5. **Levels with no LifeSet** (Sandbox scenes, Levels 6–10 data) → no life, no errors. Covered by `NoLifeWithoutWeights` (Task 2).

---

### Task 1: Thin out ground props and backdrop

**Files:**
- Modify: `Assets/_Project/Scripts/Level/Layout/LevelPlanner.cs` (`PlaceBackdrop`: `MinGap = 6f`, `MaxGap = 12f`)
- Modify: `Assets/_Project/Tests/EditMode/LevelPlannerTests.cs` (`BackdropPropsSitOnTheGround`)
- Modify (Editor): `ScriptableObjects/Levels/Level01–05.asset` `_decorationDensity` → 1.0

- [ ] **Step 1: Change the test.** In `BackdropPropsSitOnTheGround`, the spacing assertion becomes `Is.GreaterThanOrEqualTo(6f - 1e-4f)` with message `"props are at least 6 units apart"`. Only the minimum gap is pinned (gaps can be wider where hazards block a spot).
- [ ] **Step 2: Run EditMode** (`MathRunner.Tests.EditMode`) — `BackdropPropsSitOnTheGround` fails (gaps of 4–6 exist).
- [ ] **Step 3: Implement** the two constants; set `_decorationDensity` = 1.0 on Levels 1–5 through `SerializedObject` + `SetDirty` + `SaveAssets`, read back.
- [ ] **Step 4: Run EditMode** — all pass. Edit-mode capture of Level 1 x 0–40 and Level 4 x 130–170 (`LevelBuilder.SendMessage("Build", config)` in a freshly opened Game scene).
- [ ] **Step 5: Commit** — `Lively world 1: fewer ground props and backdrop`.

### Task 2: Life in the level plan

**Files:**
- Create: `Assets/_Project/Scripts/Level/Layout/LifeKind.cs` — `public enum LifeKind { Walker, Hopper, Flyer, Flag, Torch, Swing }`
- Modify: `Assets/_Project/Scripts/Level/Layout/LevelPlanner.cs`
- Modify: `Assets/_Project/Scripts/Level/Layout/LevelConfig.cs` (`_lifeDensity` default 0.6, `LifeDensity`)
- Test: `Assets/_Project/Tests/EditMode/LifePlanTests.cs`

**Interfaces:**
- Produces: `LevelPlanSettings.LifeDensity` (float, per 10 units), `LevelPlanSettings.LifeWeights` (float[6] indexed by `(int)LifeKind`, null = no life), `LevelPlanSettings.LifeVariants` (int[6], sprite variants per kind, ≥1), `LevelPlanSettings.FishChance` (float, per water pool); `LevelPlan.Life : List<(float X, float Y, LifeKind Kind, int Variant)>`, `LevelPlan.Fish : List<(float Left, float Right, float Surface)>`; `LevelPlanner.LifePatrol = 2f`.
- In this task `LevelPlanSettings.From` sets only `LifeDensity` (from `config.LifeDensity`); `LifeWeights` stays null, so real levels get no life until Task 4 wires weights, variants and fish chance from `config.Life`.

Placement (in `PlaceLife(Builder b, LevelPlanSettings s)`, called after `PlaceBackdrop`):
- Target count `round(LifeDensity * TotalGround / 10)`; attempts `target * 40`; pick a kind by weight, then a spot.
- **Walker** (patrol `[X, X + LifePatrol]`), **Hopper**, **Flag**, **Torch**: `Y` = ground top; whole footprint on one flat `Ground` segment, 1 unit in from its ends; ≥ 1.5 from spikes, saws, bees, slime patrols, every `Jumps` take-off X, gap edges and water edges (reuse the `Decorate` blocked list plus water edges).
- **Flyer**: `Y` = ground top at X + 2.5 to 3.5; X outside every full-jump arc window `[jump.X − 1, jump.X + JumpReach.AirTime(s, 0) × RunSpeed + 1]` (jumps with `HoldSeconds > 0.3`), and ≥ 1.5 from every coin.
- **Swing**: X anywhere over ground (not a gap); `Y` = ground top at X (the factory hangs it from above the screen).
- Any two life items ≥ 8 units apart. `Variant = rng.Next(LifeVariants[kind])`.
- **Fish**: each `Water` pool gets one entry with probability `FishChance`.

- [ ] **Step 1: Write the failing tests** in `LifePlanTests` (reuse the `Settings(...)` style of `LevelPlannerTests`; add `life` weights `{1,1,1,1,1,1}`, variants `{2,1,2,3,1,2}`, density 0.6, fish 1.0):
  - `SameSeedSameLife` — `Life` and `Fish` equal for two `Create` calls.
  - `LifeNeverChangesThePlan` — with vs without life weights: `Ground`, `Jumps`, `Coins`, `Decorations`, `Backdrop`, `Spikes`, `Bees`, `Walkers` equal.
  - `GroundLifeKeepsClearOfHazards([Range(1, 20)] seed)` — every Walker/Hopper/Flag/Torch on flat ground with footprint inside one segment, distances ≥ 1.5 as above.
  - `FlyersKeepClearOfJumpArcsAndCoins([Range(1, 20)] seed)` — every Flyer outside all arc windows, ≥ 1.5 from every coin, `Y − groundTop` in [2.5, 3.5].
  - `LifeIsSparse([Range(1, 10)] seed)` — consecutive life X gaps ≥ 8; count ≤ `round(0.6 × TotalGround / 10)`.
  - `KindsWithZeroWeightNeverAppear` — weights `{1,0,1,0,1,0}` → no Hopper, Flag or Swing.
  - `NoLifeWithoutWeights` — `LifeWeights = null` → `Life` and `Fish` empty.
  - `FishOnlyInWaterPools` — every `Fish` entry equals a `Water` entry; with `FishChance` 1 and water chance 0.5 there is ≥ 1 fish over seeds 1–5.
- [ ] **Step 2: Run EditMode** — compile errors / failures as expected (RED).
- [ ] **Step 3: Implement** `LifeKind`, the settings fields, `LevelPlan.Life`/`Fish`, `PlaceLife`, `LevelConfig._lifeDensity`.
- [ ] **Step 4: Run EditMode** — all pass.
- [ ] **Step 5: Commit** — `Lively world 2: life in the level plan`.

### Task 3: Moving life pieces

**Files:**
- Create: `Assets/_Project/Scripts/Level/Life/LifeMotion.cs` — pure static maths:
  - `Vector2 FlyerOffset(float t, float radius, float hz)` — figure-eight, `|x| ≤ radius`, `|y| ≤ radius / 2`.
  - `float HopHeight(float t, float period, float height)` — 0 most of the period, one parabolic hop of `height` in the last 0.4 s.
  - `float SwingAngle(float t, float maxDegrees, float hz)` — `maxDegrees × sin(2π hz t)`.
  - `(Vector2 Offset, bool Visible) FishLeap(float t, float period, float width, float height)` — hidden most of the period, then a parabolic arc across `width` peaking at `height`.
- Create: `Assets/_Project/Scripts/Level/Life/Critter.cs` (patrol with `Walker.PatrolX`, flipbook frames, flips by direction), `Hopper.cs`, `Flyer.cs`, `Swinger.cs` (chain strip + crate under a pivot), `FishJumper.cs`, `EmoteBubble.cs` (shows a sprite above its owner for 1 s, once, when `Mathf.Abs(player.x − x) < 3`; player `Transform` passed in `Configure`).
- Create: `Assets/_Project/Scripts/Level/Life/LifeFactory.cs` — `static GameObject Build(LifeKind kind, int variant, float x, float y, LifeSet set, Transform player, Transform parent)` and `static GameObject BuildFish(float left, float right, float surface, LifeSet set, Transform parent)`; sorting orders: ground life 4, flyers 7, swing −4 (behind, tint `0.85` alpha), fish 2.
- Test: `Assets/_Project/Tests/EditMode/LifeMotionTests.cs`

**Interfaces:**
- Consumes: `LifeKind` (Task 2); `LifeSet` (Task 4) — Task 3 creates `LifeSet` as a minimal class with the sprite accessors listed in Task 4 so the factory compiles; Task 4 fills in assets.
- Produces: `LifeFactory.Build`, `LifeFactory.BuildFish`.

- [ ] **Step 1: Write the failing tests:** `FlyerStaysInsideItsLoop` (sample 0–10 s), `HopReturnsToTheGround` (0 at start of each period, max ≈ height), `SwingStaysWithinItsAngle`, `FishIsHiddenMostOfTheTimeAndArcsWhenVisible`, `BuiltLifeHasNoColliders` (build every kind and a fish from a test `LifeSet` of `Sprite.Create` sprites → no `Collider2D` in children, layer `Default`, no `Obstacle`).
- [ ] **Step 2: Run EditMode** — RED.
- [ ] **Step 3: Implement** the motion maths, components (all motion on scaled `Time.deltaTime`; components cache refs in `Awake`), and the factory.
- [ ] **Step 4: Run EditMode** — all pass.
- [ ] **Step 5: Commit** — `Lively world 3: moving life pieces`.

### Task 4: Life sets, art and wiring

**Files:**
- Modify: `Assets/_Project/Scripts/Level/Life/LifeSet.cs` — ScriptableObject: `float[] Weights` (6), `int[] Variants` derived from sprite lists, `float FishChance`; per kind `Sprite[][]` frames as serializable lists: walker variants (frames a/b), hopper (idle, jump), flyer variants (a/b), flag variants (a/b), torch (a/b), swing (chain sprite, crate sprites), fish variants (swim a/b), emotes (`Sprite[]`).
- Modify: `LevelPlanner.cs` `LevelPlanSettings.From` (weights, variants, fish chance from `config.Life`); `LevelConfig.cs` (`_life`, `Life`, `SetLife`); `LevelBuilder.cs` (after decorations: for each `Plan.Life` → `LifeFactory.Build(..., _player.transform, lifeRoot)`; for each `Plan.Fish` → `BuildFish`).
- Create (Editor): `ScriptableObjects/Worlds/MeadowLife.asset`, `DesertLife.asset`; import sprites (PPU 64, pivot bottom-centre for ground life, centre for flyers/emotes; settings copied from `bee_a.png`).

Meadow weights `Walker 1 (snail, ladybug_walk), Hopper 0.7 (frog), Flyer 1 (fly, ladybug_fly), Flag 0.5 (flag_red/blue/yellow), Torch 0, Swing 0.4 (chain + block_planks / weight)`, fish chance 0.5 (`fish_blue`, `fish_yellow`). Desert weights `Walker 1 (mouse, worm_normal, worm_ring), Hopper 0, Flyer 1 (fly), Flag 0.5, Torch 0.6 (torch_on_a/b), Swing 0.4`, fish chance 0. Emotes both: `emote_exclamation`, `emote_heart`, `emote_music`, `emote_question`.

- [ ] **Step 1: Write the failing test** `LifeSetVariantsMatchItsSprites` in `LifePlanTests` (a test `LifeSet` with 2 walker variants → `Variants[(int)LifeKind.Walker] == 2`; a kind with no sprites → weight treated as 0).
- [ ] **Step 2: Run EditMode** — RED.
- [ ] **Step 3: Implement** the set, `From`, config and builder wiring; create and assign the two assets (Levels 1–3 Meadow, 4–5 Desert) via setters + `SetDirty`, read back.
- [ ] **Step 4: Run EditMode** — all pass. Play-mode captures (paused play, stepped frames, camera rendered to `Temp/`) of two stretches in Level 1 and two in Level 4, compared with the reference pictures; check sparseness and that nothing covers a hazard, coin or the question panel.
- [ ] **Step 5: Commit** — `Lively world 4: meadow and desert life`.

### Task 5: Finishable check and docs

**Files:** `CLAUDE.md`, `GAME_DESIGN.md` §2.1b (one bullet), `IMPROVEMENT_PLAN.md` (Meadow section line).

- [ ] **Step 1: One filtered PlayMode run:** `AllLevelsTests|MenuFlowTests|SlopeTests` — all pass (every level finished, all answers right, no hits).
- [ ] **Step 2: Update the docs** (life kinds, `LifeSet`, densities, harmless rule).
- [ ] **Step 3: Commit** — `Lively world 5: finishable check and docs`.
