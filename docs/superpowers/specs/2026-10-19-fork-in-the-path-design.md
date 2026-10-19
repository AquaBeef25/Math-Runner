# Fork in the path — Design

Date: 2026-10-19. Status: approved by the user (plan `docs/superpowers/plans/2026-10-19-fork-in-the-path.md`).

## Why

The instructor wants **both** the enemy encounters and the answer platforms from the original proposal, but the first platform version (three floating stone platforms, removed 2026-10-18) "didn't satisfy him": he wants something creative. The user chose **Fork in the path**: the answer is chosen *with the jump itself*.

## What the player sees

- A question comes up on the chalkboard as the road ahead splits into **three routes at different heights**, each with a chalk answer sign:
  - **high ledge**: reached by a full, held jump;
  - **middle ledge**: reached by a short hop;
  - **low road**: no jump, dipping into a hollow.
- The world slows to **0.5×** while the fork approaches, until the runner commits to a route.
- **Right route** → a treasure chest at its end pops coins (+10 each, like coins today).
- **Wrong route** → spikes pop up on it and the runner loses **one heart**, with no chest. (Mud that slowed the runner until 2026-10-19; the user asked for spikes instead.)
- **Not jumping** is a real answer: it picks the low road. Forks have no "too slow/missed"; on timed levels the countdown is the distance to the split.
- The board shows the question and three chalk circles captioned "jump high / hop / stay low", in the same colours as the signs. They can't be tapped: you answer by jumping. The hint reads "Jump to answer!".

## Where forks appear

| Level | Forks | Encounters |
|---|---|---|
| 1–2 | every question | none |
| 3 | about half | about half |
| 4–5 (Desert) | about a third | about two thirds |

Per-level knob `LevelConfig._forkShare`. Each question picks its kind from its own random stream, so the rest of the level never changes.

## Rules kept from the game

- Questions, difficulty, scoring (including the speed bonus), streaks and the following fly all work the same for forks and encounters.
- Hearts are lost only through `PlayerHit`.
- `GameManager` is the only writer of the time scale.
- A fork and an encounter are never in focus at the same time.

## Decided during the build

- **Heart timing:** a wrong route costs its heart the moment the route is taken. The spikes are only the look of it, so jumping over them can't dodge the heart (found in the final review).
- **No speed bonus at forks:** the runner always takes the whole countdown to reach the split, so a fork never earns the "fast answer" bonus that encounters give. Right answers still score their normal points and streak. Changing this would need a different rule, for example a bonus for the route chosen with the earliest jump. That is a possible follow-up.

## Geometry (T = ground height at the fork)

| Route | Height | Built from |
|---|---|---|
| Low road | T−1 | a hollow: ramp down, flat, ramp up |
| Middle ledge | T+1.0 | one-way platform |
| High ledge | T+2.7 | one-way platform, starting about 1.5 units after the middle ledge |

- A tap peaks at about 1.7: it reaches the middle ledge, never the high one.
- A full jump peaks at 3.2 (2.72 with the 0.85 safety margin): it reaches the high ledge.
- Head clearances: low road to the middle ledge 0.6; middle ledge to the high ledge 0.3.
- The routes rejoin on flat ground, with a checkpoint after the rejoin.
- Fallback heights if play shows the margins are too tight: 2.6 and 0.9.

## Non-goals

- No change to the encounters themselves.
- No new controls: jump is the only input a fork needs.
- No iOS work.
