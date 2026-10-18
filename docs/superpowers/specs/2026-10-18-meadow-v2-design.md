# Meadow v2 — Slopes, Scenery and a Real Background — Design

**Date:** 2026-10-18 · **Author:** Marvin Superales, written up with Claude
**Status:** awaiting approval · **Replaces the look of:** `2026-10-18-meadow-world-design.md` (its rules stay)

## 1. Intent

The first Meadow works but looks like stair steps and floating strips. Marvin wants it to look like Kenney's own
platformer art (two reference pictures): rolling grass hills with **slopes**, platforms that look like real objects,
things to collect and dodge, and a **painted, layered background**. Still simple and readable, still one jump button.

## 2. Decided

| Topic | Decision | Whose call |
|---|---|---|
| Background | `Background Elements Remastered` **ColorGrass** look, built as parallax layers: sky colour → far hills/trees → near hills → drifting clouds | Marvin (picked ColorGrass) |
| Ground shape | Rolling hills with **ramps** (short 45° and long gentle grass ramps) the player runs up and down; steps/cliffs stay but with rounded corner tiles | Marvin |
| Water | Some dips are **water pools** instead of bare pits; falling in = −1 heart, respawn at the last checkpoint (same as a pit today) | Claude |
| Platforms | Look like objects: grass ledges on pillars, wooden **bridges**, **"!" blocks** — all one-way like today | Marvin |
| "!" blocks | Platform + decoration only (no bump-for-coin mechanic yet) | Claude |
| Coins | Gold coins along jump arcs and on high routes, **+10 points each**, coin counter in the HUD; collected coins count toward the level score (not toward stars) | Marvin (Claude proposed values) |
| Bees | Fly a small up-down loop in the air at jump height; touching one costs a heart. Slimes, snails, saws stay | Marvin |
| Scenery | Background Elements props (trees, bushes, fences, a distant house) in a layer just behind the play area; ground props (grass, rocks, mushrooms, signs) stay on the terrain | Marvin |
| Kept as-is | Encounters (0.5×, full control, no gaps inside), 5 levels, every level proven finishable by the planner + bot | Marvin (earlier) |

## 3. Goals, non-goals, anti-goals

**Goals**
- A Meadow stretch reads like the references: rolling hills, a few objects to hop on, coins in the air, a bee or slime,
  trees and hills behind.
- Slopes feel smooth: the runner keeps its speed up and down ramps, never stutters, bounces or launches off a crest.
- Every level stays generated, deterministic and finishable (the jump plan now includes slopes, water and bees).

**Non-goals**
- Desert redo (levels 4–5) — next, reusing this system with sand/desert pieces.
- Bump-from-below blocks, moving platforms, keys/doors, enemies you stomp.
- A new art pack (everything comes from packs already in `Kenney/`).

**Anti-goals**
- Clutter: background props or coins that hide a hazard or the jump path on a phone.
- Slopes that make jump timing unpredictable (a jump started on a ramp must still clear what the planner promised).
- Coins that tempt the player into an unavoidable hit (a coin is always on a safe arc).

## 4. How it fits

- **Background:** replace the three-band parallax content with ColorGrass layers from `Background/Backgrounds/Elements`
  (sky colour, `hills`/`hillsLarge`, trees, `cloudLayer1/2`). `ParallaxLayer` and `BackgroundTheme` stay; the theme
  grows to hold any number of layers (sprite + follow + anchor) so the Desert can reuse it.
- **Terrain:** `LevelPlanner` gains **ramp** pieces (rise or fall over 1 tile with a short ramp, or over 2 tiles with a
  long ramp) and **water** pits; `Ground` becomes a height profile the builder turns into tiles + an `EdgeCollider2D`
  surface (so ramps are smooth). `JumpReach` / reachability checks treat a ramp as walkable ground.
- **Player on slopes:** `PlayerController` follows the ground's slope (keeps horizontal speed, stays glued on crests
  and dips, no bounce). Physics stays in `FixedUpdate`; the ground check accepts sloped surfaces at the feet.
- **Platforms:** `FloatingPlatform` gets looks (grass ledge on a pillar, bridge, "!" block row) chosen per piece.
- **Coins:** new `Coin` pickup + `GameEvents.CoinCollected`; `ScoreSystem` adds +10; HUD shows a coin counter.
  Placed by the planner along the jump plan's arcs and on high routes.
- **Bees:** new `Bee` hazard (Obstacle) flying a small vertical loop; the planner places it where the jump plan passes
  under or over it with margin.
- **Scenery:** `DecorationSet` gains a second, background-depth list (trees, bushes, fences, houses) drawn behind the
  terrain at a slight parallax.

- **Credits:** add *Background Elements Remastered* (CC0) to `Art/ThirdParty/CREDITS.md` and commit that pack; the other
  untracked packs (`Additional_Pack`, `Emotes`, `Enemies`) stay uncommitted unless a piece of them gets used.

## 5. Testing

- **EditMode:** ramps never exceed the walkable angle; every piece (incl. ramps, water, bees, coins) is reachable/clear
  along the jump plan; coins sit on safe arcs; scenery and coins never overlap hazards; same seed → same level.
- **PlayMode (one filtered run per task):** the runner keeps speed and stays grounded running up and down ramps; water
  costs one heart and respawns; a coin adds 10 points; all 5 levels finishable by the bot with all answers right.

## 6. Left to the builder

Exact slope tile mapping, ramp frequency per level, coin counts, bee path size and speed, background layer speeds, prop
mix and density, HUD coin icon placement.
