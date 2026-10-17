# Math Runner — Game Design & Development Plan

> **Status legend:** ✅ Approved by instructor (do not remove or change) · 💡 Suggested improvement (optional) · ❓ Open decision

---

## 1. Approved Scope ✅

This section is the instructor-approved concept. Everything else in this document builds on it and must stay consistent with it.

**Game Title:** Math Runner

**Description:** An educational 2D platformer that combines running, jumping, and mathematics. Players run through levels, jump across platforms, and solve math problems to progress.

**Core idea:** The game turns math practice into an active challenge. Players learn while making quick decisions and avoiding obstacles.

### Gameplay
- Control a character that continuously runs through different levels.
- Math questions appear during the run.
- Jump onto the platform showing the correct answer.
- Successfully complete levels to unlock harder questions.
- Combine movement skills with quick mathematical thinking.

### Game Mechanics
| Mechanic | Approved rule |
|---|---|
| Movement | Run, jump, and avoid obstacles |
| Math Choices | Choose the platform with the correct answer |
| Scoring | Correct answers give points and let the player continue |
| Lives | Wrong answers can reduce the player's lives |
| Difficulty | Questions become harder as the game progresses |

### Main Feature: Progressive Difficulty
`Level 1 [5+3=?] → Level 5 [6×8=?] → Level 10 [453÷5+7=?]`
The better the player performs, the harder the questions become.

### Player Mode & Time
- **Single-player**: one player controls the character and completes the math challenges.
- **Real-time + time-limited**: the character moves continuously. Some questions have a time limit, and higher levels give less time.

### Platform & Tech
- **Platforms:** PC (keyboard) and Mobile (touchscreen, smartphone)
- **Engine:** Unity · **Language:** C#
- **Graphics:** 2D characters, platforms, and animations
- **Controls:** Keyboard for PC, touchscreen for mobile
- **Hardware:** Basic computer or smartphone

### Core Loop
```
RUN → QUESTION → CHOOSE → REWARD → LEVEL UP
Character moves → Math problem appears → Jump to an answer → Gain points / continue → Harder math
```

---

## 2. Detailed Design (how the approved scope works in practice)

### 2.1 The Question Zone (the heart of the game)
Each level is a sequence of running segments separated by **Question Zones**:

1. The player runs through a normal segment (gaps, obstacles).
2. A **trigger** shows the question on screen, e.g. `6 × 8 = ?`
3. Ahead, **3 answer platforms** float side by side at the same height (1.4 units above the ground), each labeled with a number.
4. The player chooses by **when** they jump: the first platform they land on is their answer. Each platform gives roughly a 0.5 s timing window. Platforms are one-way, so jumping up through one doesn't count as choosing it.
   - **Correct** → platform turns green, +points, the run continues.
   - **Wrong** → platform turns red and crumbles, −1 life, the correct platform turns green, and the player drops to the ground below (the recovery floor) and keeps running.
   - **Missed all platforms / time ran out** → counts as wrong. Not jumping simply runs underneath the platforms.
   - After any result the other platforms fade out and stop being solid, and the panel shows the solved equation, e.g. `6 × 2 = 12`.

Why same height instead of different heights: picking low/mid/high platforms by how long you hold jump needs roughly 0.05 s precision, which is too hard for kids and for touchscreens. Timing-based choice keeps the one-button control and gives generous windows.

**Key idea: the timer and the physical distance are the same thing.** The spawner places the answer platforms at `distance = runSpeed × timeLimit` ahead of the trigger. A shorter time limit therefore means the platforms are physically closer. This keeps "continuous running" and "time-limited questions" consistent with each other.
- **Timed questions** show a visible countdown bar that empties as the player reaches the platforms.
- **Untimed questions** (early levels) use a generous distance and show no countdown.

### 2.2 Movement
Because the character runs automatically, the only required input is **jump**. That makes the game well suited to mobile.

| Feature | Purpose |
|---|---|
| Auto-run at `LevelConfig.runSpeed` | Approved "continuously runs" |
| Variable jump height (hold = higher) | Short hops vs long jumps over gaps and obstacles |
| Minimum jump (0.08 s) | Even the quickest tap rises 1.56 units, clearing the 1.4-unit answer platforms |
| Coyote time (~0.1 s) | Allows jumping a moment after leaving a ledge, so it feels fair |
| Jump buffer (~0.1 s) | Allows pressing jump just before landing |
| Obstacles (spikes, gaps, rolling objects) | Approved "avoid obstacles". Hitting one = −1 life, then a short invincibility period |

**Controls**
| Action | PC | Mobile |
|---|---|---|
| Jump (tap = short, hold = high) | Space / W / ↑ | Tap or hold anywhere on the screen |
| Pause | Esc / P | Pause button (top corner) |

### 2.3 Lives, Scoring, Level Completion
- **Lives:** start with 3. Wrong answer, missed question, obstacle hit, or falling into a gap = −1. A fall respawns the player at the last checkpoint. After an obstacle hit or fall the player flashes and is invincible for 1.5 s. At 0 lives you get Game Over with "Try Again" ("Main Menu" arrives with the menus in Phase 5).
- **Score per correct answer:** `basePoints (100) × levelNumber + timedBonus + streakBonus`
  - `timedBonus` = +50 for a correct answer on a timed question. (A "remaining time" bonus doesn't work here: the platforms sit at fixed positions, so every player lands at about the same moment and landing time can't measure thinking speed.)
  - `streakBonus` = +25 × the number of correct answers already in a row (0 for the first, 25 for the second, 50 for the third…); resets on a wrong answer
  - Example, Level 5 with untimed → 4 s → 3 s → 2 s questions all correct: 500 + 575 + 600 + 625 = 2300
- **Level completion:** reach the finish flag after all Question Zones (suggested 8 questions per level) with at least 1 life left. This unlocks the next level.

### 2.4 Progressive Difficulty Table
Two layers of difficulty, matching both approved statements:
1. **Across levels** ("questions become harder as the game progresses"): each level has its own operations, number ranges, time limit, and run speed.
2. **Within a level** ("the better the player performs, the harder"): adaptive tiers (see 2.5).

| Lv | Topic | Example | Timed? / Limit | Run speed* |
|---|---|---|---|---|
| 1 | Addition, single digit | `5 + 3` ✅ | No | 5.0 |
| 2 | Subtraction, single digit (no negatives) | `9 − 4` | No | 5.0 |
| 3 | Add/Sub within 20 | `14 + 5`, `17 − 8` | Some / 12 s | 5.5 |
| 4 | Two-digit add/sub | `34 + 27` | Some / 10 s | 6.0 |
| 5 | Multiplication tables 2–9 | `6 × 8` ✅ | Some / 10 s | 6.0 |
| 6 | Exact division (tables) | `56 ÷ 8` | Some / 9 s | 6.5 |
| 7 | Mixed × ÷ up to 12 | `12 × 7`, `96 ÷ 8` | Most / 8 s | 7.0 |
| 8 | Two-step, order of operations | `4 + 6 × 3` | Most / 8 s | 7.0 |
| 9 | Three-digit add/sub, 2-digit × 1-digit | `345 + 278`, `23 × 4` | Most / 7 s | 7.5 |
| 10 | Multi-step with 3-digit division | `453 ÷ 5 + 7` ✅ (see ❓ below) | All / 6 s | 8.0 |

*Units per second; tune during playtesting.

❓ **Decide with the instructor: `453 ÷ 5 + 7 = 97.6`, which is not a whole number.** The approved example stays as written. The options are:
- **(A) Recommended:** the generator only produces divisions that come out even (e.g. `455 ÷ 5 + 7 = 98`). This uses the same format and difficulty while keeping every answer a whole number, which makes distractors easier to generate and platforms easier to read.
- **(B)** Allow 1-decimal answers at Level 10 only (`97.6`). This is harder and needs decimal distractors.

### 2.5 Adaptive Difficulty Within a Level
Each level has 3 tiers (Easy / Normal / Hard) that shift the number ranges inside that level's topic.
- Start at **Normal**.
- **3 correct in a row** → move up one tier (max Hard).
- **1 wrong answer** → move down one tier (min Easy).
- The tier never leaves the level's topic, so Level 3 questions never turn into Level 7 questions.

### 2.6 Answer Choice (Distractor) Generation
Each question has 1 correct answer and 2 wrong answers. Wrong answers should be **plausible**, because obviously wrong choices teach nothing:
- off by 1 or 2 (`48 → 47, 50`)
- off by 10 (`48 → 58`)
- wrong-operation result (`6 × 8` → `14`, from adding)
- digit swap (`48 → 84`)

Rules: all choices unique, none negative, correct answer placed at a random position.

### 2.7 Level Structure (chunk-based)
Levels are assembled from reusable **chunk prefabs** rather than hand-built one by one:
- `Chunk_Flat`, `Chunk_Gap`, `Chunk_Spikes`, `Chunk_Stairs`, …
- `Chunk_QuestionZone` (trigger, 3 answer platforms, recovery floor)

A `LevelConfig` ScriptableObject lists which chunks appear, how many questions there are, and the difficulty settings. This makes 10 levels manageable for a student project.

### 2.8 Screens / Scenes
| Scene | Contents |
|---|---|
| `MainMenu` | Play, Level Select, Settings, Quit |
| `LevelSelect` | 10 level buttons (locked/unlocked, best score) |
| `Game` | One gameplay scene that loads a `LevelConfig` |
| (overlays) | HUD, Pause, Level Complete, Game Over, Settings |

**HUD:** lives (hearts), score, streak, question panel, countdown bar, pause button.

---

## 3. Technical Architecture

### 3.1 Recommended Setup
- **Unity:** latest Unity 6 LTS
- **Packages:** Input System (keyboard and touch through one API), TextMeshPro, 2D Sprite / 2D Animation, Cinemachine (camera follow), Unity Test Framework
- **Orientation (mobile):** Landscape
- **Version control:** Git with a Unity `.gitignore`. Always commit `.meta` files. Git LFS is optional for art/audio.

### 3.2 Folder Structure
```
Assets/_Project/
├── Scripts/
│   ├── Core/        GameManager, GameState, GameEvents
│   ├── Player/      PlayerController, PlayerHealth, PlayerAnimator
│   ├── Input/       InputReader (wraps keyboard + touch)
│   ├── Math/        MathQuestion, QuestionGenerator, DistractorGenerator, DifficultyTracker
│   ├── Level/       LevelBuilder, QuestionZone, AnswerPlatform, Obstacle, FinishFlag
│   ├── Systems/     QuestionDirector, ScoreSystem, ScoreCalculator, LivesSystem
│   ├── UI/          HUD, QuestionPanel, MenuControllers, Popups
│   ├── Save/        SaveSystem (PlayerPrefs / JSON)
│   └── Audio/       AudioManager
├── ScriptableObjects/Levels/   Level01.asset … Level10.asset
├── Prefabs/  (Player, Chunks, Platforms, UI)
├── Scenes/   MainMenu, LevelSelect, Game
├── Art/      Sprites, Animations, Tilesets
├── Audio/    Music, SFX
└── Tests/EditMode/   QuestionGeneratorTests, DistractorTests, ScoreTests
```

### 3.3 Core Systems & Responsibilities
| System | Responsibility |
|---|---|
| `GameManager` | State machine: `Menu → Playing → Paused → LevelComplete / GameOver` |
| `GameEvents` | Static C# events (`OnAnswerCorrect`, `OnAnswerWrong`, `OnLifeLost`, `OnLevelComplete` …) so systems stay decoupled |
| `PlayerController` | Auto-run, jump physics (Rigidbody2D), ground check, coyote time, jump buffer |
| `InputReader` | Converts keyboard and touch into `JumpPressed` / `JumpReleased` |
| `QuestionGenerator` | **Pure C# (no MonoBehaviour)**: builds a `MathQuestion` from config, tier, and RNG. Unit-testable |
| `DifficultyTracker` | Streak counting and tier changes (2.5) |
| `QuestionZone` | Requests a question, positions platforms by time limit, resolves the result |
| `AnswerPlatform` | Shows a number and reports landing → correct/wrong |
| `ScoreSystem` / `LivesSystem` | Points, streak, lives; raise events |
| Question timing | Handled by `QuestionZone` (time limit = platform distance) and `QuestionPanel` (countdown bar); no separate timer class |
| `PlayerHealth` | Obstacle hits, falls, checkpoint respawn, invincibility flashing |
| `Obstacle` / `KillZone` / `Checkpoint` / `FinishFlag` | Level triggers (`Prefabs/Level/`) |
| `HudView` / `OverlayController` | Lives, score, streak, level label; Pause / Game Over / Level Complete screens |
| `SaveSystem` | Unlocked levels, best scores, settings |

### 3.4 Data Contracts
Math engine (built in Phase 2, `Scripts/Math/`, assembly `MathRunner.Math`):
```csharp
// One level's math = a list of rules; the generator picks a rule at random each question.
QuestionSettings { List<QuestionRule> Rules; List<string> Validate(); }
QuestionRule     { QuestionType Type; OperandRanges ForTier(DifficultyTier); }   // Easy / Normal / Hard
OperandRanges    { IntRange A, B, C; }        // meaning of A/B/C per type: see QuestionTypes.cs
QuestionType     Add, Subtract, Multiply, Divide, AddMultiply (a + b×c), MultiplySubtract (a×b − c), DivideAdd (a÷b + c)

var gen = new QuestionGenerator(new System.Random(seed));
MathQuestion q = gen.Generate(settings, tracker.Tier);
q.Text          // "6 × 8 = ?"
q.Answer        // 48
q.Choices       // { 42, 48, 58 }  — 1 correct + 2 distractors, shuffled
q.CorrectIndex  // 1
q.Problem       // type + operands, for stats later

LevelCurriculum.ForLevel(1..10)   // default rules from the table in 2.4 (seed LevelConfig assets from this)
DifficultyTracker.RecordCorrect() / RecordWrong() / Tier
```
Timing is not part of the math engine. The Question Zone decides whether a question is timed and for how long (Phase 3), using LevelConfig.

LevelConfig (Phase 5) — wraps the math rules plus level pacing:
```csharp
[CreateAssetMenu(menuName = "MathRunner/Level Config")]
public class LevelConfig : ScriptableObject
{
    [SerializeField] private int _levelNumber;
    [SerializeField] private QuestionSettings _questions;   // seeded from LevelCurriculum.ForLevel(n)
    [SerializeField] private int _questionCount = 8;
    [SerializeField, Range(0,1)] private float _timedQuestionRatio;
    [SerializeField] private float _timeLimitSeconds;
    [SerializeField] private float _runSpeed;
    [SerializeField] private GameObject[] _chunkPool;
}
```

---

## 4. Development Roadmap

Each phase ends with a playable or testable result. Build with gray boxes (plain colored rectangles) first and add art later.

### Phase 0 — Project Setup
- [x] Create a Unity 6 project (2D URP template) and install the packages from 3.1
- [x] Git repo, Unity `.gitignore`, first commit
- [x] Create the folder structure (3.2)
- [x] Add physics layers: Ground, Player, Obstacle, AnswerPlatform
- **Done when:** an empty `Game` scene opens and the project is on Git.

### Phase 1 — Core Movement (gray box)
- [x] `InputReader` (keyboard first)
- [x] `PlayerController`: auto-run, variable jump, ground check, coyote time, jump buffer
- [x] Cinemachine camera follow
- [x] A test strip with gaps (`Scenes/Sandbox_Movement`)

Measured (deterministic physics test, defaults: run 6 u/s, jump height 3.2, gravity scale 4, fall ×1.4):
full-hold apex 3.04u / airtime 0.76s / distance 4.56u · quick tap apex 0.72u · coyote ≈0.08s · jump buffer 0.10s ·
all test gaps (2–4u wide, rises up to +1.5u) clearable. Practical max flat gap ≈ 4.5u at 6 u/s. Remember to re-check
gap widths when run speed rises in later levels (distance scales with speed).
- **Done when:** jumping across gaps feels good on keyboard.

### Phase 2 — Math Engine (no visuals)
- [x] `MathQuestion`, `QuestionGenerator`, `DistractorGenerator`, `DifficultyTracker`
- [x] `LevelCurriculum` with Easy/Normal/Hard ranges for all 10 levels
- [x] EditMode tests: whole-number answers, no negatives, unique choices, the correct answer is always included, and tier rules work (75 tests, all passing)

Notes: answers are whole numbers (§6 decision #1, option A, used as the default until the instructor confirms).
Distractors prefer one typical mistake (e.g. `(4 + 6) × 3` for `4 + 6 × 3`, a neighbouring times-table result,
the wrong operation) plus near misses (±1, ±2, ±10, ±100), and never a pair mirrored around the answer, so the
correct answer isn't simply "the middle number".
- **Done when:** all tests pass for all 10 level configs.

### Phase 3 — Question Zone
- [x] `QuestionZone` chunk: trigger, 3 `AnswerPlatform`s, recovery floor (`Prefabs/Chunks/QuestionZone.prefab`)
- [x] Platform placement based on time limit (2.1)
- [x] Correct/wrong feedback (glow, crumble, highlight the correct answer)
- [x] `QuestionPanel` UI (question, countdown bar, result text)
- [x] `QuestionDirector` (owns the generator + adaptive tier) and `GameEvents` (`QuestionShown`, `QuestionResolved`)

Verified in Play mode (`Scenes/Sandbox_QuestionZone`, Level 5 questions) with a scripted bot stepping frame by frame:
correct (untimed), wrong (4 s), missed (3 s) and correct (2 s) all resolve as expected; timer bar is half full at half time;
tier drops Normal → Easy after the wrong answer. Bug found and fixed: jumping up *through* a one-way platform counted
as landing on it.
- **Done when:** one question works end to end, in both the correct and wrong cases.

### Phase 4 — Game Systems & HUD
- [x] `ScoreSystem` (+ pure `ScoreCalculator`, 3 EditMode tests), `LivesSystem`, `GameManager` state machine, more `GameEvents` (question timing lives in `QuestionZone` + `QuestionPanel`)
- [x] Obstacles (spikes), gaps with kill zone + checkpoint respawn, invincibility frames with flashing
- [x] HUD (hearts, level, score, streak, pause button), Pause / Game Over / Level Complete overlays
- [x] Pause via button or Esc/P; taps on buttons don't make the player jump
- [x] Gray-box level `Scenes/Sandbox_Level` (4 zones, 5 spikes, 3 gaps, checkpoints, finish flag)

Verified in Play mode with a frame-stepping bot: a winning run scores exactly 500 + 575 + 600 + 625 = 2300 and shows
Level Complete with 3 lives; a losing run (wrong answer → fall → spike) goes 3 → 2 → 1 → 0 lives, respawns at the
checkpoint, and shows Game Over; Play Again / Try Again reload the level; the pause button freezes time and blocks jumps.
Not automatable (Unity ignores simulated keyboard/mouse input while the Game view is unfocused), so test by hand:
Esc/P pausing, and that clicking the pause button doesn't also jump.
- **Done when:** a full gray-box level can be won or lost.

### Phase 5 — Levels & Progression
- [x] `LevelConfig` assets for levels 1–10 (table in 2.4)
- [x] `LevelBuilder` assembles chunks from the config (layout from the pure `LevelPlanner`)
- [x] `SaveSystem`, Level Select with locks, stars and best scores, Main Menu

Done as IMPROVEMENT_PLAN.md Stage A. Phases 6–8 continue there as Stages B–F.
- **Done when:** you can play from Level 1 to Level 10, with unlocks saved between sessions.

### Phase 6 — Mobile
- [ ] Touch input through `InputReader`, on-screen pause button
- [ ] Safe-area UI scaling (notches), landscape lock
- [ ] Android build tested on a real phone
- **Done when:** the game is fully playable on a phone at a stable frame rate.

### Phase 7 — Art, Animation, Audio
- [ ] Character sprites and animations (run, jump, fall, hurt)
- [ ] Platform/tile art, background parallax
- [ ] SFX (jump, correct, wrong, life lost, level up) and music
- **Done when:** no gray boxes remain.

### Phase 8 — Polish & Playtesting
- [ ] Tune run speeds, time limits, and number ranges using real playtesters
- [ ] Bug fixing and performance pass
- [ ] Final PC and Android builds
- **Done when:** the game is ready to present.

---

## 5. 💡 Improvement Ideas (optional, not in approved scope)

These add to the approved design without removing anything. Check with the instructor before treating any of them as required.

### High value, low effort (recommended)
1. **"Show the correct answer" on mistakes:** already part of 2.1. This is the single most important learning feature, because the player sees the right answer instead of just losing a life.
2. **Tutorial level (Level 0):** teaches jumping and how answer platforms work with very easy questions and no lives lost.
3. **Star rating per level (1–3 stars):** based on accuracy and lives remaining. It gives players a reason to replay.
4. **Juice:** screen shake on wrong answers, particles on correct ones, combo sound pitch rising with streak. Cheap to add and makes the game much more fun.
5. **Streak display:** a "×5 STREAK!" popup rewards quick math visually.

### Medium effort
6. **Power-ups** (picked up on the run):
   - 🛡 *Shield*: the next wrong answer costs no life
   - ⏱ *Slow-Mo*: run speed halved for one question
   - 🔍 *Hint*: removes one wrong platform (good for struggling players)
7. **Math stats screen:** accuracy per operation (+, −, ×, ÷) and average answer time. This shows players and teachers where the weak spots are, which strengthens the educational pitch.
8. **Practice / Relaxed Mode:** no lives, no timer, pick an operation. Good for learning and for accessibility.
9. **Themed worlds:** a visual theme per operation group (e.g. Addition Meadow → Multiplication Mines → Division Desert), which gives levels identity and makes progression visible.
10. **Boss Gate at Level 5 and 10:** a finale of 3 quick questions in a row before the finish flag.

### Larger / stretch
11. **Endless Mode** with a local high-score table, where difficulty keeps rising until you fall.
12. **Daily Challenge:** a fixed random seed per day, so everyone gets the same questions.
13. **Cosmetic skins** bought with coins collected during runs.
14. **Language toggle** (e.g. English / Filipino) for menus and instructions.
15. **Accessibility options:** larger number text, colorblind-safe platform colors (don't rely on red/green alone), adjustable timer multiplier.

---

## 6. Open Decisions ❓
| # | Question | Recommendation |
|---|---|---|
| 1 | Level 10 example produces a decimal (`97.6`) | Whole-number answers only (option A in 2.4) |
| 2 | What happens physically on a wrong answer? | Platform crumbles → recovery floor → keep running (2.1) |
| 3 | Questions per level | 8 (tune by playtest) |
| 4 | Starting lives / can lives be regained? | 3 lives; optionally +1 life every 10-answer streak |
| 5 | Mobile orientation | Landscape |
| 6 | Art style | Pixel art (simple to produce, readable on small screens) or flat vector |

---

## 7. Playtest Checklist
- [ ] Can a new player understand "jump on the right answer" without being told?
- [ ] Are numbers readable on a small phone at full run speed?
- [ ] Do time limits at Level 10 feel hard but fair?
- [ ] Is losing a life clearly communicated?
- [ ] Do players see and remember the correct answer after a mistake?
- [ ] Does adaptive difficulty kick in without feeling like a sudden spike?
