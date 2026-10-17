using System.Collections;
using MathRunner.Core;
using MathRunner.Input;
using MathRunner.Player;
using MathRunner.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MathRunner.Tests.PlayMode
{
    // Full runs through the gray-box level, played by LevelBot with a fixed 0.02 s frame step.
    public class LevelRunTests : IPrebuildSetup
    {
        private const string LevelScene = "Sandbox_Level";
        private const string LevelScenePath = "Assets/_Project/Scenes/Sandbox_Level.unity";
        private const int MaxFrames = 8000;

        // SceneManager.LoadScene only finds scenes in the build list, so make sure the test scene is in it.
        public void Setup()
        {
#if UNITY_EDITOR
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            if (System.Array.Exists(scenes, s => s.path == LevelScenePath)) return;
            var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(scenes)
            {
                new UnityEditor.EditorBuildSettingsScene(LevelScenePath, true)
            };
            UnityEditor.EditorBuildSettings.scenes = list.ToArray();
#endif
        }

        private GameManager _game;
        private ScoreSystem _score;
        private LivesSystem _lives;
        private PlayerController _player;
        private InputReader _input;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.captureDeltaTime = 0.02f;
            SceneManager.LoadScene(LevelScene);
            yield return null;
            yield return null;
            FindSceneObjects();
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator WinningRun_ScoresEveryAnswerAndCompletesTheLevel()
        {
            using (var bot = new LevelBot())
            {
                yield return PlayUntilOver(bot);

                Assert.That(_game.State, Is.EqualTo(GameState.LevelComplete));
                Assert.That(bot.Hits, Is.Empty, "The bot should clear every spike and gap");
                Assert.That(_lives.Lives, Is.EqualTo(3));
                Assert.That(_score.CorrectCount, Is.EqualTo(4));
                // Level 5, zones untimed / 4 s / 3 s / 2 s: 500 + (500+50+25) + (500+50+50) + (500+50+75)
                Assert.That(_score.Score, Is.EqualTo(2300));
                Assert.That(Time.timeScale, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator LosingRun_WrongAnswerFallAndSpikeEndTheGame()
        {
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = _ => AnswerPlan.Wrong;
                bot.ShouldJump = h => h.Kind == HazardKind.Gap ? h.TimesReached > 1 : !bot.Respawned;
                yield return PlayUntilOver(bot);

                Assert.That(_game.State, Is.EqualTo(GameState.GameOver));
                Assert.That(_lives.Lives, Is.Zero);
                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Wrong));
                Assert.That(bot.Hits, Is.EqualTo(new[] { HitCause.Fall, HitCause.Obstacle }));
                Assert.That(bot.Respawned, Is.True, "Falling should respawn the player at a checkpoint");
                Assert.That(_score.Score, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator MissedQuestion_CostsALifeAndTheRunContinues()
        {
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = _ => AnswerPlan.Miss;
                int frames = 0;
                while (bot.Results.Count == 0 && frames++ < MaxFrames)
                {
                    bot.BeforeFrame();
                    yield return null;
                    bot.AfterFrame();
                }

                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Missed));
                Assert.That(bot.Results[0].ChosenIndex, Is.EqualTo(-1));
                Assert.That(_lives.Lives, Is.EqualTo(2));
                Assert.That(_game.State, Is.EqualTo(GameState.Playing));
            }
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTheRunAndBlocksJumping()
        {
            for (int i = 0; i < 30; i++) yield return null;

            _game.TogglePause();
            Assert.That(_game.State, Is.EqualTo(GameState.Paused));
            Assert.That(Time.timeScale, Is.Zero);

            _input.NotifyJumpPressed();
            Assert.That(_input.JumpHeld, Is.False, "Jump presses must be ignored while paused");

            float x = _player.transform.position.x;
            for (int i = 0; i < 10; i++) yield return null;
            Assert.That(_player.transform.position.x, Is.EqualTo(x).Within(0.001f));

            _game.TogglePause();
            Assert.That(_game.State, Is.EqualTo(GameState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Restart_ReloadsAFreshLevel()
        {
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = _ => AnswerPlan.Wrong;
                int frames = 0;
                while (bot.Results.Count == 0 && frames++ < MaxFrames)
                {
                    bot.BeforeFrame();
                    yield return null;
                    bot.AfterFrame();
                }
            }
            Assert.That(_lives.Lives, Is.EqualTo(2));

            _game.Restart();
            yield return null;
            yield return null;
            FindSceneObjects();

            Assert.That(_lives.Lives, Is.EqualTo(3));
            Assert.That(_score.Score, Is.Zero);
            Assert.That(_game.State, Is.EqualTo(GameState.Playing));
            Assert.That(_player.transform.position.x, Is.LessThan(2f));
        }

        private IEnumerator PlayUntilOver(LevelBot bot)
        {
            int frames = 0;
            while (_game.State == GameState.Playing && frames++ < MaxFrames)
            {
                bot.BeforeFrame();
                yield return null;
                bot.AfterFrame();
            }
        }

        private void FindSceneObjects()
        {
            _game = Object.FindFirstObjectByType<GameManager>();
            _score = Object.FindFirstObjectByType<ScoreSystem>();
            _lives = Object.FindFirstObjectByType<LivesSystem>();
            _player = Object.FindFirstObjectByType<PlayerController>();
            _input = Object.FindFirstObjectByType<InputReader>();
        }
    }
}
