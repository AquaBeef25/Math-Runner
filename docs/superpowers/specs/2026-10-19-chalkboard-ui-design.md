# Chalkboard UI — Design

Date: 2026-10-19. Status: draft for the user's approval.
Mockups: `.superpowers/brainstorm/1003-1792352002/content/chalkboard-screens-v2.html` (option 2 = the chosen question panel).

## Why

The user dislikes the current UI: "the math question and the colors. overall actually." Right now the question sits on a grey see-through panel, the answer buttons are tan, and the menus are plain Unity defaults. None of it matches the Kenney art in the world. The user picked a **chalkboard** look, because the game is about maths and should feel like a fun classroom. They want that look on **every screen**, with the question and its answers **written together on one board** (mockup option 2).

## Goals

- One visual language across every screen: green chalkboard panels in a wooden frame, chalk-style hand-written text on the boards, Kenney UI Pack buttons for anything you press, and Kenney platformer sprites for hearts, coin and stars.
- The question panel becomes one board:
  - the question is written in chalk;
  - a chalk-line timer shrinks as time runs out;
  - the four answers are written in a row on the same board, each inside a hand-drawn chalk circle in its own colour (blue, green, yellow, red, in the order of keys 1–4).
- Make better use of art that is already in the project. The UI Pack (`Art/ThirdParty/Kenney/UIPack`) was imported but never used.

## Non-goals

- No new screens or features: no Settings screen, no volume sliders, no music.
- No change to game rules, scoring, star rules, timings, the encounter flow, input bindings or scene flow.
- No localisation work.

## Anti-goals (a failure even if it works)

- The board or the answers cover the runner, the enemy or the platforms on a phone-shaped screen. The board stays at the top, as the question panel does today.
- Chalk text that kids cannot read quickly, especially digits. Readability beats decoration.
- Taps on HUD graphics that stop being jumps. The existing rule still holds: every non-button graphic has `raycastTarget = false`.
- Tests that start failing because the UI wiring changed. Keep the public hooks the tests use (`Button.onClick`, the 1–4 keys through `InputReader`, `GameManager` methods).

## Decided by the user

- Chalkboard style, on every screen.
- The question panel is mockup option 2: question and answers together on one board, answers circled in coloured chalk.
- Download one free, open-licence hand-written font for the chalk text.

## Screens

**Question board (Game scene and the sandboxes that have the question UI)**
- Top centre, about the width of today's question panel plus answer row.
- Contents, top to bottom:
  - the question, in chalk;
  - a chalk-line timer that shrinks (it turns a chalky red when urgent, like today's urgent colour);
  - four answer circles in a row.
- Each answer circle is a real `Button` (tap/click), and keys 1–4 keep working.
- After a pick:
  - the chosen circle is drawn thicker;
  - the others fade;
  - on resolve, a chalk tick shows on the right answer, and a chalk cross on a wrong pick.
- This replaces today's colour swaps and the separate answer button row. The board then hides as it does today.
- Pop-in and urgent-pulse animations stay; they apply to the board.

**HUD**
- Hearts are Kenney `hud_heart` / `hud_heart_empty` sprites in place of the ♥ text.
- Coins show the `hud_coin` icon plus the count.
- Score, streak and level label are written in chalk on a small board strip.
- The pause button becomes a Kenney round button.

**Main menu**
- The title, the runner sprite, a big green Kenney **Play** button and a grey **Quit** button. These are the same two buttons as today.

**Level select**
- A board titled "Pick a level", with a grid of Kenney square buttons:
  - green = Meadow levels (grass terrain);
  - yellow = Desert levels (the level's `LevelConfig` uses the Sand `TerrainSet`);
  - grey = locked.
- Under each button: the best stars (Kenney star sprites, from the save's `BestStars`) or "New!".
- The level's topic stays visible: its text is shown smaller, on the button or under it.

**Pause, Level complete, Game over**
- The game dims and a board appears with the same buttons as today:
  - **Pause:** Resume / Restart / Levels.
  - **Level complete:** Next / Retry / Levels.
  - **Game over:** Retry / Levels.
- **Level complete** shows 0–3 Kenney stars (filled or empty) and the stats in chalk: correct answers, coins, score, and "New best!" or the best score.
- **Game over** uses gentle wording and shows how many answers were right.

## Fonts and art

- **Buttons and titles:** Kenney Future (in the UI Pack), as a TextMeshPro font asset.
- **Chalk text:** Patrick Hand (Google Fonts, SIL Open Font Licence), as a TextMeshPro font asset. *Builder's call:* this was picked over Gochi Hand because its digits are the clearest for kids. Add it to `Art/ThirdParty/CREDITS.md`, and keep its licence file next to it.
- **Chalkboard frame:** there is no chalkboard sprite in the Kenney packs. Build one 9-sliced sprite: a green board with a wood frame, drawn by us. *Builder's call:* a PNG generated by a small script or a hand-made texture is fine; it is the one new piece of art.
- **Answer circles:** a chalk-ring sprite tinted per colour. Tick and cross: chalk-style sprites, or Kenney's `icon_checkmark` / `icon_cross` tinted chalk-white.
- **Chalk colours** are one shared palette:
  - chalk white #F6F3E8;
  - blue #8FD0FF, green #A6EC9B, yellow #FFE08A, red #FF9D9D;
  - board green #2B5443–#3B6E58;
  - wood #A8743F.
  - The palette lives in one place (a small `UiTheme` ScriptableObject or one static class), not scattered through scripts.

## Constraints

- uGUI + TextMeshPro, as now. Unity Editor work goes through `Unity_RunCommand`; never hand-edit YAML.
- `[SerializeField] private`, namespace `MathRunner.UI`, one class per file.
- `QuestionPanel` and `AnswerButtons` keep listening to the same `GameEvents`.
- `WorldTime.RealDeltaTime` stays the clock for any tweens that must run in slow motion.
- Every scene's EventSystem stays as it is.
- Check the result:
  - with screenshots of each screen;
  - in a phone-shaped Game view (the board must not cover the runner);
  - EditMode tests;
  - one filtered PlayMode run (`EncounterTests` and `LevelRunTests`), because PlayMode runs are token-heavy.

## Left to the builder

- Exact sizes and spacing, the board's 9-slice borders, the animation timings for the tick and cross.
- Whether the answer circles are new child objects of the board or the existing `AnswerButtons` buttons re-parented and re-skinned. The behaviour above is what matters.
- How the chalkboard sprite is produced.
