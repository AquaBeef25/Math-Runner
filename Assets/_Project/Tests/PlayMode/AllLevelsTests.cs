using System.Collections;
using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Save;
using MathRunner.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MathRunner.Tests.PlayMode
{
    // Builds every level in the catalog and plays it with all answers correct: each must be finishable without a hit.
    public class AllLevelsTests : IPrebuildSetup
    {
        private const string GameScene = "Game";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const float FrameStep = 0.02f;

        public void Setup()
        {
#if UNITY_EDITOR
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            if (System.Array.Exists(scenes, s => s.path == GameScenePath)) return;
            var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(scenes)
            {
                new UnityEditor.EditorBuildSettingsScene(GameScenePath, true)
            };
            UnityEditor.EditorBuildSettings.scenes = list.ToArray();
#endif
        }

        private string _savePath;

        [SetUp]
        public void UseTemporarySave()
        {
            _savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mathrunner-playmode-" + System.Guid.NewGuid() + ".json");
            SaveSystem.UseFile(_savePath);
        }

        [TearDown]
        public void TearDown()
        {
            LevelSelection.Current = null;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            if (System.IO.File.Exists(_savePath)) System.IO.File.Delete(_savePath);
            SaveSystem.UseFile(null);
        }

        [Test]
        public void CatalogListsTenLevelsInOrder()
        {
            var catalog = LevelCatalog.Load();
            Assert.That(catalog, Is.Not.Null, "Resources/LevelCatalog is missing");
            Assert.That(catalog.Levels.Count, Is.EqualTo(10));
            for (int i = 0; i < catalog.Levels.Count; i++)
            {
                Assert.That(catalog.Levels[i].LevelNumber, Is.EqualTo(i + 1));
                Assert.That(catalog.Levels[i].Questions.Validate(), Is.Empty, $"Level {i + 1} has invalid question settings");
            }
        }

        [UnityTest]
        public IEnumerator EveryLevelCanBeFinishedWithAllAnswersCorrect([NUnit.Framework.Range(0, 9)] int index)
        {
            var config = LevelCatalog.Load().Levels[index];
            LevelSelection.Current = config;
            Time.captureDeltaTime = FrameStep;
            SceneManager.LoadScene(GameScene);
            yield return null;
            yield return null;

            var builder = Object.FindFirstObjectByType<LevelBuilder>();
            var game = Object.FindFirstObjectByType<GameManager>();
            var score = Object.FindFirstObjectByType<ScoreSystem>();
            var lives = Object.FindFirstObjectByType<LivesSystem>();
            Assert.That(builder.Level, Is.SameAs(config));

            int maxFrames = (int)(builder.Plan.FinishX / config.RunSpeed / FrameStep) + 500;
            using (var bot = new LevelBot())
            {
                int frames = 0;
                while (game.State == GameState.Playing && frames++ < maxFrames)
                {
                    bot.BeforeFrame();
                    yield return null;
                    bot.AfterFrame();
                }

                Assert.That(game.State, Is.EqualTo(GameState.LevelComplete), $"Level {config.LevelNumber} not finished after {frames} frames");
                Assert.That(bot.Hits, Is.Empty, $"Level {config.LevelNumber}: the bot was hit by {string.Join(", ", bot.Hits)}");
                Assert.That(lives.Lives, Is.EqualTo(3));
                Assert.That(score.CorrectCount, Is.EqualTo(config.QuestionCount));
                Assert.That(score.AnsweredCount, Is.EqualTo(config.QuestionCount));

                var record = SaveSystem.Data.GetRecord(config.LevelNumber);
                Assert.That(record, Is.Not.Null, "Completing a level should save it");
                Assert.That(record.BestScore, Is.EqualTo(score.Score));
                Assert.That(record.BestStars, Is.EqualTo(3), "An all-correct, hit-free run is flawless");
                Assert.That(SaveSystem.Data.IsUnlocked(config.LevelNumber + 1), Is.True);
                Assert.That(System.IO.File.Exists(_savePath), Is.True);
            }
        }
    }
}
