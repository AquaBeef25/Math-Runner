# Chalkboard UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Re-skin every screen (question board, HUD, overlays, main menu, level select) in the approved chalkboard style, using Kenney UI Pack buttons, Kenney HUD sprites and a chalk handwriting font.

**Architecture:**
- One `UiTheme` ScriptableObject holds the palette, both TMP fonts and the sprites, so nothing is scattered.
- Small, testable pieces carry the new behaviour:
  - pure `AnswerMarks` (which circle gets a tick or a cross);
  - `HeartRow` and `StarRow` components with pure state functions.
- The existing UI scripts (`QuestionPanel`, `AnswerButtons`, `HudView`, `OverlayController`, `LevelButton`) keep their events and buttons and only gain the new visuals.
- The scene objects are rebuilt in the Editor through `Unity_RunCommand`, one scene group per task, and read back afterwards.

**Tech Stack:** Unity 6000.3.11f1, uGUI + TextMeshPro, Unity Test Framework (EditMode + PlayMode).

**Spec:** `docs/superpowers/specs/2026-10-19-chalkboard-ui-design.md`. Mockups: `.superpowers/brainstorm/1003-1792352002/content/chalkboard-screens-v2.html` (question = option 2).

## Global Constraints

- Branch `feature/chalkboard-ui` from `main`. Merge only when the user asks.
- Namespace `MathRunner.UI` for UI scripts, in `Assets/_Project/Scripts/UI/`. One class per file. `[SerializeField] private`. Never write bare `Math.`.
- Never hand-edit `.unity` / `.prefab` / `.asset` YAML. Do Editor work with `Unity_RunCommand`.
- Assign ScriptableObject references through public setters + `EditorUtility.SetDirty`, then read them back.
- Palette (exact values, only in `UiTheme`):
  - chalk white `#F6F3E8`;
  - answer colours, in key order 1–4: blue `#8FD0FF`, green `#A6EC9B`, yellow `#FFE08A`, red `#FF9D9D`;
  - board dark `#2B5443`, board light `#3B6E58`;
  - wood `#A8743F`, wood dark `#6B4522`;
  - chalk urgent `#FF9D9D`;
  - dim overlay `rgba(20,30,40,0.45)`.
- Fonts:
  - **Kenney Future** (`Art/ThirdParty/Kenney/UIPack/Font/Kenney Future.ttf`) for buttons and titles;
  - **Patrick Hand** (Google Fonts, SIL OFL) for chalk text.
  - Both become dynamic TMP font assets.
- Every non-button UI graphic: `raycastTarget = false`.
- Keep these working unchanged:
  - `Button.onClick` → `InputReader.NotifyAnswerChosen(i)`;
  - keys 1–4;
  - the `GameEvents` subscriptions;
  - `WorldTime.RealDeltaTime` for the countdown;
  - the object names and paths the tests use: `PlayButton`, `HUD/Overlays/LevelCompletePanel/Box/Result`, `…/Box/NextLevelButton`, `LevelButton` children `Detail`, and buttons named `Level N`.
- PlayMode: **one** filtered run, in Task 7 only (user rule: it is token-heavy).
- Before every commit, revert `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`. Never commit the untracked Kenney folders (`Additional_Pack/`, `Emotes/`, `Enemies/`, `Background/Vector`) or `Assets/_Recovery`.
- Commits end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Long questions or 3-digit answers** (e.g. `125 + 375 = ?`, answer `500`) must fit the board and the circles. Answer labels and the question text auto-size (TMP `enableAutoSizing`, min 18). Tested in Task 4 (`QuestionBoardTextAutoSizes`).
2. **Fewer than 4 choices:** the unused circles are hidden and the marks skip them. Tested in Task 3 (`MarksIgnoreMissingChoices`) and Task 4 (`UnusedCirclesHide`).
3. **Untimed questions:** the chalk timer line is hidden, exactly as the bar is today. Tested in PlayMode `ChalkBoardPlayTests.UntimedQuestionHidesTimerLine` (written in Task 4, run in Task 7's single PlayMode run).
4. **Phone-shaped screen (1080×2340 landscape → 2340×1080):** the board stays in the top ~30 % of the screen and never covers the runner. Checked with a capture in Task 7.
5. **Pause while the board is up:** the pause board draws above the question board, and the board's circles can't be tapped through it. Tested in Task 6 (`PauseBoardDrawsAboveQuestionBoard`, comparing sibling or sort order).

---

### Task 1: Fonts, sprites and import settings

**Files:**
- Create: `Assets/_Project/Art/ThirdParty/Fonts/PatrickHand/PatrickHand-Regular.ttf`, `OFL.txt`
- Create (generated in the Editor): `Assets/_Project/Art/UI/Fonts/KenneyFuture SDF.asset`, `PatrickHand SDF.asset`
- Create (generated PNGs): `Assets/_Project/Art/UI/chalkboard.png` (9-slice board with wood frame), `chalk_ring.png`, `chalk_line.png`, `chalk_tick.png`, `chalk_cross.png`
- Modify: `Assets/_Project/Art/ThirdParty/CREDITS.md`
- Test: `Assets/_Project/Tests/EditMode/UiAssetTests.cs`

**Interfaces:**
- Produces the asset paths above, used by `UiTheme` (Task 2).

- [ ] **Step 1: Write the failing test** `UiAssetTests`:
  - `ChalkFontsExist`: `AssetDatabase.LoadAssetAtPath<TMP_FontAsset>` finds both SDF assets, and each has `atlasPopulationMode == Dynamic`;
  - `ChalkboardIsNineSliced`: `chalkboard.png` loads as a `Sprite` with every `border` component > 0;
  - `KenneyButtonsAreNineSliced`: `UIPack/PNG/{Blue,Green,Yellow,Red,Grey}/Default/button_rectangle_depth_gloss.png` all have `border == (16,20,16,14)` (L,B,R,T);
  - `ChalkGlyphsIncludeMathSymbols`: Patrick Hand's source font has the characters `0-9 + − × ÷ = ? ✓` checked with `TMP_FontAsset.HasCharacters`. **Ruling allowed:** if `✓` or `−` are missing, the board uses `-` and the tick sprite; assert only what is used.
- [ ] **Step 2: Run** EditMode filtered to `UiAssetTests`. Expected: FAIL (assets missing).
- [ ] **Step 3: Download the font** (approved by the user on 2026-10-19). Get `https://github.com/google/fonts/raw/main/ofl/patrickhand/PatrickHand-Regular.ttf` and `.../ofl/patrickhand/OFL.txt`:
  - first into a new empty scratch folder;
  - then copy them into `Art/ThirdParty/Fonts/PatrickHand/`.
- [ ] **Step 4: Create the TMP font assets in the Editor.** Use `TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic)`. Save with `AssetDatabase.CreateAsset`, then add the atlas texture and material as sub-assets.
- [ ] **Step 5: Generate the chalk sprites** in `Unity_RunCommand` (Texture2D → `EncodeToPNG` → `File.WriteAllBytes` → import):
  - **Board:** 128×128 PNG. 10 px wood frame (`#A8743F`, a 2 px `#6B4522` inner line), green fill with a soft radial highlight (`#3B6E58` centre → `#2B5443` edges). Border 16 on every side.
  - **Ring:** white 96×80 elliptical ring, 6 px thick, slightly wobbly (jitter the radius ±2 px with a fixed seed).
  - **Line:** 64×8 white rounded bar.
  - **Tick and cross:** white, 64×64, 7 px strokes.
  - Import settings for all of them: Sprite (Single), uncompressed, no mips, Clamp, Full Rect.
- [ ] **Step 6: Set 9-slice borders** on the five Kenney `button_rectangle_depth_gloss.png` and five `button_square_depth_flat.png` (square border 16,20,16,14), plus `button_round_depth_gloss.png` (no border). Use `TextureImporter.spriteBorder` + `SaveAndReimport`.
- [ ] **Step 7: Run the tests.** Expected: PASS. Check the Console for errors.
- [ ] **Step 8: Credits and commit.** Add Patrick Hand (SIL OFL 1.1, path to `OFL.txt`) to `CREDITS.md`. Commit: `Chalkboard UI 1: fonts, chalk sprites, 9-slice borders`.

### Task 2: `UiTheme` asset and `TerrainSet.IsDesert`

**Files:**
- Create: `Assets/_Project/Scripts/UI/UiTheme.cs`, asset `Assets/_Project/Resources/UiTheme.asset`
- Modify: `Assets/_Project/Scripts/Level/TerrainSet.cs` (add `[SerializeField] private bool _isDesert;` + `public bool IsDesert` + `SetDesert(bool)`); `ScriptableObjects/Terrain/Sand.asset` → true
- Test: `Assets/_Project/Tests/EditMode/UiThemeTests.cs`

**Interfaces:**
- Produces: `UiTheme : ScriptableObject` with:
  - colours: `Color ChalkWhite`, `Color ChalkUrgent`, `Color DimOverlay`, `Color AnswerColor(int index)` (index 0–3, clamped);
  - fonts: `TMP_FontAsset ButtonFont`, `TMP_FontAsset ChalkFont`;
  - board sprites: `Sprite Board`, `Sprite Ring`, `Sprite Line`, `Sprite Tick`, `Sprite Cross`;
  - HUD sprites: `Sprite HeartFull`, `Sprite HeartEmpty`, `Sprite Coin`, `Sprite Star`, `Sprite StarEmpty`;
  - buttons: `Sprite Button(UiButtonColor c)`, `Sprite SquareButton(UiButtonColor c)`, `Sprite RoundButton`;
  - icons: `Sprite PlayIcon`, `Sprite RepeatIcon`;
  - `static UiTheme Load()` from `Resources/UiTheme`, so it must live at `Assets/_Project/Resources/UiTheme.asset` (ruling: **Resources** instead of `ScriptableObjects/UI`; the scripts need it at runtime without wiring).
- Produces: `enum UiButtonColor { Blue, Green, Yellow, Red, Grey }` in `UiButtonColor.cs`.
- Produces: `bool TerrainSet.IsDesert`.

- [ ] **Step 1: Write the failing tests** `UiThemeTests`:
  - `AnswerColorsFollowKeyOrder`: `AnswerColor(0..3)` equal `#8FD0FF, #A6EC9B, #FFE08A, #FF9D9D` (ColorUtility, within 1/255);
  - `ThemeAssetIsComplete`: `UiTheme.Load()` is not null, and every sprite and font property above is not null for all 5 `UiButtonColor` values;
  - `OnlySandIsDesert`: `Grass.asset` has `IsDesert == false`, `Sand.asset` has `true`.
- [ ] **Step 2: Run them.** Expected: FAIL (type missing → compile error counts as red; the Console must show only that).
- [ ] **Step 3: Implement** `UiTheme`, `UiButtonColor` and `TerrainSet.IsDesert`.
  - Create the asset in the Editor with the Global Constraints palette.
  - Assign the Task 1 assets: Kenney `hud_heart`, `hud_heart_empty`, `hud_coin` (platformer pack Tiles/Default), UI Pack `star.png` / `star_outline_depth.png` (Yellow), `icon_play_light`, `icon_repeat_light`.
  - Assign through setter methods (`SetSprites(...)`, `SetFonts(...)`) + SetDirty, then read back.
- [ ] **Step 4: Run** `UiThemeTests` + `UiAssetTests`. Expected: PASS.
- [ ] **Step 5: Commit** `Chalkboard UI 2: UiTheme`.

### Task 3: Pure pieces: `AnswerMarks`, `HeartRow`, `StarRow`

**Files:**
- Create: `Scripts/UI/AnswerMarks.cs`, `Scripts/UI/HeartRow.cs`, `Scripts/UI/StarRow.cs`
- Test: `Tests/EditMode/ChalkUiLogicTests.cs`

**Interfaces:**
- Produces: `enum AnswerMark { None, Tick, Cross }` in its own file `AnswerMark.cs`.
- Produces: `static AnswerMark[] AnswerMarks.For(int choiceCount, int correctIndex, int chosenIndex, int slots = 4)`:
  - Tick on `correctIndex`;
  - Cross on `chosenIndex` when it differs;
  - `chosenIndex == -1` (missed) → only the Tick;
  - indexes ≥ `choiceCount` are always `None`.
- Produces: `HeartRow : MonoBehaviour`:
  - `SetImages(Image[] hearts, Sprite full, Sprite empty)`;
  - `void Show(int lives)`;
  - pure `static bool[] States(int lives, int count)` (true = full, clamped 0..count).
  - The row has `StartingLives` images (3).
- Produces: `StarRow : MonoBehaviour`:
  - `SetImages(Image[] stars, Sprite full, Sprite empty)`;
  - `void Show(int stars)`;
  - `int Stars { get; }`;
  - same pure `States` helper (reuse `HeartRow.States`, no copy).

- [ ] **Step 1: Write the failing tests:**
  - `CorrectPickGetsOnlyATick`: `For(4, 2, 2)` → `[None, None, Tick, None]`.
  - `WrongPickGetsACrossAndTheAnswerATick`: `For(4, 1, 3)` → `[None, Tick, None, Cross]`.
  - `MissedShowsOnlyTheAnswer`: `For(4, 0, -1)` → `[Tick, None, None, None]`.
  - `MarksIgnoreMissingChoices`: `For(3, 1, 3)` → `[None, Tick, None, None]`.
  - `HeartStatesFillFromTheLeft`: `States(2, 3)` → `[true, true, false]`; `States(-1, 3)` → all false; `States(5, 3)` → all true.
  - `StarRowShowsSprites`: build a GameObject with 3 `Image`s, `Show(2)` → sprites `[full, full, empty]` and `Stars == 2`.
- [ ] **Step 2: Run them.** Expected: FAIL (compile).
- [ ] **Step 3: Implement** the three files (and `AnswerMark.cs`).
- [ ] **Step 4: Run** `ChalkUiLogicTests`. Expected: 6/6 PASS.
- [ ] **Step 5: Commit** `Chalkboard UI 3: answer marks, heart and star rows`.

### Task 4: Question board (Game, Sandbox_Level, Sandbox_QuestionZone)

**Files:**
- Modify: `Scripts/UI/AnswerButtons.cs`, `Scripts/UI/QuestionPanel.cs`
- Scenes, via `Unity_RunCommand`: `Game.unity`, `Sandbox_Level.unity`, `Sandbox_QuestionZone.unity`
- Test: `Tests/EditMode/QuestionBoardTests.cs`

**Interfaces:**
- Consumes: `UiTheme` (Task 2), `AnswerMarks.For` + `AnswerMark` (Task 3).
- `AnswerButtons` (Edit mode does not run `Awake`/`OnEnable` on scene objects, so the handlers become public methods that the event subscriptions call: `public void Show(MathQuestion question)`, `public void Lock(int choiceIndex)`, `public void Resolve(QuestionResult result)`):
  - **Inspector:** replaces its three colour fields with `[SerializeField] private Image[] _rings = new Image[4]; [SerializeField] private Image[] _marks = new Image[4];`.
  - **On show:** ring colour = `UiTheme.AnswerColor(i)` at full alpha, scale 1, marks hidden.
  - **On lock:** the chosen ring's scale is 1.15 with alpha 1; the others go to alpha 0.4; all non-interactable.
  - **On resolve:** sets each `_marks[i]` from `AnswerMarks.For(question.Choices.Count, question.Choices.IndexOf(question.Answer), result.ChosenIndex)`: Tick → `UiTheme.Tick` tinted green `#A6EC9B`, Cross → `UiTheme.Cross` tinted red `#FF9D9D`. It **no longer hides `_root`** (the board does that).
  - Pure helper used by the test: `public static float RingAlpha(int index, int chosenIndex)` (chosen or none locked → 1, else 0.4).
- `QuestionPanel`:
  - colours default to chalk: timer `ChalkWhite`, urgent `ChalkUrgent`, correct `#A6EC9B`, wrong `#FF9D9D`;
  - question text colour = `ChalkWhite` instead of `Color.white`;
  - behaviour unchanged otherwise.
- **Scene layout (the same in all three scenes):** `_panel` becomes the board (`Image` with `UiTheme.Board`, Sliced, top-centre, about 62 % of the screen width × 30 % of the height). Inside, top to bottom:
  - `Question` (TMP, ChalkFont, auto-size 18–64);
  - `TimerBar` (chalk line + fill);
  - `Feedback` (TMP, ChalkFont);
  - `Answers` (horizontal layout).
- **Answers:** `AnswerButtons._root` moves inside the board. Each of the 4 buttons is:
  - `Ring` (`Image`, raycast target = the button graphic);
  - `Label` (TMP, ChalkFont, ChalkWhite, auto-size 18–48);
  - `Mark` (`Image`, `raycastTarget = false`).
- The old tan button graphics are removed and `_hint` is kept as a small chalk line "Tap or press 1–4". Everything else under the board has `raycastTarget = false`.

- [ ] **Step 1: Write the failing tests** `QuestionBoardTests`. Open `Game.unity` with `EditorSceneManager.OpenScene` (additive, closed in TearDown) for the scene checks.
  - `RingAlphaFadesTheOthers`: `RingAlpha(0, 2) == 0.4f`, `RingAlpha(2, 2) == 1f`, `RingAlpha(1, -1) == 1f`.
  - `QuestionBoardTextAutoSizes`: in Game, the question TMP and all 4 answer labels have `enableAutoSizing`, and `fontSizeMin <= 18`.
  - `AnswerCirclesSitInsideTheBoard`: each `AnswerButtons` button is a descendant of `QuestionPanel._panel`. Find it via `transform.Find` from the HUD; no reflection.
  - `UnusedCirclesHide`: call the scene's `AnswerButtons.Show` with a 3-choice `MathQuestion` → button 3 is inactive; then `Resolve` (correct pick) → only one mark visible.
- [ ] **Step 1b: Write the PlayMode file** `Tests/PlayMode/ChalkBoardPlayTests.cs` (Sandbox_QuestionZone, whose first zone is untimed; drive it the way `EncounterTests` does). Its tests:
  - `UntimedQuestionHidesTimerLine`: at the first question, `TimerBar` is inactive and 4 rings are active.
  - `WrongPickShowsTickAndCross`: pick a wrong answer with `LevelBot`/`InputReader.NotifyAnswerChosen` → after resolve, the right circle's mark shows the Tick sprite and the chosen one shows the Cross.
  - It only needs to compile here; it runs in Task 7.
  - `NonButtonGraphicsIgnoreTaps`: every `Graphic` under the board that is not a `Button.targetGraphic` has `raycastTarget == false`.
- [ ] **Step 2: Run them.** Expected: FAIL.
- [ ] **Step 3: Change** `AnswerButtons` and `QuestionPanel` as in Interfaces.
- [ ] **Step 4: Rebuild the board in `Game.unity`** via `Unity_RunCommand`, save, and read the hierarchy back. Then do the same in `Sandbox_Level` and `Sandbox_QuestionZone`, whose layouts already mirror Game's question UI. **Ruling allowed:** if a sandbox's HUD differs, rebuild only what exists there.
- [ ] **Step 5: Run** `QuestionBoardTests` + the full EditMode suite. Expected: all PASS (EditMode total ≥ 1052 + new).
- [ ] **Step 6: Capture.** In Game, use paused Play mode + `captureDeltaTime` + `EditorApplication.Step()` until the first question shows, with the canvas set to ScreenSpaceCamera for the capture. Render it to `Temp/board.png` and Read it. Expected: a chalkboard with the question and 4 coloured rings, as in mockup option 2.
- [ ] **Step 7: Commit** `Chalkboard UI 4: question board`.

### Task 5: HUD (Game + Sandbox_Level)

**Files:**
- Modify: `Scripts/UI/HudView.cs`
- Scenes via `Unity_RunCommand`: `Game.unity`, `Sandbox_Level.unity`
- Test: `Tests/EditMode/HudTests.cs`

**Interfaces:**
- Consumes: `HeartRow` (Task 3), `UiTheme`.
- `HudView`:
  - `_livesText` and `_lifeSymbol` are replaced by `[SerializeField] private HeartRow _hearts;`, and `OnLivesChanged` calls `_hearts.Show(lives)`;
  - the score, streak, level and coin texts stay TMP, now ChalkFont on a small board strip (top-left);
  - the coin text gets a sibling `Image` with `UiTheme.Coin`.
- **Pause button:** `UiTheme.RoundButton` (Grey) with a two-bar pause glyph (two `Image` bars, ChalkWhite or dark), top-right.

- [ ] **Step 1: Write the failing tests** `HudTests` (Game scene opened additively):
  - `HeartsShowLives`: the HUD has a `HeartRow` with 3 images; `Show(2)` on it → sprites `[full, full, empty]` (Edit mode does not run `OnEnable`, so call it directly);
  - `HudTextsUseChalkFont`: the score, streak, level and coin TMPs use `UiTheme.ChalkFont`;
  - `HudGraphicsIgnoreTaps`: every non-button `Graphic` under `HUD/TopBar` has `raycastTarget == false`.
- [ ] **Step 2: Run them.** Expected: FAIL.
- [ ] **Step 3: Change `HudView`, then rebuild the HUD** in both scenes. Keep `TopBar/ScorePopup` (FeedbackFX's template) and `Flash` untouched. Restyle `ScorePopup` to ChalkFont only.
- [ ] **Step 4: Run** `HudTests` + the full EditMode suite. Expected: PASS.
- [ ] **Step 5: Commit** `Chalkboard UI 5: HUD`.

### Task 6: Pause, Level complete, Game over boards

**Files:**
- Modify: `Scripts/UI/OverlayController.cs`, `Tests/PlayMode/MenuFlowTests.cs`
- Scenes: `Game.unity`, `Sandbox_Level.unity` (the overlays exist in both)
- Test: `Tests/EditMode/OverlayBoardTests.cs`

**Interfaces:**
- Consumes: `StarRow`, `UiTheme`.
- `OverlayController` adds `[SerializeField] private StarRow _levelCompleteStars;`:
  - `OnProgressSaved` calls `_levelCompleteStars.Show(result.Stars)`;
  - `_levelCompleteResult.text` becomes only `result.NewBest ? "New best score!" : $"Best: {result.BestScore}"`;
  - the stats text adds the coins: `$"Coins: {_score.Coins}"`;
  - `GameOver` stats start with `"You answered {CorrectCount} right."`.
- **Scene layout for each panel:**
  - full-screen dim `Image` (`DimOverlay`, `raycastTarget = true` so taps don't reach the game);
  - `Box` = board sprite (sliced);
  - title TMP in ChalkFont: "Paused", "Level complete!", "Out of hearts";
  - the existing buttons, re-skinned with Kenney rectangle buttons and Kenney Future labels:
    - Resume and Next = Green (with `PlayIcon`);
    - Restart, Retry and Again = Blue (with `RepeatIcon`);
    - Levels = Grey.
  - Level complete: `Box/Stars` (StarRow) above `Box/Result`.
  - Keep the names `Box`, `Result`, `NextLevelButton`.
- **Overlays draw after the question board:** `Overlays` is the HUD's last sibling.

- [ ] **Step 1: Write the failing tests:**
  - `OverlayBoardTests.PauseBoardDrawsAboveQuestionBoard`: in Game, `Overlays.GetSiblingIndex() > board's top-level ancestor's sibling index` (or a higher Canvas `sortingOrder` if it is a nested canvas).
  - `OverlayBoardTests.LevelCompleteHasStarRow`: `Overlays/LevelCompletePanel/Box/Stars` has a `StarRow` with 3 images.
  - **Update `MenuFlowTests.NextLevelButtonAfterFinishingLevelOneStartsLevelTwo`:** replace `Does.Contain("3 of 3 stars")` with `Box/Stars` `StarRow.Stars == 3`, and keep `Result` containing "New best score!". Its PlayMode run happens in Task 7.
- [ ] **Step 2: Run** the EditMode ones. Expected: FAIL.
- [ ] **Step 3: Change `OverlayController`, then rebuild** the three panels in both scenes.
- [ ] **Step 4: Run** `OverlayBoardTests` + the full EditMode suite. Expected: PASS.
- [ ] **Step 5: Capture** each panel: set the GameObject active in Edit mode, render the camera with the canvas in ScreenSpaceCamera, then restore it **without saving**. Expected: a dim background, the board, chalk title, Kenney buttons.
- [ ] **Step 6: Commit** `Chalkboard UI 6: overlay boards`.

### Task 7: Main menu, level select, final check, docs

**Files:**
- Modify: `Scripts/UI/LevelButton.cs`, `Tests/PlayMode/MenuFlowTests.cs`
- Scenes: `MainMenu.unity`, `LevelSelect.unity`
- Modify: `CLAUDE.md` (Current Progress: chalkboard UI line; the `StarRating.Display` note), `IMPROVEMENT_PLAN.md` (UI item done)
- Test: `Tests/EditMode/LevelButtonTests.cs`

**Interfaces:**
- Consumes: `StarRow`, `UiTheme`, `UiButtonColor`, `TerrainSet.IsDesert`.
- `LevelButton`:
  - gains `[SerializeField] private Image _face; [SerializeField] private StarRow _stars;`;
  - `Show` sets `_face.sprite = UiTheme.SquareButton(ColorFor(level, unlocked))`;
  - pure `public static UiButtonColor ColorFor(bool desert, bool unlocked)`: locked → Grey, desert → Yellow, else Green;
  - `_stars` is shown only when there is a record, with `record.BestStars`;
  - `_detail.text` = `"Locked"` / `"New!"` / `$"Best {record.BestScore}"`.
  - `StarRating.Display` stays (still used by nothing in the UI after this). **Ruling allowed:** delete it if there are no references left, together with its test, if any.
- **Main menu:** a board-less title in Kenney Future (white with a `#2B5443` 3 px outline), the `character_green_idle` sprite, `PlayButton` = Green rectangle + `PlayIcon` + "PLAY", `QuitButton` = Grey "QUIT".
- **Level select:**
  - a board titled "Pick a level" (ChalkFont);
  - the grid of `LevelButton`s: square Kenney face, number in Kenney Future, topic small under it in ChalkFont, stars and detail under that;
  - Back = Grey rectangle button.

- [ ] **Step 1: Write the failing tests:**
  - `LevelButtonTests.ColorFollowsWorldAndLock`: `ColorFor(false, true) == Green`, `ColorFor(true, true) == Yellow`, `ColorFor(true, false) == Grey`.
  - **Update `MenuFlowTests`:** the level-1 detail assertion becomes the `StarRow.Stars == 2` (found under the button) plus `Detail` containing "900"; "Locked" and "New!" stay as they are.
- [ ] **Step 2: Run** `LevelButtonTests`. Expected: FAIL.
- [ ] **Step 3: Change `LevelButton`, then rebuild** MainMenu and LevelSelect. Keep the names `PlayButton`, the template with children `Detail`, and the `Level N` naming in `LevelSelectController`.
- [ ] **Step 4: Run** the full EditMode suite. Expected: all PASS.
- [ ] **Step 5: The one PlayMode run** (filtered to `MenuFlowTests`, `EncounterTests`, `LevelRunTests`, `ChalkBoardPlayTests`). Read `Logs/TestResults-PlayMode.txt`. Expected: all PASS.
- [ ] **Step 6: Phone-shaped capture.** In Game, use a 2340×1080 render target with the first question showing. Expected: the board in the top ~30 %, with the runner and the enemy visible below it. Also capture MainMenu and LevelSelect.
- [ ] **Step 7: Docs and commit.** Update CLAUDE.md and IMPROVEMENT_PLAN.md. Commit `Chalkboard UI 7: menus, level select, docs`.
