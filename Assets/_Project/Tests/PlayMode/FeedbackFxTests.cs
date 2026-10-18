using System.Collections;
using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Save;
using MathRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MathRunner.Tests.PlayMode
{
    // FeedbackFX is wired up in the Game scene and reacts to answers (popup on correct, red flash on wrong).
    public class FeedbackFxTests
    {
        private const int MaxFrames = 6000;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mathrunner-fx-" + System.Guid.NewGuid() + ".json");
            SaveSystem.UseFile(_savePath);
            Time.captureDeltaTime = 0.02f;
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

        [UnityTest]
        public IEnumerator CorrectAnswerShowsAScorePopup()
        {
            var fx = default(FeedbackFX);
            yield return LoadLevel(1, f => fx = f);
            using (var bot = new LevelBot())
            {
                yield return Until(bot, () => bot.Results.Count == 1);
                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Correct));
                Assert.That(fx.ActivePopups, Is.EqualTo(1), "A '+N' popup should float up after a correct answer");
                Assert.That(fx.FlashAlpha, Is.EqualTo(0f), "No red flash on a correct answer");
                for (int i = 0; i < 60; i++) yield return null;
                Assert.That(fx.ActivePopups, Is.EqualTo(0), "The popup should clean itself up");
            }
        }

        [UnityTest]
        public IEnumerator WrongAnswerFlashesRedThenFades()
        {
            var fx = default(FeedbackFX);
            yield return LoadLevel(1, f => fx = f);
            using (var bot = new LevelBot { AnswerPlanFor = _ => AnswerPlan.Wrong })
            {
                yield return Until(bot, () => bot.Results.Count == 1);
                for (int i = 0; i < 5; i++) yield return null;
                Assert.That(fx.FlashAlpha, Is.GreaterThan(0f));
                Assert.That(fx.ActivePopups, Is.EqualTo(0), "No score popup for a wrong answer");
                for (int i = 0; i < 30; i++) yield return null;
                Assert.That(fx.FlashAlpha, Is.EqualTo(0f));
            }
        }

        private static IEnumerator LoadLevel(int number, System.Action<FeedbackFX> found)
        {
            LevelSelection.Current = LevelCatalog.Load().Levels[number - 1];
            SceneManager.LoadScene(SceneFlow.Game);
            yield return null;
            yield return null;
            var fx = Object.FindFirstObjectByType<FeedbackFX>();
            Assert.That(fx, Is.Not.Null, "The Game scene needs a FeedbackFX");
            found(fx);
        }

        private static IEnumerator Until(LevelBot bot, System.Func<bool> done)
        {
            int frames = 0;
            while (!done() && frames++ < MaxFrames)
            {
                bot.BeforeFrame();
                yield return null;
                bot.AfterFrame();
            }
            Assert.That(done(), Is.True, "Timed out waiting");
        }
    }
}
