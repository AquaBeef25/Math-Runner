# Meadow World (Levels 1–3) — Design

**Date:** 2026-10-18 · **Author:** Marvin Superales, written up with Claude
**Status:** awaiting approval · **Builds on:** `2026-10-18-enemy-encounter-design.md`

## 1. Intent

Make the run feel alive and Geometry Dash-like instead of a flat strip with the odd spike. The ground rises and falls,
there are blocks and platforms to hop onto, sometimes a high and a low route to choose between, a few moving
obstacles, and simple decoration (grass, flowers, bushes, fences). It should still **look simple and clean**: the same
Kenney grass tiles, nothing busy. Enemy encounters stay, but the player now keeps control during them and navigates
while answering.

The game is cut to **5 levels for now** (Marvin will extend it after consulting his adviser): **levels 1–3 are the
Meadow**, **levels 4–5 the Desert**. This design covers the Meadow; the Desert gets its own short design later.

## 2. Decided rules

| Rule | Decision | Whose call |
|---|---|---|
| Level count | 5 levels in Level Select; levels 6–10 stay as data but are removed from the catalog | Marvin |
| Worlds | Levels 1–3 Meadow (Hills background, grass terrain); 4–5 Desert (Desert background; sand terrain later) | Marvin |
| Terrain | Not flat: steps up/down, floating platforms, pillars, gaps, and high/low route splits | Marvin |
| Look | Simple: existing Kenney grass tiles + light decoration, no clutter | Marvin |
| Control during encounters | **No autopilot** — the player can jump while answering; terrain and obstacles continue during encounters | Marvin |
| Focus Zone speed | **0.5×** (was 0.2×), so jumping still feels like platforming. Answer time stays the same in real seconds | Marvin |
| Obstacles | Spikes, gaps, walking slime/snail, saw. All clearable with the one jump button | Marvin (Claude proposed) |
| Math per level | Levels 1–5 keep their current math (Addition … Times tables) | Claude |
| No pits during encounters | Encounter stretches use steps, platforms, spikes and walkers but **no gaps**, so a fall/respawn never lands mid-question | Claude |
| Coins | Optional stretch goal: coins on high routes, +10 points each | Claude |
| Answer buttons | Move from the bottom-right to a row just under the question panel at the top, so they never hide the ground and hazards ahead | Claude |
| Checkpoints | Placed on top of the terrain at their spot (not at a fixed height), so a fall respawns the player on solid ground | Claude |

## 3. Goals, non-goals, anti-goals

**Goals**
- Every Meadow level has varied height and at least two route splits.
- Each level is generated from its `LevelConfig` (deterministic per seed), gets harder from 1 to 3 (more hazards,
  taller steps, faster run), and is **always finishable** with the single jump input.
- Decoration makes the world feel lived-in but never blocks, hides or looks like a hazard.
- Enemy encounters work on uneven ground with the player in control.

**Non-goals (this round)**
- Desert terrain/obstacles (levels 4–5 just get the Desert background for now).
- Slopes the player runs up (Kenney ramps), moving platforms, bees/flying hazards, double jump.
- Hand-built levels or a level editor.

**Anti-goals**
- A jump that can't be made, a platform that can't be reached, or a route that dead-ends.
- Clutter: decoration so dense that hazards are hard to spot on a phone.
- A hazard or step placed where the player can't see it coming in time (must be on screen ≥ ~1 s ahead).
- Encounters becoming unfair: an unavoidable hit that comes from terrain rather than the math.

## 4. The pieces

**Terrain pieces** (heights in whole tiles = 1 unit; all sizes checked against the real jump via `JumpMath`):
- **Step up / step down:** ground rises or falls 1–2 tiles. A step up is never taller than a full jump can clear with
  margin.
- **Platform:** a floating grass strip (`terrain_grass_horizontal_*`) 2–4 tiles wide, reachable from the ground.
- **Pillar:** a narrow 1–2-tile-wide column (`terrain_grass_vertical_*`) to hop over or land on.
- **Gap:** a pit sized for one jump (existing).
- **Route split:** a stretch with a high path (chain of platforms) above a low path (ground with a hazard). Both are
  safe if played well; the high path may hold coins (stretch goal).

**Obstacles:**
- **Spike** (existing), also placeable on platforms.
- **Walker:** a slime or snail (`slime_normal_walk_*`, `snail_walk_*`) that patrols slowly back and forth on a flat
  stretch; touching it costs a heart like a spike. Jump over it.
- **Saw:** `saw_a/b` spinning on the ground or a platform; touching it costs a heart.

**Decoration** (no colliders, behind the player): `grass`, `bush`, `rock`, `mushroom_red/brown`, `fence`, `sign`, a few
`hill_top` props, placed on ground tops away from hazards, seeded so each level always looks the same.

## 5. How it fits the existing code

- **`LevelPlanner`** grows from "ground + spikes + gaps" into a **segment generator**: it lays out terrain pieces with
  heights, obstacles, encounter stretches and decoration spots, and also records a **reference jump plan** (where a
  jump must happen to follow one safe route). It stays pure and deterministic, so EditMode tests can prove every piece
  is reachable and every level finishable.
- **`LevelBuilder`** instantiates the new pieces (new prefabs: Platform, Pillar, Walker, Saw, Decoration) the way it
  does ground and spikes today.
- **`LevelConfig`** gains a few difficulty knobs (e.g. max step height, route-split count, walker/saw chance,
  decoration density); levels 1–3 get Meadow values, 4–5 Desert background.
- **Encounters:** `EnemyEncounter` stops calling `BeginAutopilot` / `EndAutopilot`; `EncounterSettings.FocusScale`
  becomes 0.5; the planner reserves the same encounter stretch length (fly-in + wind-up + exit + limit × 0.5) but fills
  it with gap-free terrain. The enemy's hover point follows the player's height smoothly so it stays on screen.
- **`LevelBot`** follows the planner's reference jump plan instead of reacting to spikes/gaps only, so PlayMode tests
  (and `AllLevelsTests`) keep proving levels are finishable.
- **`LevelCatalog`** lists 5 levels.
- The hand-built `Sandbox_Level` stays as it is for its existing tests.

## 6. Testing

- **EditMode:** every step/platform/pillar is within jump reach; every gap is jumpable; no hazard inside an encounter
  stretch is a gap; decorations never overlap hazards or the jump path; same seed → same level; difficulty rises from
  level 1 to 3.
- **PlayMode (kept to one filtered run per task):** the bot finishes levels 1–5 with all answers right; jumping works
  during a Focus Zone; a walker/saw hit costs one heart.

## 7. Left to the builder

Exact piece widths and frequencies per level, decoration density and mix, walker speed and patrol length, saw size,
the enemy's height smoothing, and whether coins make it into this round (only if everything else is done).

## 8. Doc updates

- GAME_DESIGN §2.1/§2.2: encounters no longer use autopilot; Focus Zone 0.5×; §2.4/§4: 5 levels for now (Meadow 1–3,
  Desert 4–5). §1 is unaffected (run, jump, avoid obstacles; real-time, time-limited).
- CLAUDE.md: remove "never put hazards inside an encounter / autopilot" notes; describe the segment generator.
