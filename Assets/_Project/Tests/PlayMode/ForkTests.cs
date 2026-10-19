using System;
using System.Collections;
using System.Linq;
using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Player;
using MathRunner.Save;
using MathRunner.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MathRunner.Tests.PlayMode
{
    // Forks in the path in the real Game scene (Level 1: every question is a fork). The bot answers by jumping: a full
    // jump for route 1 (high ledge), a short hop for route 2 (middle ledge), no jump for route 3 (low road).
    public class ForkTests
    {
        private const float FrameStep = 0.02f;
        private const int MaxFrames = 6000;

        private string _savePath;
        private PlayerController _player;
        private LivesSystem _lives;
        private ScoreSystem _score;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mathrunner-fork-" + Guid.NewGuid() + ".json");
            SaveSystem.UseFile(_savePath);
            LevelSelection.Current = LevelCatalog.Load().Levels[0];
            Time.captureDeltaTime = FrameStep;
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;
            _player = Object.FindFirstObjectByType<PlayerController>();
            _lives = Object.FindFirstObjectByType<LivesSystem>();
            _score = Object.FindFirstObjectByType<ScoreSystem>();
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

        private static ForkZone FirstFork() =>
            Object.FindObjectsByType<ForkZone>(FindObjectsSortMode.None).OrderBy(z => z.transform.position.x).First();

        private static IEnumerator PlayUntil(LevelBot bot, Func<bool> done, string what)
        {
            int frames = 0;
            while (!done() && frames++ < MaxFrames)
            {
                bot.BeforeFrame();
                yield return null;
                bot.AfterFrame();
            }
            Assert.That(done(), Is.True, "Timed out waiting for " + what);
        }

        // The jump really decides the route: a full jump lands on the high ledge, a hop on the middle one, no jump on the low road.
        [UnityTest]
        public IEnumerator EachJumpTakesItsRoute([Values(ForkPlan.High, ForkPlan.Middle, ForkPlan.Low)] int lane)
        {
            var fork = FirstFork();
            using (var bot = new LevelBot { PickFor = (n, q) => lane })
            {
                yield return PlayUntil(bot, () => bot.Results.Count > 0, "the first fork to resolve");
                Assert.That(fork.ChosenLane, Is.EqualTo(lane));
                Assert.That(bot.Results[0].ChosenIndex, Is.EqualTo(lane));
                Assert.That(bot.Results[0].Question.Choices.Count, Is.EqualTo(3));
                Assert.That(Time.timeScale, Is.EqualTo(1f), "focus ended once the route was taken");
            }
        }

        [UnityTest]
        public IEnumerator RightRouteOpensTheChest()
        {
            var fork = FirstFork();
            int coins = 0;
            Action<int> onCoin = v => coins++;
            GameEvents.CoinCollected += onCoin;
            try
            {
                using (var bot = new LevelBot())
                {
                    yield return PlayUntil(bot, () => bot.Results.Count > 0, "the first fork to resolve");
                    Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Correct));
                    int lane = fork.ChosenLane;
                    yield return PlayUntil(bot, () => _player.transform.position.x > fork.Plan.Lanes[lane].ChestX + 1f, "the runner to pass the chest");
                    Assert.That(fork.Chest(lane).IsOpened, Is.True);
                    Assert.That(coins, Is.GreaterThanOrEqualTo(5));
                    Assert.That(bot.Hits, Is.Empty);
                    Assert.That(_lives.Lives, Is.EqualTo(3));
                    Assert.That(_score.CorrectCount, Is.EqualTo(1));
                }
            }
            finally
            {
                GameEvents.CoinCollected -= onCoin;
            }
        }

        // The heart is the wrong answer's price, not the mud's: jumping over the mud must not dodge it.
        [UnityTest]
        public IEnumerator WrongRouteCostsItsHeartEvenWhenJumpingTheMud()
        {
            var fork = FirstFork();
            var input = Object.FindFirstObjectByType<MathRunner.Input.InputReader>();
            using (var bot = new LevelBot { AnswerPlanFor = n => n == 0 ? AnswerPlan.Wrong : AnswerPlan.Correct })
            {
                yield return PlayUntil(bot, () => bot.Results.Count > 0, "the first fork to resolve");
                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Wrong));
                var lanePlan = fork.Plan.Lanes[fork.ChosenLane];
                yield return PlayUntil(bot, () => _player.transform.position.x > lanePlan.MudLeft - 2.2f && _player.IsGrounded, "the run-up to the mud");
                input.NotifyJumpPressed();
                for (int i = 0; i < 25; i++)
                {
                    bot.BeforeFrame();
                    yield return null;
                    bot.AfterFrame();
                }
                input.NotifyJumpReleased();
                yield return PlayUntil(bot, () => _player.transform.position.x > lanePlan.MudRight + 1f, "the runner to pass the mud");
                Assert.That(bot.Hits, Is.EqualTo(new[] { HitCause.Mud }), "exactly one heart for the wrong route");
                Assert.That(_lives.Lives, Is.EqualTo(2));
            }
        }

        [UnityTest]
        public IEnumerator WrongRouteMudSlowsAndCostsOneHeart()
        {
            var fork = FirstFork();
            using (var bot = new LevelBot { AnswerPlanFor = n => n == 0 ? AnswerPlan.Wrong : AnswerPlan.Correct })
            {
                yield return PlayUntil(bot, () => bot.Results.Count > 0, "the first fork to resolve");
                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Wrong));
                int lane = fork.ChosenLane;
                var lanePlan = fork.Plan.Lanes[lane];
                yield return PlayUntil(bot, () => _player.transform.position.x > lanePlan.MudLeft + 0.5f, "the runner to reach the mud");
                Assert.That(_player.SpeedFactor, Is.EqualTo(MudPatch.SlowFactor), "slowed in the mud");
                yield return PlayUntil(bot, () => _player.transform.position.x > lanePlan.MudRight + 1f, "the runner to leave the mud");
                Assert.That(_player.SpeedFactor, Is.EqualTo(1f), "full speed again");
                Assert.That(bot.Hits, Is.EqualTo(new[] { HitCause.Mud }));
                Assert.That(_lives.Lives, Is.EqualTo(2));
                Assert.That(fork.Chest(lane).IsOpened, Is.False);
            }
        }
    }
}
