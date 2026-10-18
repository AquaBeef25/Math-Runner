# Enemy Encounter & Focus Zone — Design

**Date:** 2026-10-18 · **Author:** Marvin Superales (storyboard + refined spec), written up with Claude
**Status:** awaiting approval · **Replaces:** the answer-platform Question Zone

## 1. Intent

Make the math moments feel like action instead of a quiz stop. Today a question appears, the player taps an answer and the
runner auto-jumps onto that answer's platform. The new version turns every question into a short fight: a flying enemy
shoots at the player, time slows down ("Focus Zone"), and answering correctly before the slow bullet arrives blasts the
enemy away. The bullet is both the threat and the countdown, so the player always *sees* how much time is left.

The instructor approved this change proposal (per Marvin, 2026-10-18). §1 of GAME_DESIGN.md is updated to match (see §9).

## 2. The player's experience (storyboard scenes 1–6)

1. **Running (Scene 1).** Unchanged: auto-run, jump spikes and gaps, hearts lost to hazards.
2. **Enemy arrives (Scene 2).** A flying enemy enters from the top right, settles ahead of and above the player, shakes
   briefly (wind-up) and fires. The bullet is visible and travels toward the player.
3. **Focus Zone (Scene 3).** The instant it fires, the whole world slows to ~20 % speed: player, bullet, enemy,
   particles, animations. Nothing freezes. Screen edges darken and colours fade slightly; a soft glow surrounds the player.
4. **Math panel (Scene 4).** Question in a large box at the top, **four** big answer buttons along the bottom, the world
   still visible behind. A bar empties as the bullet closes in. Tap/click (or keys 1–4); only the first pick counts.
5. **Correct (Scene 5).** Normal speed snaps back, a white-gold shockwave ring bursts from the player, the enemy pops in
   a puff, the bullet fizzles out. No heart lost. Existing confetti, score popup and sound play. Running resumes.
6. **Wrong or too slow (Scene 6).** Normal speed snaps back, the bullet hits (red flash, shake, −1 heart, bullet removed),
   the enemy flies off to the top right. Game over if that was the last heart; otherwise running resumes.

## 3. Goals, non-goals, anti-goals

**Goals**
- Every question in every level is an enemy encounter (they replace answer platforms entirely).
- Slow-motion is consistent: everything in the world slows together, and pause still works on top of it.
- The bullet is the timer: if it reaches the player before an answer, that counts exactly like a wrong answer.
- Everything already built keeps working off the same events: score/streak/stars, difficulty, feedback FX, sounds,
  save data, all 10 levels.

**Non-goals (this round)**
- New art packs or a hand-drawn jetpack/gun (the purple Kenney character is the enemy, by Marvin's choice).
- Multiple enemies at once, enemies outside encounters, enemies the player can jump on.
- Music, mixer, volume settings (still open from B4).

**Anti-goals (would make it a failure even if it "works")**
- The game freezes or stutters during slow-mo, or unpausing during a Focus Zone returns to full speed.
- The player can lose a heart twice for one wrong answer, or the bullet hits twice.
- A hazard (spike/gap) can hurt the player during an encounter.
- Answers are hard to read or tap on a phone-shaped screen.

## 4. Decided rules

| Rule | Decision | Whose call |
|---|---|---|
| Relation to platforms | Encounters **replace** answer platforms in all levels | Marvin |
| Too slow | Bullet reaches player = same as wrong (−1 heart, streak reset, counts as a miss) | Marvin |
| Enemy look | Purple Kenney character (`character_purple_*`), flipped to face the player, bobbing, small flame particle under it as the "jetpack" | Marvin (Claude proposed) |
| Bullet look | Kenney `fireball` sprite, slowly spinning | Claude |
| Answers | 4 choices (generator already supports a choice count); keys 1–4 | Spec |
| Jumping in the Focus Zone | Not allowed: the player runs slowly on flat ground (autopilot, as today) | Claude |
| Time limit | Real seconds from shot to bullet impact = the level's time limit; "untimed" questions use the level's untimed lead (≥ 6 s), so every question has a generous deadline on early levels and less on later ones | Claude |
| Slow-mo amount | ~0.2× world speed, tunable | Claude |
| Life loss | Wrong/missed answers cost a heart **when the bullet hits**, not when the answer is judged (no double loss) | Claude |

## 5. How it fits the existing code

**Replaced**
- `QuestionZone` + `AnswerPlatform` (+ the `QuestionZone` prefab's platforms) → a new **`EnemyEncounter`** zone that
  runs the sequence: trigger → enemy fly-in → fire → Focus Zone → resolve → enemy exit.
- `Sandbox_QuestionZone` scene → an encounter sandbox (4 encounters: generous / 4 s / 3 s / 2 s, like today).

**Kept and reused**
- `QuestionDirector` / `QuestionGenerator` / `DifficultyTracker`: `QuestionGenerator` is created with `choiceCount: 4`.
- `GameEvents`: `QuestionShown` (time limit = bullet travel time), `AnswerChosen`, `AnswerLocked`, `QuestionResolved`
  keep their meaning, so ScoreSystem, ProgressTracker, star ratings, FeedbackFX, AudioManager and DifficultyTracker
  need no logic change. `QuestionResult.TimeRemainingFraction` = share of the bullet's flight left at the pick (feeds
  the existing speed bonus).
- `InputReader` answer keys: 3 → 4. `AnswerButtons`: 3 → 4 buttons, re-laid out for 4.
- `QuestionPanel`: same panel; its timer bar follows the bullet's remaining flight.
- `LevelConfig` + `LevelPlanner`: same question counts, hazards between questions and timed ratio. The planner reserves
  a flat, hazard-free stretch for each encounter instead of the platform zone (length = fly-in + slowed focus distance +
  recovery, all in seconds × run speed, so it stays pure and EditMode-testable).

**Changed**
- `LivesSystem` stops taking a life on non-correct `QuestionResolved`; the bullet's `PlayerHit(HitCause.Bullet)` takes
  it instead (one heart per wrong/missed answer, never two).
- `PlayerHealth` gets a bullet-hit entry point that **ignores the post-hit invincibility** (a spike just before the
  encounter must not cancel the consequence) and does **not** respawn the player.
- `GameManager.SetState` no longer writes `Time.timeScale` directly; it tells the time-scale owner (below).

**New**
- `TimeScaleController` (or the time part of `GameManager`): the single owner of `Time.timeScale`. Effective scale =
  0 when paused, else focus scale (0.2) during a Focus Zone, else 1. Scales `Time.fixedDeltaTime` with it so physics
  stays smooth. Raised via new GameEvents `FocusStarted` / `FocusEnded`.
- `FlyingEnemy` (fly-in, hover-bob, wind-up, fire, retreat, destroyed) and `EnemyBullet` (homes on the player so a
  wrong answer always lands; damages once, then removes itself).
- `HitCause.Bullet` for the bullet's damage, so feedback can tell it apart.
- Focus look: a second URP Volume (vignette + lower saturation) whose weight fades in/out; a glow sprite on the player.
- `Shockwave` effect: expanding ring that, on a correct answer, destroys the enemy and the bullet.
- Sounds: fire, shockwave, enemy pop, slow-mo in/out added to `SoundSet` (Kenney Interface Sounds where something fits).
- `LevelBot` + PlayMode tests rewritten for encounters (pick right/wrong/none per question).

## 6. Timeline of one encounter (all in real seconds unless noted)

```
trigger ─ fly-in (~1.2 s, normal speed) ─ wind-up (~0.4 s) ─ FIRE → Focus Zone starts
        Focus Zone: world at 0.2×, bullet flight = level time limit T (real seconds)
        ├─ correct pick at t < T → focus ends → shockwave → enemy + bullet destroyed → QuestionResolved(Correct)
        ├─ wrong pick at t < T  → focus ends → QuestionResolved(Wrong) → bullet hits (−1 heart) → enemy retreats
        └─ no pick by T         → bullet hits at 0.2× → focus ends → QuestionResolved(Missed) (−1 heart) → enemy retreats
recovery run (flat) → next hazards
```
The answer is locked at the first pick (`AnswerLocked`), matching today's behaviour.

**Bullet timing (builder's guarantee):** the bullet steers toward the player each frame with speed = remaining distance ÷
remaining flight time, so it arrives exactly when its clock runs out, however the player moves. Its clock runs on
scaled time, so in slow-mo its remaining *real* time stretches by 1 ÷ 0.2, and after a wrong pick (normal speed again)
the rest of the flight plays out quickly. The panel's timer bar shows remaining flight ÷ total flight.

## 7. Testing

- **EditMode:** planner reserves hazard-free encounter stretches of the right length; bullet flight-time maths; time-scale
  combination rules (pause × focus); 4-choice question generation.
- **PlayMode (keep to one filtered run per task):** right answer destroys enemy + bullet, no heart lost; wrong answer
  loses exactly one heart from the bullet; no answer = bullet hit; pausing in the Focus Zone and unpausing returns to
  0.2×, not 1×; `AllLevelsTests` (every level finishable with all answers right) once at the end.
- `Sandbox_Level` and its `LevelRunTests` (exact layout, 2300 score) are rebuilt for encounters; the expected score is
  recomputed rather than kept.

## 8. Left to the builder

Exact fly-in path and hover height (enemy must stay on screen and not cover the panel), bullet and ring sizes, Focus
Zone colour grading values, panel styling (Kenney UI Pack diamonds/boxes welcome), sound picks, and how the old
platform scripts are retired (delete vs. keep unused — prefer delete with `git rm`).

## 9. GAME_DESIGN.md §1 update (instructor-approved change)

- Gameplay: "Jump onto the platform showing the correct answer." → "When a flying enemy fires, time slows down (Focus
  Zone): choose the correct answer before the bullet arrives to destroy the enemy."
- Mechanics → Math Choices: "Choose the correct answer during the Focus Zone (tap/click or keys); wrong or too slow and
  the bullet costs a life."
- Core loop: `RUN → ENEMY → FOCUS → ANSWER → REWARD → LEVEL UP`.
- A note under §1 recording that the instructor approved this change on 2026-10-18.
