# Random Encounters — Design

Date: 2026-10-19 · Status: draft for Marvin's approval · Builds on the enemy-encounter rework
(`2026-10-18-enemy-encounter-design.md`), which stays the base: question + 4 choices, Focus Zone slow-motion, first
pick locked, right answer = shockwave, the attack is the timer.

## Intent

Every encounter currently looks the same: the purple alien flies in and shoots a fireball. Marvin's storyboard
("Random Encounters") makes each question meet one of four enemies at random, each attacking in its own way, so the
encounters feel varied and alive. Obstacles that cost hearts outside encounters were removed (2026-10-19), so the
encounters are now the whole challenge.

## The four enemies (Marvin)

Each encounter picks one at random. The question, answer panel, slow-motion and scoring are unchanged.

| Enemy | Appears | Attack (the timer) | Right answer | Wrong / no answer |
|-------|---------|--------------------|--------------|-------------------|
| **Alien** (current) | Flies in to a **random spot anywhere in the sky on screen**, always airborne | Shoots a fireball that reaches the runner exactly when time runs out | Shockwave: alien and fireball destroyed | Fireball hits: −1 heart |
| **Bee** | Flies in ahead of the runner | Charges straight at the runner, arriving when time runs out | Shockwave: bee destroyed | Bee bites: −1 heart |
| **Frog** | Sits on the ground ahead | Leaps at the runner, landing on it when time runs out | Shockwave: frog destroyed | Frog lands on the runner: −1 heart |
| **Fly** | Flies in ahead | Buzzes toward the runner | Shockwave: fly destroyed | **No bite yet: the fly starts following the runner** |

### The following fly (Marvin)

- A fly that follows stays near the runner until the **next encounter**.
- Next encounter answered **right**: that encounter's shockwave also kills the following fly → no hearts lost.
- Next encounter answered **wrong** (or missed): that enemy's attack hits **and** the fly bites → **−2 hearts** (it
  "adds up"). After biting, the fly **flies away**.
- **Claude's calls (marked as guesses):**
  - The following fly can't hurt the runner by touching it; it only bites at the next encounter's result.
  - While a fly is following, the next random pick is **not** another fly (so two flies never stack).
  - If the level ends (finish flag) with a fly still following, it flies away without biting; if the runner loses
    their last heart first, the game is over as usual.
  - The fly bites at the same moment the next enemy's attack lands, so both hearts go at once with one hurt flash.

## Scenery change (Marvin)

The harmless scenery life must never look like an enemy: **harmless flies and frogs are removed** from the Meadow and
Desert life sets. Snails, ladybugs, mice, worms, flags and torches stay. (Harmless bees and slimes were already
excluded.)

## Goals

- One of four enemies per encounter, chosen at random but **reproducibly** per level seed (same level → same order),
  so tests and the bot can rely on it.
- Each attack still arrives exactly when the question's time runs out (real seconds, same in slow-motion), so the
  timer the player sees and the attack always agree.
- Hearts are still lost only through `PlayerHit`; the fly's extra bite is a second `PlayerHit`.

## Non-goals

- No new question types, scoring or star rules (the extra fly heart counts like any other lost heart).
- No change to level layout, terrain, coins or scenery other than removing harmless flies and frogs.

## Anti-goals

- An enemy that can't be read in time: every enemy must be clearly visible on screen before its attack starts, and
  never hidden behind the question panel.
- An attack that lands early or late compared with the timer.
- A following fly that hurts the runner without a wrong answer at the next encounter.
- Random picks that make a level impossible or change between plays of the same level.

## Constraints

- Unity 6 / URP and the project conventions in CLAUDE.md (GameEvents for communication, GameManager owns time scale,
  pure testable maths, EditMode + one filtered PlayMode run per task, Editor work through `Unity_RunCommand`).
- Kenney New Platformer Pack art in the current style (`bee_a/b`, `frog_idle/jump`, `fly_a/b`, the existing alien).
- The planner's reserved encounter ground (fly-in + wind-up + exit buffer + limit × focus) must still fit every enemy.

## Left to the builder

Exact fly-in paths, the alien's random-spot range (inside the visible sky, clear of the question panel and the
runner), charge/leap curves, the following fly's hover offset and wobble, sounds and hurt feedback, and how the random
pick is seeded.

## Done when

- Captures show each of the four enemies in an encounter, and the alien at different sky spots.
- EditMode tests: the pick is deterministic per seed and never gives a fly while one follows; each attack's arrival
  time equals the question's time limit; the following-fly rules (killed on right, −2 then gone on wrong, gone at the
  finish).
- PlayMode: `EncounterTests` extended for bee/frog/fly and the following-fly cases; `AllLevelsTests` still passes
  (every level finished with all answers right).
