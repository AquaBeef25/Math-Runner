# Random Encounters Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Each encounter meets one of four enemies picked at random per level seed — alien (fireball, random sky spot), bee (charges), frog (leaps), fly (follows on a wrong answer and bites at the next wrong one) — and the harmless scenery loses its flies and frogs.

**Architecture:** `LevelPlanner` picks an `EnemyKind` (and, for the alien, a sky spot) per encounter with its own random stream. `EnemyEncounter` keeps its flow (arrive → wind-up → Focus + question → resolve) but works through an `IEncounterAttack` (the thing that reaches the runner exactly at the deadline): the alien's `EnemyBullet`, or the enemy itself for bee/frog/fly via a new `ChargingEnemy`. A `FollowingFly` holds the one fly that may follow the runner to the next encounter.

**Tech Stack:** Unity 6000.3.11f1, 2D URP, C#, Unity Test Framework (EditMode + PlayMode), Kenney New Platformer Pack.

**Spec:** `docs/superpowers/specs/2026-10-19-random-encounters-design.md`

## Global Constraints

- The question flow is unchanged: 4 choices, first pick locked, Focus Zone at `EncounterSettings.FocusScale`, right answer = shockwave, the attack is the timer and lands exactly `flightRealSeconds` (real time) after the question shows.
- Hearts are lost only through `PlayerHit` (`PlayerHealth.HitByBullet` for attacks, so invincibility never swallows them). The fly's extra bite is a second `HitByBullet` at the same moment.
- Random picks use `System.Random(seed * 59 + 7)` in the planner and never change terrain, jumps, coins, decorations, backdrop or life.
- Never two flies in a row (covers "no new fly while one follows").
- No other gameplay changes. `EnemyKind` default is `Alien`, so the hand-built sandboxes keep today's behaviour.
- CLAUDE.md conventions: `[SerializeField] private`, one class per file, no bare `Math.`, only `GameManager` writes `Time.timeScale`, Editor setup via `Unity_RunCommand` with setters + `SetDirty`, revert the TMP fallback asset before commits, PlayMode at most one filtered run per task.

## Review Focus

1. **Answering during the enemy's fly-in / leap with a moving runner** — the attack must still arrive on time when the runner speeds over ramps or jumps. Pinned by `AttackArrivesExactlyAtTheDeadline` (Task 3, moving target) and the PlayMode encounter tests.
2. **A following fly when the level ends or the runner loses the last heart** — it flies away without a bite / no exception after the scene unloads. Pinned by `FollowingFlyLeavesAtTheFinish` and `FollowingFlyIsForgottenOnReload` (Task 5).
3. **Pause or restart while a fly follows** — `FollowingFly.Current` must not survive a scene reload. Pinned by `FollowingFlyIsForgottenOnReload` (Task 5).
4. **Alien spot behind the question panel or off-screen** — spot range pinned by `AlienSpotsStayInTheVisibleSky` (Task 2).
5. **Frog leap through a step or ramp** — the frog is drawn over terrain (no collider) and lands on the runner; pinned visually in Task 6 captures; the leap keeps its peak above both start and end (`LeapPeaksAboveBothEnds`, Task 3).

---

### Task 1: Scenery without flies and frogs

**Files:** Modify (Editor): `ScriptableObjects/Worlds/MeadowLife.asset`, `DesertLife.asset` (Flyer and Hopper weight 0 via `LifeSet.SetKind`). Test: `Assets/_Project/Tests/EditMode/LifeMotionTests.cs` (or a new `ScenerySetTests.cs`).

- [ ] **Step 1: Failing test** `ScenerySetsHaveNoFliesOrFrogs` — loads both assets with `AssetDatabase.LoadAssetAtPath<LifeSet>` and asserts `Weights[(int)LifeKind.Flyer] == 0` and `Weights[(int)LifeKind.Hopper] == 0`, and Walker > 0.
- [ ] **Step 2: Run EditMode** — RED (Meadow has flyers and frogs).
- [ ] **Step 3:** set the weights through `Unity_RunCommand`, `SetDirty`, `SaveAssets`, read back.
- [ ] **Step 4: Run EditMode** — all pass.
- [ ] **Step 5: Commit** — `Random encounters 1: no harmless flies or frogs`.

### Task 2: An enemy for every encounter in the plan

**Files:**
- Create: `Assets/_Project/Scripts/Level/Layout/EnemyKind.cs` — `public enum EnemyKind { Alien, Bee, Frog, Fly }` (append-only).
- Modify: `LevelPlanner.cs` — `LevelPlanSettings.EnemyWeights` (float[4], null = all Alien), `LevelPlan.ZoneEnemies : List<(EnemyKind Kind, float SpotX, float SpotY)>` index-aligned with `Zones`; `PickEnemies(LevelPlan, LevelPlanSettings)` after `PlaceLife`.
- Modify: `LevelConfig.cs` — `_enemyWeights` (float[4], default `{1,1,1,1}`), `EnemyWeights`.
- Test: `Assets/_Project/Tests/EditMode/EncounterPlanTests.cs`.

Rules: weighted pick per zone; if the pick is `Fly` and the previous zone was `Fly`, re-pick among the others (Alien if they all weigh 0); alien spot = offset from the runner (`SpotX` in [3, 8], `SpotY` in [2.5, 4.5]); other kinds get spot (0, 0).

- [ ] **Step 1: Failing tests:** `EveryEncounterHasAnEnemy` (counts equal), `SameSeedSameEnemies`, `EnemiesNeverChangeTheRestOfThePlan` (Ground, Jumps, Coins, Life, Zones equal with/without weights), `NeverTwoFliesInARow([Range(1, 30)] seed)` (equal weights: no two consecutive `Fly`; and with weights `{0,0,0,1}` the zones alternate Fly, Alien, Fly… because a repeat fly falls back to Alien), `AllFourKindsAppear` (over seeds 1–10 with equal weights), `AlienSpotsStayInTheVisibleSky`, `NoWeightsMeansAllAliens`.
- [ ] **Step 2: Run EditMode** — RED.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run EditMode** — pass.
- [ ] **Step 5: Commit** — `Random encounters 2: an enemy for every encounter`.

### Task 3: Attack paths and the charging enemies

**Files:**
- Create: `Assets/_Project/Scripts/Enemies/IEncounterAttack.cs` — `event Action Arrived; float RemainingFraction { get; } void Launch(Transform target, float flightSeconds); void Fizzle();`
- Modify: `EnemyBullet.cs` — implements `IEncounterAttack` (no behaviour change).
- Create: `Assets/_Project/Scripts/Enemies/AttackPath.cs` — pure:
  - `Vector2 Charge(Vector2 position, Vector2 target, float remaining, float dt)` (same homing rule as `EnemyBullet.Step`),
  - `float LeapHeight(float u, float peak)` (parabola, 0 at u = 0 and 1),
  - `Vector2 Buzz(float t, float amplitude, float hz)` (side wobble that is 0 at arrival).
- Create: `Assets/_Project/Scripts/Enemies/ChargingEnemy.cs` — one MonoBehaviour for bee/frog/fly, `[SerializeField] private AttackStyle _style` (`Charge`, `Leap`, `Buzz`), `Appear(Transform player, EncounterSettings s, Vector2 offset)` (fly-in like the alien; the frog lands on the ground ahead), `WindUp()`, `Defeat()`, `Retreat()`, implements `IEncounterAttack` (the enemy is its own attack), frames via `SpriteFlipbook`.
- Modify: `FlyingEnemy.cs` — `Appear(Transform player, EncounterSettings s, Vector2 offset)` uses the given offset instead of `HoverOffset` when non-zero.
- Test: `Assets/_Project/Tests/EditMode/AttackPathTests.cs`.

- [ ] **Step 1: Failing tests:** `AttackArrivesExactlyAtTheDeadline` (simulate `Charge` with a target moving at 6 u/s for 3 s in 0.02 steps → distance < 0.05 at the end), `LeapPeaksAboveBothEnds`, `BuzzIsStillAtArrival`, `ChargingEnemyHasNoSolidCollider` (built from code: any Collider2D is a trigger or none).
- [ ] **Step 2: Run EditMode** — RED.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run EditMode** — pass.
- [ ] **Step 5: Commit** — `Random encounters 3: attack paths and charging enemies`.

### Task 4: Encounters spawn the planned enemy

**Files:**
- Modify: `EnemyEncounter.cs` — `Configure(QuestionDirector, float flightRealSeconds, bool speedBonus, EnemyKind kind = EnemyKind.Alien, Vector2 spot = default)`; serialized prefabs `_beePrefab`, `_frogPrefab`, `_flyPrefab` (`ChargingEnemy`); `Fire()` launches `IEncounterAttack` (alien → new bullet; others → the enemy itself); right answer → shockwave, `Defeat`/`Fizzle`; wrong/missed arrival → `HitByBullet` (fly: hand-off to Task 5, until then the fly also bites).
- Modify: `LevelBuilder.cs` — passes `Plan.ZoneEnemies[i]` to `Configure`; `LevelPlanSettings.From` reads `config.EnemyWeights`.
- Create (Editor): prefabs `Prefabs/Enemies/BeeEnemy.prefab`, `FrogEnemy.prefab`, `FlyEnemy.prefab` (sprites `bee_a/b`, `frog_idle/jump`, `fly_a/b`, scale so they read at encounter size); assign on the `EnemyEncounter` prefab.
- Test: `Assets/_Project/Tests/PlayMode/EncounterTests.cs` — runtime-configured kinds in Sandbox_Level.

- [ ] **Step 1: Failing tests:** `BeeWrongCostsOneHeart`, `FrogWrongCostsOneHeart`, `BeeRightDefeatsIt`, `AttackLandsWhenTimeRunsOut` (bee, no pick → Missed at the deadline in real seconds).
- [ ] **Step 2: One filtered PlayMode run** (`EncounterTests`) — new tests RED, the 13 existing pass.
- [ ] **Step 3: Implement** code, prefabs, builder wiring.
- [ ] **Step 4: Run EditMode + the same filtered PlayMode run** — all pass.
- [ ] **Step 5: Commit** — `Random encounters 4: encounters spawn the planned enemy`.

### Task 5: The following fly

**Files:**
- Create: `Assets/_Project/Scripts/Enemies/FollowingFly.cs` — `static FollowingFly Current`; `Follow(Transform player)` (hover behind-above the runner with a wobble), `Kill(Vector3 at)` (defeat effect), `BiteAndLeave(PlayerHealth health)` (one `HitByBullet`, then flies away), `Leave()`; clears `Current` in `OnDestroy`; subscribes to `GameEvents.LevelCompleted` → `Leave()`.
- Modify: `EnemyEncounter.cs` — fly encounter wrong/missed: no hit, the fly becomes `FollowingFly.Current`; any encounter right → `FollowingFly.Current?.Kill(...)`; any encounter's attack arriving on wrong/missed → after its own hit, `FollowingFly.Current?.BiteAndLeave(health)`.
- Test: `EncounterTests.cs` (PlayMode).

- [ ] **Step 1: Failing tests:** `FlyWrongFollowsWithoutBiting` (lives unchanged, `Current` set), `FollowingFlyDiesOnNextRightAnswer` (lives unchanged, `Current` null), `FollowingFlyBitesOnNextWrong` (−2 hearts at once, then `Current` null), `FollowingFlyLeavesAtTheFinish` (raise `LevelCompleted` → `Current` null, no hit), `FollowingFlyIsForgottenOnReload`.
- [ ] **Step 2: One filtered PlayMode run** — RED.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: EditMode + the same filtered PlayMode run** — pass.
- [ ] **Step 5: Commit** — `Random encounters 5: the following fly`.

### Task 6: Finishable check, captures and docs

**Files:** `CLAUDE.md`, `GAME_DESIGN.md` (encounter section: one bullet), `IMPROVEMENT_PLAN.md`.

- [ ] **Step 1:** Play-mode captures (paused play, stepped frames, camera to `Temp/`) of each enemy kind in an encounter and the alien at two different spots; check nothing hides behind the question panel.
- [ ] **Step 2: One filtered PlayMode run:** `AllLevelsTests|EncounterTests|MenuFlowTests|LevelRunTests` — all pass.
- [ ] **Step 3: Update the docs.**
- [ ] **Step 4: Commit** — `Random encounters 6: finishable check and docs`.
