# Lively World — Harmless Moving Life — Design

Date: 2026-10-18 · Status: draft for Marvin's approval · Follows Meadow v2 and the desert terrain (both on `main`).

## Intent

Marvin's reference pictures (Kenney New Platformer Pack sample scenes) feel alive: flies buzz, critters wander, crates
swing, flags wave. Our levels look right now but feel still between enemy encounters. The goal is a world that is
**nicer to look at**, not harder to play: everything added here is decoration that happens to move.

Encounters will be improved separately, later. This work must not touch them.

## Goals

- Moving life in every level: things in the air, critters on the ground, moving objects and small reactions.
- Meadow (levels 1–3) and Desert (levels 4–5) each get their own fitting mix.
- **Sparse.** Roughly one living thing per screen. The existing ground props (mushrooms, bushes, rocks, cactus…)
  are currently **too crowded and get thinned out** as part of this work (Marvin, 2026-10-18).

## Non-goals

- No new mechanics: no springs, keys/locks, ladders or coin blocks (Marvin chose "looks alive" over "things to play
  with").
- No change to encounters, level layout, jumps, hazards, coins, score or stars.

## Anti-goals (a failure even if it technically works)

- **Anything harmless that can cost a heart, block, push or slow the runner.** Moving life has no solid collider and no
  `Obstacle`; the reference jump plan and `AllLevelsTests` stay exactly as they are.
- **Harmless things that look like hazards.** Bees and slimes are already hazards in this game, so there is no
  "harmless" bee or slime (Claude's call; they would teach the player to ignore real hazards).
- **Clutter:** life or props covering a hazard, a coin, a jump take-off, a gap edge or the question panel; or a level
  that looks busier than the reference pictures.
- Art in a different style. Only the **New Platformer Pack** (and the vector `Emotes`) are used; the older-style
  `Enemies/` and `Additional_Pack/` folders are not.

## What appears (Marvin: "all would be fine")

| Kind | Meadow (1–3) | Desert (4–5) | Behaviour |
|------|--------------|--------------|-----------|
| In the air | fly, ladybug (flying) | fly | Drifts in a slow loop/figure-eight around a spot above the ground, wings flapping (`fly_a/b`, `ladybug_fly`). |
| On the ground | snail, frog, ladybug (walking) | mouse, worm | Walks a short stretch of flat ground and back (`snail_walk_a/b`, `mouse_walk_a/b`, `worm_*_move_a/b`); the frog sits and hops now and then (`frog_idle`/`frog_jump`). The runner passes through them. |
| Moving objects | crate/weight swinging on a chain, waving flag | same | Swinging pieces hang **behind** the play area (backdrop depth, slightly faded) so they never read as an obstacle; the chain comes down from above the top of the screen (Claude's call). Flags wave (`flag_<colour>_a/b`). |
| Water | fish leaping out of water pools | — (desert has no water) | Now and then a fish jumps out of a pool in an arc and drops back (`fish_*`). |
| Reactions | emote bubble over a critter as the runner passes | same | A small "!" / heart / music-note bubble (`Emotes/PNG/Vector/Style 1`) pops over a nearby critter for about a second. Pure visual. |
| Torches | — | flickering torch on some ground spots | `torch_on_a/b`; desert only (Claude's call: torches fit ruins/pyramids better than meadows). |

## Placement rules

- Seeded and deterministic like every other planner output, with **its own random stream**, so adding life never
  changes terrain, hazards, jumps or coins (same pattern as coins/backdrop).
- Ground critters, flags and torches only on **flat ground tops** (never ramps), at least 1.5 units from spikes,
  saws, bees, slime patrols, gap edges, water edges and planned jump take-offs; a walker's whole patrol obeys this.
- Flying life stays clear of the runner's planned jump arcs and of coins.
- Encounter stretches: allowed, but nothing within the screen-top area where the question panel shows.
- **Density:** about one piece of moving life per 15–20 units of level (Claude's proposal for "one per screen"),
  each level's knob in `LevelConfig`. Ground props drop from 2.5 to about 1 per 10 units; backdrop scenery spacing
  widens a little (Claude's proposal). Exact numbers are tuned by looking at captures.

## Constraints

- Unity 6 / URP, existing conventions (CLAUDE.md): pure planner + EditMode tests; visuals built by `LevelBuilder`;
  art via `Unity_RunCommand`, no hand-edited YAML; PlayMode at most one filtered run per task.
- Moving life uses the same time as the world (slows down in the Focus Zone like everything else).
- Nothing new on the `Player`, `Ground`, `Obstacle` or `AnswerPlatform` physics layers for life objects.

## Left to the builder

Exact sprites per critter, speeds, loop sizes, emote timing, colours of flags, chain length, density numbers within
the ranges above, and how the life list is split per world (e.g. a "life set" next to `DecorationSet`).

## Done when

- Levels 1–5 show the mixes above, sparse, in captures of two stretches per world.
- EditMode tests: life is deterministic per seed, never changes the rest of the plan, and keeps its distances from
  hazards, take-offs, gap/water edges and coins; ground life only on flat ground.
- `AllLevelsTests` still passes unchanged (proof that nothing harmless interferes).
