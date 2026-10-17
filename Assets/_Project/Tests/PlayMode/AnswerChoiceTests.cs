using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MathRunner.Core;
using MathRunner.Input;
using MathRunner.Level;
using MathRunner.Math;
using MathRunner.Player;
using MathRunner.Save;
using MathRunner.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MathRunner.Tests.PlayMode
{
    // Tap-to-answer: picking locks in, the runner jumps onto the pick by itself, no pick by the decision line is a miss.
    public class AnswerChoiceTests
    {
        private const int MaxFrames = 6000;

        private string _savePath;
        private GameManager _game;
        private LivesSystem _lives;
        private PlayerController _player;
        private InputReader _input;
        private readonly List<int> _locked = new List<int>();
        private readonly List<ScoreUpdate> _scores = new List<ScoreUpdate>();

        private void OnLocked(int index) => _locked.Add(index);
        private void OnScore(ScoreUpdate update) { if (update.Delta > 0) _scores.Add(update); }

        [SetUp]
        public void SetUp()
        {
            _savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mathrunner-answers-" + System.Guid.NewGuid() + ".json");
            SaveSystem.UseFile(_savePath);
            Time.captureDeltaTime = 0.02f;
            _locked.Clear();
            _scores.Clear();
            GameEvents.AnswerLocked += OnLocked;
            GameEvents.ScoreChanged += OnScore;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.AnswerLocked -= OnLocked;
            GameEvents.ScoreChanged -= OnScore;
            LevelSelection.Current = null;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            if (System.IO.File.Exists(_savePath)) System.IO.File.Delete(_savePath);
            SaveSystem.UseFile(null);
        }

        [UnityTest]
        public IEnumerator FirstPickLocksInAndLaterPicksAreIgnored()
        {
            yield return LoadLevel(1);
            using (var bot = new LevelBot { AnswerPlanFor = _ => AnswerPlan.Miss })
            {
                MathQuestion question = null;
                GameEvents.QuestionShown += Capture;
                void Capture(MathQuestion q, float limit) => question = q;
                try
                {
                    yield return Until(bot, () => question != null);
                    int wrong = (question.CorrectIndex + 1) % 3;
                    _input.NotifyAnswerChosen(wrong);
                    _input.NotifyAnswerChosen(question.CorrectIndex);
                    yield return Until(bot, () => bot.Results.Count == 1);

                    Assert.That(_locked, Is.EqualTo(new[] { wrong }), "Only the first pick should be accepted");
                    Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Wrong));
                    Assert.That(bot.Results[0].ChosenIndex, Is.EqualTo(wrong));
                    Assert.That(_lives.Lives, Is.EqualTo(2));
                }
                finally { GameEvents.QuestionShown -= Capture; }
            }
        }

        [UnityTest]
        public IEnumerator NoPickIsMissedAtTheDecisionLine()
        {
            yield return LoadLevel(1);
            var zone = Object.FindObjectsByType<QuestionZone>(FindObjectsSortMode.None).OrderBy(z => z.transform.position.x).First();
            float resolvedAtX = float.NaN;
            using (var bot = new LevelBot { AnswerPlanFor = _ => AnswerPlan.Miss })
            {
                GameEvents.QuestionResolved += Record;
                void Record(QuestionResult r) => resolvedAtX = _player.transform.position.x;
                try { yield return Until(bot, () => bot.Results.Count == 1); }
                finally { GameEvents.QuestionResolved -= Record; }

                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Missed));
                Assert.That(bot.Results[0].ChosenIndex, Is.EqualTo(-1));
                Assert.That(resolvedAtX, Is.EqualTo(zone.DecisionX).Within(_player.RunSpeed * 0.05f), "The miss should happen at the decision line, not at the end of the zone");
                Assert.That(_lives.Lives, Is.EqualTo(2));
                Assert.That(_locked, Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator WrongPickCrumblesThatPlatformAndCostsALife()
        {
            yield return LoadLevel(1);
            using (var bot = new LevelBot { AnswerPlanFor = _ => AnswerPlan.Wrong })
            {
                yield return Until(bot, () => bot.Results.Count == 1);
                var result = bot.Results[0];
                Assert.That(result.Outcome, Is.EqualTo(AnswerOutcome.Wrong));
                Assert.That(_lives.Lives, Is.EqualTo(2));

                for (int i = 0; i < 30; i++) yield return null;
                var zone = Object.FindObjectsByType<QuestionZone>(FindObjectsSortMode.None).OrderBy(z => z.transform.position.x).First();
                var platforms = zone.GetComponentsInChildren<AnswerPlatform>().OrderBy(p => p.transform.position.x).ToArray();
                Assert.That(platforms[result.ChosenIndex].GetComponent<BoxCollider2D>().enabled, Is.False, "The wrong platform should crumble away");
                Assert.That(_player.IsGrounded && _player.transform.position.y < 1f, Is.True, "The player should drop back to the ground");
            }
        }

        [UnityTest]
        public IEnumerator EveryPlatformCanBeLandedOnAtSlowAndFastSpeeds([Values(1, 10)] int level)
        {
            yield return LoadLevel(level);
            using (var bot = new LevelBot { PickFor = (n, q) => n })
            {
                yield return Until(bot, () => bot.Results.Count == 3 || _game.State != GameState.Playing);
                Assert.That(bot.Results.Count, Is.EqualTo(3));
                for (int i = 0; i < 3; i++)
                {
                    Assert.That(bot.Results[i].ChosenIndex, Is.EqualTo(i), $"Question {i}: the runner should land on platform {i}");
                    Assert.That(bot.Results[i].Outcome, Is.Not.EqualTo(AnswerOutcome.Missed));
                }
                Assert.That(bot.Hits, Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator PickingAtHalfTimeEarnsHalfTheSpeedBonus()
        {
            yield return LoadLevel(10); // every question timed (6 s), 1000 base points
            using (var bot = new LevelBot { PickDelaySeconds = 3f })
            {
                yield return Until(bot, () => bot.Results.Count == 1);
                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Correct));
                Assert.That(bot.Results[0].TimeRemainingFraction, Is.EqualTo(0.5f).Within(0.01f));
                Assert.That(_scores[0].Delta, Is.InRange(1024, 1026), "1000 base + about half of the 50-point speed bonus");
            }
        }

        [UnityTest]
        public IEnumerator MashingJumpAfterPickingStillLandsOnThePick()
        {
            yield return LoadLevel(1);
            using (var bot = new LevelBot())
            {
                int frames = 0;
                bool press = true;
                while (bot.Results.Count == 0 && frames++ < MaxFrames)
                {
                    bot.BeforeFrame();
                    if (_locked.Count > 0)
                    {
                        if (press) _input.NotifyJumpPressed(); else _input.NotifyJumpReleased();
                        press = !press;
                    }
                    yield return null;
                    bot.AfterFrame();
                }
                _input.NotifyJumpReleased();
                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Correct));
            }
        }

        [UnityTest]
        public IEnumerator PicksAreIgnoredWhilePaused()
        {
            yield return LoadLevel(1);
            using (var bot = new LevelBot { AnswerPlanFor = _ => AnswerPlan.Miss })
            {
                MathQuestion question = null;
                GameEvents.QuestionShown += Capture;
                void Capture(MathQuestion q, float limit) => question = q;
                try
                {
                    yield return Until(bot, () => question != null);
                    _game.TogglePause();
                    _input.NotifyAnswerChosen(question.CorrectIndex);
                    Assert.That(_locked, Is.Empty, "A pick while paused must be ignored");
                    _game.TogglePause();
                    _input.NotifyAnswerChosen(question.CorrectIndex);
                    Assert.That(_locked, Is.EqualTo(new[] { question.CorrectIndex }));
                    yield return Until(bot, () => bot.Results.Count == 1);
                    Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Correct));
                }
                finally { GameEvents.QuestionShown -= Capture; }
            }
        }

        private IEnumerator LoadLevel(int number)
        {
            LevelSelection.Current = LevelCatalog.Load().Levels[number - 1];
            SceneManager.LoadScene(SceneFlow.Game);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            _lives = Object.FindFirstObjectByType<LivesSystem>();
            _player = Object.FindFirstObjectByType<PlayerController>();
            _input = Object.FindFirstObjectByType<InputReader>();
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
