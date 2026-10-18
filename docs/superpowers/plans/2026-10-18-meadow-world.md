# Meadow World Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cut the game to 5 levels (Meadow 1–3, Desert 4–5) and turn the Meadow into Geometry Dash-style terrain — steps, platforms, pillars, route splits, walkers, saws and decoration — with the player in control during enemy encounters at 0.5× slow-mo.

**Architecture:** `LevelPlanner` stays pure and deterministic but grows into a segment generator: it outputs ground with heights, platforms, hazards, encounter stretches, decoration spots and a **reference jump plan**, all checked against the real jump via `JumpReach` (built on `JumpMath`). `LevelBuilder` instantiates prefabs from the plan. `LevelBot` follows the reference jump plan, so PlayMode tests keep proving every level is finishable.

**Tech Stack:** Unity 6000.3.11f1, C#, URP 2D, Kenney New Platformer Pack, Unity Test Framework. Editor work via `Unity_RunCommand`.

**Spec:** `docs/superpowers/specs/2026-10-18-meadow-world-design.md` (builds on `2026-10-18-enemy-encounter-design.md`)

## Global Constraints

- CLAUDE.md conventions: `MathRunner.<Folder>` namespaces, `[SerializeField] private`, tunables in ScriptableObjects, `GameEvents` between systems, no YAML hand-edits, ScriptableObject scene references via public setter + `SetDirty` and read back.
- 5 levels in the catalog; levels 1–3 Meadow (Hills background, grass terrain), 4–5 Desert background; levels 6–10 kept as assets but out of the catalog.
- Focus Zone **0.5×**; **no autopilot** during encounters; encounter stretches contain **no gaps**.
- Terrain heights in whole tiles (1 unit). Player: jump height 3.2, collider 0.9 × 1. Every step, platform and gap must be reachable by a full jump with margin (`JumpReach`).
- Every hazard on screen ≥ 1 s before the player reaches it (camera shows ~10 units ahead; at ≤ 8 units/s this holds automatically — keep run speeds ≤ 8).
- Decorations: no colliders, sorting below the player, never on a hazard or a jump take-off/landing.
- Answer buttons sit in a row just under the question panel (top of the screen), not bottom-right.
- PlayMode: at most one filtered run per task (user's request). Revert `LiberationSans SDF - Fallback.asset` before committing; never commit the user's untracked Kenney `Additional_Pack/`, `Emotes/`, `Enemies/` folders. Commits end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **A walker patrolling into a step or off a platform edge** → it must turn around at its patrol bounds, never fall or float → Task 4 `WalkerTurnsAtItsBounds` + planner `WalkersStayOnFlatGround`.
2. **Falling into a gap right before an encounter** → respawn at a checkpoint that sits on terrain, never inside a step → Task 3 `CheckpointsSitOnTheGroundTop`.
3. **Jumping during the Focus Zone at 0.5×** → jump height and reach unchanged (only slower) → Task 2 `JumpReachIsTheSameInSlowMotion` (PlayMode).
4. **Decoration hiding a hazard** → no decoration within 1.5 units of a spike, walker, saw or gap edge → Task 6 `DecorationsKeepClearOfHazards`.
5. **A route split whose high path ends over a gap** → both routes land on solid ground → Task 3 `BothRoutesOfASplitRejoinSolidGround`.

---

### Task 1: Five levels, Meadow and Desert themes

**Files:**
- Assets via `Unity_RunCommand`: `Resources/LevelCatalog.asset` (levels 1–5 only); `Level04.asset`, `Level05.asset` `_background` → `Backgrounds/Desert.asset` (1–3 stay Hills).
- Modify: `Tests/PlayMode/AllLevelsTests.cs` (`CatalogListsFiveLevelsInOrder`, `Range(0, 4)`), `Tests/PlayMode/MenuFlowTests.cs` (5 buttons, "Levels 2-5 locked").
- Test: `Tests/EditMode/LevelThemeTests.cs`

**Interfaces:** Produces the 5-level catalog every later task's tests use.

- [ ] **Step 1: Write the failing test** `FiveLevelsMeadowThenDesert`: `LevelCatalog.Load().Levels.Count == 5`, numbers 1..5 in order, levels 1–3 `Background.name == "Hills"`, 4–5 `"Desert"`.
- [ ] **Step 2: Run EditMode** — fails (10 levels, 4–5 are Trees).
- [ ] **Step 3: Update the catalog and level assets** (SerializedObject on the assets; read back).
- [ ] **Step 4: Update** the two PlayMode tests' counts; run all EditMode — pass.
- [ ] **Step 5: Commit** — `Meadow 1: five levels, Meadow and Desert themes`.

### Task 2: Encounters with player control at 0.5×, answers at the top

**Files:**
- Modify: `Scripts/Enemies/EnemyEncounter.cs` (remove `BeginAutopilot` / `EndAutopilot` calls and the comment about autopilot), `Scripts/Enemies/FlyingEnemy.cs` (hover y follows the player with `Mathf.SmoothDamp`, smoothing 0.25 s, so jumps don't yank the enemy), `EncounterSettings.asset` `_focusScale` → 0.5.
- Scenes `Game`, `Sandbox_Level`, `Sandbox_QuestionZone`: move `AnswerButtons` root to top-centre, directly under the question panel (anchor top-centre, y ≈ −175), hint text above or below the row.
- Modify: `Tests/PlayMode/EncounterTests.cs` (`TimeSlowsDuringFocus` expects 0.5 / `base × 0.5`; `PauseDuringFocusResumesSlowMotion` expects 0.5; new `PlayerCanJumpDuringFocus`, `JumpReachIsTheSameInSlowMotion`).
- Test: `Tests/EditMode/FlyingEnemyTests.cs` (`HoverHeightFollowsThePlayerSmoothly` for a new pure `FlyingEnemy.SmoothY(float current, float target, ref float velocity, float smoothTime, float dt)`).

**Interfaces:**
- Produces: encounters never touch `PlayerController` autopilot; `PlayerController.BeginAutopilot/EndAutopilot/QueueAutoJump` become unused (delete them and their fields).

- [ ] **Step 1: Write the failing tests**: EditMode `HoverHeightFollowsThePlayerSmoothly` (moves toward the target, never overshoots, reaches within 0.01 after 2 s of 0.02 steps); PlayMode `PlayerCanJumpDuringFocus` (during focus `NotifyJumpPressed` → `Jumped` fires and y rises) and `JumpReachIsTheSameInSlowMotion` (peak height of a held jump during focus within 0.05 of one outside focus); update the two 0.2 assertions to 0.5.
- [ ] **Step 2: Run EditMode** — the new EditMode test fails (method missing).
- [ ] **Step 3: Implement** the code, asset and scene changes; delete the unused autopilot members.
- [ ] **Step 4: Run EditMode** (pass), then one filtered PlayMode run: `EncounterTests` — all pass. Screenshot a Focus Zone with the answer row at the top.
- [ ] **Step 5: Commit** — `Meadow 2: player control in encounters at 0.5x, answers at the top`.

### Task 3: Terrain in the planner (steps, platforms, pillars, splits, jump plan)

**Files:**
- Create: `Scripts/Level/Layout/JumpReach.cs`
- Modify: `Scripts/Level/Layout/LevelPlanner.cs`, `Scripts/Level/Layout/LevelConfig.cs` (world knobs)
- Test: `Tests/EditMode/LevelPlannerTests.cs`, `Tests/EditMode/JumpReachTests.cs`

**Interfaces:**
- Produces `static class JumpReach`: `float AirTime(LevelPlanSettings s, float heightDiff)` (full jump from ground to landing `heightDiff` higher, via `JumpMath.AirTimeToLand`), `float Distance(LevelPlanSettings s, float heightDiff)` = `AirTime × RunSpeed`, `bool CanReach(LevelPlanSettings s, float dx, float heightDiff)` (needs 15 % margin on both).
- Produces in `LevelPlanSettings`: `JumpHeight`, `Gravity`, `GravityScale`, `FallMultiplier` (from the player), `MaxStepTiles` (0–2), `RouteSplits` (count per level), `PlatformChance` (0–1). `From(config, span, focusScale, PlayerController player)` fills the jump fields.
- Produces in `LevelPlan` (replacing the old flat lists): `Ground: List<(float Start, float End, float Top)>`, `Platforms: List<(float Left, float Right, float Top)>`, `Spikes: List<(float X, float Y)>`, `Checkpoints: List<(float X, float Y)>`, `Jumps: List<(float X, float HoldSeconds)>` (reference take-offs, hold in **scaled** seconds), `float GroundTopAt(float x)` (`float.NaN` over a gap), and the existing `Zones`, `FinishX`, `EndX`, `ZoneLength`.
- Produces in `LevelConfig`: `[SerializeField] int _maxStepTiles; int _routeSplits; float _platformChance;` + getters; level values: L1 (1, 1, 0.3), L2 (1, 2, 0.4), L3 (2, 3, 0.5), L4–5 (1, 1, 0.3).
- Generation: the existing beat loop (hazards → encounter → recovery) now draws each hazard beat from {spike, gap, step up, step down, pillar, platform hop} weighted by the knobs; `RouteSplits` beats become a high path (2–3 platforms) over a low path (ground + spike). Encounter stretches draw only {flat, spike, step, pillar} — never gaps. Heights stay within [0, 3] tiles.

- [ ] **Step 1: Write the failing tests**: `JumpReachTests` (distance at heightDiff 0 matches the measured 0.76 s × speed ±0.03 s; can't reach a 4-tile step; can reach a 2-tile step 2 units away); planner `EveryStepAndPlatformIsReachable` (seeds 1–20, speeds 5/6/8: each consecutive standing surface along `Jumps` is `CanReach`), `BothRoutesOfASplitRejoinSolidGround`, `EncounterStretchesHaveNoGaps`, `CheckpointsSitOnTheGroundTop`, `HeightsStayWithinThreeTiles`, `HarderLevelsHaveMoreVariety` (L3 has more height changes than L1 for the same seed), and update the existing tests to the new tuple shapes (`SameSeedGivesTheSameLayout` stays).
- [ ] **Step 2: Run EditMode** — compile fails (new members) → RED.
- [ ] **Step 3: Implement** `JumpReach`, the planner, `LevelConfig` knobs; set L1–L5 values on the assets. Keep `LevelBuilder` compiling by building ground with `SetRect(start, -4, end, top)` and spikes at `(X, Y)` (platforms come in Task 5).
- [ ] **Step 4: Run all EditMode** — pass.
- [ ] **Step 5: Commit** — `Meadow 3: terrain planner with steps, platforms and route splits`.

### Task 4: Walkers and saws

**Files:**
- Create: `Scripts/Level/Walker.cs`, `Scripts/Level/Saw.cs`; prefabs `Prefabs/Level/Walker.prefab` (slime: `slime_normal_walk_a/b` flipbook, trigger collider + `Obstacle`), `Prefabs/Level/Saw.prefab` (`saw_a/b`, spinning, trigger + `Obstacle`).
- Modify: `LevelPlanner.cs` (`Walkers: List<(float Left, float Right, float Y)>`, `Saws: List<(float X, float Y)>`; hazard beats gain walker/saw), `LevelConfig.cs` (`_walkerChance`, `_sawChance`: L1 0.15/0, L2 0.25/0.1, L3 0.3/0.2, L4–5 0.15/0.1).
- Test: `Tests/EditMode/WalkerTests.cs`, `LevelPlannerTests.cs`

**Interfaces:**
- Produces: `Walker` — `void Configure(float left, float right, float speed)`; `static float PatrolX(float left, float right, float speed, float time)` (ping-pong between bounds); faces its direction of travel. `Saw` — spins at a serialized speed; both reuse `Obstacle` for damage.
- Walker speed 1.5 units/s, patrol width ≥ 3 units on a flat stretch; walkers allowed inside encounter stretches, saws too.

- [ ] **Step 1: Write the failing tests**: `WalkerTurnsAtItsBounds` (PatrolX stays in [left, right], hits both ends over a full period); planner `WalkersStayOnFlatGround` (the ground top is constant across `[Left, Right]`), `SawsSitOnASurface`, plus the reference jump plan still clears them (`Jumps` contains a take-off before each).
- [ ] **Step 2: Run EditMode** — RED.
- [ ] **Step 3: Implement** the components, prefabs (import the sprites PPU 64), planner and knobs.
- [ ] **Step 4: Run all EditMode** — pass.
- [ ] **Step 5: Commit** — `Meadow 4: walkers and saws`.

### Task 5: Building the terrain

**Files:**
- Create: `Prefabs/Level/Platform.prefab` (`TiledStrip` with `terrain_grass_horizontal_left/middle/right`, `BoxCollider2D` on layer Ground with `PlatformEffector2D` one-way)
- Modify: `Scripts/Level/Layout/LevelBuilder.cs` (instantiate ground with tops, platforms, spikes at Y, walkers, saws, checkpoints at Y, encounters at their ground top); import `terrain_grass_horizontal_*` and `terrain_grass_vertical_*` (PPU 64, Full Rect).
- Test: none pure — verified by Task 7's PlayMode run; here an Edit-mode capture of a built level.

**Interfaces:**
- Consumes the `LevelPlan` from Tasks 3–4. Produces a fully built level in `Game`.

- [ ] **Step 1: Implement** the prefab and builder changes.
- [ ] **Step 2: Build level 3 in Edit mode** via `Unity_RunCommand` (instantiate a temporary `LevelBuilder` far from the scene, e.g. at x = 1000) and capture 3 screenshots (start, a route split, an encounter stretch); delete it afterwards.
- [ ] **Step 3: Run all EditMode** — pass. Console clean.
- [ ] **Step 4: Commit** — `Meadow 5: build stepped terrain, platforms and hazards`.

### Task 6: Decoration

**Files:**
- Create: `Scripts/Level/DecorationSet.cs` (ScriptableObject: `Sprite[] Sprites`, `float[] Weights`), asset `ScriptableObjects/Worlds/MeadowDecorations.asset` (`grass`, `bush`, `rock`, `mushroom_red`, `mushroom_brown`, `fence`, `sign`; grass/bush most common), `Prefabs/Level/Decoration.prefab` (SpriteRenderer only, sorting order 5, pivot bottom).
- Modify: `LevelPlanner.cs` (`Decorations: List<(float X, float Y, int Kind)>`, `LevelPlanSettings.DecorationDensity` per 10 units and `DecorationKinds`), `LevelConfig.cs` (`_decorationDensity` 2.5 for all; `_decorations` → `DecorationSet` with a public setter), `LevelBuilder.cs` (spawn them).
- Test: `LevelPlannerTests.cs`

- [ ] **Step 1: Write the failing tests**: `DecorationsKeepClearOfHazards` (≥ 1.5 units from every spike, walker range, saw, gap edge and `Jumps` take-off), `DecorationsSitOnTheGroundTop`, `DecorationDensityIsRoughlyAsSet` (within ±40 % per level), `SameSeedSameDecorations`.
- [ ] **Step 2: Run EditMode** — RED.
- [ ] **Step 3: Implement**; import the decoration sprites (PPU 64, pivot bottom-centre).
- [ ] **Step 4: Run all EditMode** — pass. Edit-mode capture of a decorated stretch (same method as Task 5).
- [ ] **Step 5: Commit** — `Meadow 6: decoration`.

### Task 7: Bot follows the jump plan; every level finishable

**Files:**
- Modify: `Tests/PlayMode/LevelBot.cs` — optional `LevelPlan` input (`new LevelBot(plan)`): presses jump when the player crosses each `Jumps[i].X`, holding for `HoldSeconds` of **scaled** time (frames = HoldSeconds ÷ `Time.deltaTime`), so it works in slow-mo; the old spike/gap reflex stays for the hand-built `Sandbox_Level` (no plan).
- Modify: `Tests/PlayMode/AllLevelsTests.cs` (pass `LevelBuilder.Plan` to the bot), `Tests/PlayMode/MenuFlowTests.cs` if its bot needs the plan.

- [ ] **Step 1: Implement** the bot change.
- [ ] **Step 2: Run one filtered PlayMode run**: `AllLevelsTests` (5 levels), `EncounterTests`, `LevelRunTests`, `MenuFlowTests` — all pass; every level reaches LevelComplete with all answers right and no hazard hits.
- [ ] **Step 3: Commit** — `Meadow 7: bot follows the jump plan; all five levels finishable`.

### Task 8: Docs

**Files:** `GAME_DESIGN.md` (§2.1 no autopilot, 0.5×; §2.2 jump during encounters; §2.4/§4: 5 levels — Meadow 1–3, Desert 4–5; world pieces), `CLAUDE.md` (remove "never put hazards inside an encounter"/autopilot notes; describe the segment generator, `JumpReach`, the jump plan, decoration set), `IMPROVEMENT_PLAN.md` (Meadow done; Desert next).

- [ ] **Step 1: Update the docs.**
- [ ] **Step 2: Commit** — `Meadow 8: docs`.

### Later (not in this plan)
Desert terrain/obstacles for levels 4–5 (sand tiles, cactus), coins on high routes, bees/flying hazards.
