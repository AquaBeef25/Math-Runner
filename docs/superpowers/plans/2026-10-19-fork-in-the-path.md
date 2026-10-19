# Fork in the path: answer platforms that sit alongside enemy encounters

## Context

The instructor now wants **both** kinds of question in the game:
- the enemy encounters we have now;
- the answer platforms from the original proposal, but "something creative". The old version, removed on 18 Oct, was three stone platforms floating ahead of the runner, and it didn't satisfy him.

The user picked **Fork in the path**. When a question comes up, the road splits into **3 routes at different heights**, each with a chalk answer sign:

| Answer | How you choose it | Route |
|---|---|---|
| 1 | full held jump | high ledge |
| 2 | short hop | middle ledge |
| 3 | no jump | low road, dipping into a hollow |

**Your jump is your answer.** The right route ends at a **treasure chest** of coins. A wrong route ends in **mud**, which slows you down and costs a heart.

User decisions:
- **Levels:** forks on Levels 1–2 only, about half of Level 3's questions, and about a third in Levels 4–5. Everything else stays an encounter.
- **Wrong route:** costs a heart, plus a mud slow, plus no chest.
- **Slow motion:** the world slows to 0.5× as the fork approaches, until the runner commits to a route.
- **No jump:** counts as choosing the low road. Forks have no "Missed" outcome; on timed levels, the timer is the distance to the split.

## Approach

Each question zone gets a kind: `Encounter` or `Fork`. A fork uses the same events as an encounter, so scoring, difficulty, streaks, the chalk board, hearts and the test bot all keep working:
- `QuestionShown` → `AnswerLocked` → `QuestionResolved`, plus `FocusStarted` / `FocusEnded`.

The answer is detected from **where the runner's feet land**, not from a tap.

### Geometry (verified against the player's jump numbers)

T is the ground height at the fork:
- **Low road:** a hollow at T−1, made of a ramp down, flat ground and a ramp up (existing `RampPiece`).
- **Middle ledge:** top at T+1.0.
- **High ledge:** top at T+2.7, starting about 1.5 units later than the middle ledge.

Reach:
- A tap peaks at about 1.66–1.84, so it reaches the middle ledge but never the high one.
- A full jump peaks at 3.2 (2.72 with the 0.85 safety margin), so it reaches the high ledge, rising up through the one-way middle ledge.

Head clearances, both locked by tests:
- low runner to the middle ledge: 0.6;
- middle runner to the high ledge: 0.3.

Fallback if the margins prove flaky in play: high ledge at 2.6, middle at 0.9.

Each lane has, in order: its sign at the start, then the commit band, then mud from +4 to +7 (only shown if that route is wrong), then the chest at +7.5. Then the routes rejoin on flat ground, with a checkpoint after the rejoin.

**Mapping is fixed:** choice 0 = high, 1 = middle, 2 = low. Questions are generated at runtime, so the plan can't know the right lane in advance. The bot reads `CorrectIndex` when the question appears.

## Tasks (TDD; EditMode first; one filtered PlayMode run per task)

Branch `feature/fork-in-the-path`, built Native like the last features, with one fresh review at the end.

0. **Spec.** Write `docs/superpowers/specs/2026-10-19-fork-in-the-path-design.md` from this plan and commit it.
1. **Short-hop maths.**
   - `Scripts/Player/JumpMath.cs`: add `CutJumpPeak(jumpHeight, gravity, gravityScale, holdSeconds, minJump, cutMult)` and `CutJumpAirTime(...)`.
   - `JumpReach`: add `HopPeak(s)` and `CanHopReach(s, dx, dh)`.
   - Add `MinJumpTime` and `JumpCut` to `LevelPlanSettings`, read from `PlayerController`.
   - Tests in `JumpReachTests`: a 0.1 s hop peaks at about 1.84; a 0.08 s tap reaches ≥ 1.0/0.85; a hop stays under 2.7.
2. **Data model + knob.**
   - `LevelPlanner.cs`: add `enum ZoneKind { Encounter, Fork }`, `LevelPlan.ZoneKinds` (lined up with `Zones`) and `LevelPlan.Forks` (`ForkPlan`: ZoneIndex, SplitX, BaseTop, EndX and 3 lanes).
   - Each lane: Left, Right, Top, TakeoffX, HoldSeconds, CommitX, MudLeft, MudRight, ChestX.
   - `LevelConfig._forkShare` [0..1] with a setter, plus `LevelPlanSettings.ForkShare`.
3. **Planner.**
   - Own random stream `seed·61+11`, one draw per question: `fork = NextDouble() < ForkShare`.
   - A fork zone uses `ForkTerrain` (flat, gap-free approach, then the split above) instead of `EncounterTerrain`.
   - Fork take-offs stay **out of** `Plan.Jumps`.
   - `PickEnemies` still draws for every zone, so encounter levels stay identical.
   - New `Tests/EditMode/ForkPlanTests.cs`:
     - ForkShare 0 leaves every shipped level unchanged;
     - a hop reaches the middle but not the high ledge;
     - a full jump lands on the high ledge;
     - both head clearances are ≥ 0.25;
     - the approach has no gaps or hazards;
     - the checkpoint comes after the rejoin;
     - forks never overlap encounters.
   - Existing zone tests (`EncounterStretchesNeedNoJump`, no gaps, no water) skip fork zones.
4. **Questions + events.**
   - `QuestionDirector.NextQuestion(int choices)`: a second `QuestionGenerator(rng, 3)` that shares the difficulty tracker.
   - `GameEvents`: add `AnswerMode { Tap, Route }` and an `AnswerModeSet` event. `EnemyEncounter` raises `Tap` before its question.
   - `HitCause.Mud`, after checking every switch over `HitCause` (FeedbackFX, AudioManager, LivesSystem).
5. **Player hooks.**
   - `PlayerController.SpeedFactor`, clamped 0.2–1, applied to the run speed and the slope speed, and reset on respawn and restart.
   - `PlayerHealth.HitByWrongRoute()`: costs one heart, no respawn, raises `PlayerHit(Mud)`.
6. **Pure fork logic.** New `Scripts/Level/ForkLogic.cs`:
   - `LaneFor(feetY, baseTop, grounded)`: a lane only commits once the runner is grounded and not rising.
   - `Outcome(lane, correct)`.
   - `RemainingFraction(...)`.
   - Unit-tested at the band edges.
7. **Runtime pieces.** All Editor wiring and prefabs are done through `Unity_RunCommand`.
   - New `ForkZone.cs`, modelled on `EnemyEncounter`:
     - **On trigger:** `NextQuestion(3)` → `AnswerModeSet(Route)` → `FocusStarted(0.5)` → `QuestionShown`.
     - **First lane committed:** `AnswerLocked` + `FocusEnded`, then resolve.
     - **Right:** arm the chest; a following fly dies.
     - **Wrong:** turn on the mud; a following fly bites and leaves, exactly as at encounters.
     - It ignores taps and keys 1–4. It fires once; if a respawn happens before the fork, it can fire again.
   - New `MudPatch.cs`: heart once, `SpeedFactor` 0.5 inside, back to 1 on exit.
   - New `TreasureChest.cs`: opens, coins pop out, raises `CoinCollected` once per coin.
   - New `RouteSign.cs`: a chalk number on a Kenney sign.
   - `LevelBuilder`: builds the ledges (`FloatingPlatform`), the hollow, the signs, chest, mud and `ForkZone` for each fork, with Sand/Grass skins.
   - Art: Kenney `sign`, `boxCoin` / `boxCoin_disabled` and tinted dirt for mud. `boxCoin` lives in the user's untracked `Additional_Pack`, so it is copied into a tracked `Kenney/ForkUsed/` folder, the same way `EmotesUsed` was.
8. **Board UI.** In `Route` mode, `AnswerButtons` shows 3 circles that can't be tapped:
   - captions "jump high", "hop" and "stay low";
   - colours match the in-world signs;
   - Lock and Resolve show the tick and cross as now.
   - The hint reads "Jump to answer!".
   - Tests: an EditMode test for the caption and colour mapping; a PlayMode check that a fork shows 3 circles that can't be tapped.
9. **Level assets.** Set `_forkShare`: L1 = 1, L2 = 1, L3 = 0.5, L4 = L5 = 0.33. A test asserts these values.
10. **Bot + PlayMode.**
    - `LevelBot`, on `AnswerModeSet(Route)`, picks a lane: Correct → the correct one; Wrong → `(correct+1) % 3`; Miss → the low road. It queues that lane's (TakeoffX, Hold) and skips `NotifyAnswerChosen`.
    - New `ForkTests` (forced ForkShare 1):
      - right high lane: chest coins counted, no hit;
      - wrong middle lane: 1 heart lost, slowed, then back to normal speed, focus ended;
      - no jump: low lane resolved.
    - `AllLevelsTests` must pass unchanged: every level finishable, all correct, 3 stars.
11. **Docs.**
    - `CLAUDE.md`: an architecture note.
    - `GAME_DESIGN.md`: an "instructor-requested addition" note under §1's change log. §1's text stays untouched.
    - `IMPROVEMENT_PLAN.md`.

## Critical files
- `Assets/_Project/Scripts/Level/Layout/LevelPlanner.cs`, `LevelBuilder.cs`, `LevelConfig.cs`
- `Assets/_Project/Scripts/Player/PlayerController.cs`, `PlayerHealth.cs`, `JumpMath.cs` (+ `JumpReach`)
- `Assets/_Project/Scripts/Systems/QuestionDirector.cs`, `Assets/_Project/Scripts/Core/GameEvents.cs`
- `Assets/_Project/Scripts/UI/AnswerButtons.cs`, `QuestionPanel.cs`
- `Assets/_Project/Scripts/Enemies/EnemyEncounter.cs` (the model for `ForkZone`; it gains the `AnswerModeSet(Tap)` raise)
- New: `Scripts/Level/ForkZone.cs`, `ForkLogic.cs`, `MudPatch.cs`, `TreasureChest.cs`, `RouteSign.cs`
- `Assets/_Project/Tests/PlayMode/LevelBot.cs`, new `ForkTests.cs`, new `Tests/EditMode/ForkPlanTests.cs`

## Reused, not rebuilt
- **From the encounter flow:**
  - all the `GameEvents` the encounter flow already uses;
  - `ScoreSystem` speed bonus via `remainingFraction`;
  - `LivesSystem` (hearts via `PlayerHit`);
  - `FollowingFly.Current`.
- **Level pieces:**
  - `RampPiece`, `FloatingPlatform` (one-way) and `GroundSegment.EdgeFrom`;
  - `TerrainSet` skins;
  - `Coin` / `CoinCollected`.
- **UI and helpers:** `UiTheme` colours and fonts, `Tween` / `Ease`.
- **From the old `AnswerPlatform`** (`git show dcc4ca0^`): the landing test, "feet above the top and not rising".

## Risks to watch
- **One-way ledges:** a hop through a ledge must not commit while the runner is still rising.
- **Full-jump margin:** 2.72 vs 2.7 is tight. Tests lock it; the fallback heights are above.
- **Rejoin:** the ledges must end over flat ground, not over a ramp.
- **Speed:** `SpeedFactor` must reset on respawn and restart.
- **Respawning:** a fork that already resolved stays passive.
- **Focus:** a fork and an encounter can never be in focus at the same time; the planner test guards this.

## Verification
- EditMode suite stays green: currently 1109, plus the new fork tests.
- Edit-mode captures of a planned fork in the Game scene, built with `LevelBuilder.SendMessage("Build", Level01)`: all 3 lanes, signs, chest, mud skin, and the board in Route mode at 2340×1080.
- One filtered PlayMode run: `ForkTests`, `AllLevelsTests`, `EncounterTests`, `ChalkBoardPlayTests`.
- A fresh whole-branch review on the most capable model, then a local merge only when asked.
- A hand-check on the phone APK, by the user: tap = hop, hold = high jump, feel of the slow-motion.
