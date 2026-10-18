using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MathRunner.Core;
using MathRunner.Enemies;
using MathRunner.Input;
using MathRunner.Math;
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
    // Enemy encounters in Sandbox_Level: encounter 0 gives 6 s, 1 gives 4 s, 2 gives 3 s, 3 gives 2 s of slowed bullet flight.
    public class EncounterTests
    {
        private const string LevelScene = "Sandbox_Level";
        private const int MaxFrames = 9000;

        private string _savePath;
        private float _baseFixedDelta;
        private GameManager _game;
        private LivesSystem _lives;
        private PlayerController _player;
        private InputReader _input;
        private readonly List<int> _locked = new List<int>();
        private int _focusStarts;

        private void OnLocked(int index) => _locked.Add(index);
        private void OnFocusStarted(float factor) => _focusStarts++;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mathrunner-encounter-" + Guid.NewGuid() + ".json");
            SaveSystem.UseFile(_savePath);
            _baseFixedDelta = Time.fixedDeltaTime;
            Time.captureDeltaTime = 0.02f;
            _locked.Clear();
            _focusStarts = 0;
            GameEvents.AnswerLocked += OnLocked;
            GameEvents.FocusStarted += OnFocusStarted;
            SceneManager.LoadScene(LevelScene);
            yield return null;
            yield return null;
            FindSceneObjects();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.AnswerLocked -= OnLocked;
            GameEvents.FocusStarted -= OnFocusStarted;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixedDelta;
            if (System.IO.File.Exists(_savePath)) System.IO.File.Delete(_savePath);
            SaveSystem.UseFile(null);
        }

        [UnityTest]
        public IEnumerator CorrectPickDestroysEnemyAndBullet()
        {
            using (var bot = new LevelBot())
            {
                yield return PlayUntil(bot, () => _focusStarts > 0);
                var encounter = ActiveEncounter();
                var enemy = encounter.Enemy;
                var bullet = encounter.Bullet;
                yield return PlayUntil(bot, () => bot.Results.Count > 0);
                yield return Frames(bot, 3);

                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Correct));
                Assert.That(bot.Hits, Has.No.Member(HitCause.Bullet));
                Assert.That(_lives.Lives, Is.EqualTo(3));
                Assert.That(enemy == null, Is.True, "The shockwave should destroy the enemy");
                Assert.That(bullet == null, Is.True, "The shockwave should remove the bullet");
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
        }

        [UnityTest]
        public IEnumerator WrongPickLetsTheBulletHitOnce()
        {
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = n => n == 0 ? AnswerPlan.Wrong : AnswerPlan.Correct;
                yield return PlayUntil(bot, () => _focusStarts > 0);
                var encounter = ActiveEncounter();
                var enemy = encounter.Enemy;
                var bullet = encounter.Bullet;
                yield return PlayUntil(bot, () => bot.Hits.Contains(HitCause.Bullet));
                yield return Frames(bot, 150); // 3 s

                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Wrong));
                Assert.That(bot.Hits.Count(h => h == HitCause.Bullet), Is.EqualTo(1));
                Assert.That(_lives.Lives, Is.EqualTo(2));
                Assert.That(bullet == null, Is.True, "The bullet should be gone after it hits");
                Assert.That(enemy == null, Is.True, "The enemy should have flown off and been removed");
            }
        }

        [UnityTest]
        public IEnumerator NoPickIsMissedWhenTheBulletLands()
        {
            using (var bot = new LevelBot())
            {
                bot.PickFor = (n, q) => n == 1 ? -1 : q.CorrectIndex;
                yield return PlayUntil(bot, () => _focusStarts > 1);
                float realSeconds = 0f;
                int frames = 0;
                while (!bot.Hits.Contains(HitCause.Bullet) && frames++ < MaxFrames)
                {
                    bot.BeforeFrame();
                    yield return null;
                    realSeconds += Time.captureDeltaTime; // one simulated real-time step (unscaledDeltaTime is wall-clock here)
                    bot.AfterFrame();
                }
                yield return Frames(bot, 2);

                Assert.That(bot.Results[1].Outcome, Is.EqualTo(AnswerOutcome.Missed));
                Assert.That(bot.Hits.Count(h => h == HitCause.Bullet), Is.EqualTo(1));
                Assert.That(_lives.Lives, Is.EqualTo(2));
                Assert.That(realSeconds, Is.EqualTo(4f).Within(0.04f), "The bullet takes the level's time limit in real seconds");
            }
        }

        [UnityTest]
        public IEnumerator TimeSlowsDuringFocus()
        {
            using (var bot = new LevelBot())
            {
                bot.PickDelaySeconds = 2f;
                yield return PlayUntil(bot, () => _focusStarts > 0);
                yield return Frames(bot, 5);

                Assert.That(ActiveEncounter().IsFocusActive, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(0.2f).Within(1e-5f));
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(_baseFixedDelta * 0.2f).Within(1e-5f));
            }
        }

        [UnityTest]
        public IEnumerator FirstPickLocksAndLaterPicksAreIgnored()
        {
            using (var bot = new LevelBot())
            {
                bot.PickFor = (n, q) => n == 0 ? -1 : q.CorrectIndex;
                yield return PlayUntil(bot, () => _focusStarts > 0);
                _input.NotifyAnswerChosen(1);
                _input.NotifyAnswerChosen(2);
                yield return PlayUntil(bot, () => bot.Results.Count > 0);

                Assert.That(_locked, Is.EqualTo(new[] { 1 }));
                Assert.That(bot.Results[0].ChosenIndex, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator PauseDuringFocusResumesSlowMotion()
        {
            using (var bot = new LevelBot())
            {
                bot.PickDelaySeconds = 3f;
                yield return PlayUntil(bot, () => _focusStarts > 0);
                yield return Frames(bot, 2);

                _game.TogglePause();
                Assert.That(Time.timeScale, Is.Zero);
                _game.TogglePause();
                Assert.That(Time.timeScale, Is.EqualTo(0.2f).Within(1e-5f), "Unpausing must return to slow-mo, not full speed");
            }
        }

        [UnityTest]
        public IEnumerator BulletHitIgnoresInvincibility()
        {
            using (var bot = new LevelBot())
            {
                bot.PickFor = (n, q) => n == 0 ? -1 : q.CorrectIndex;
                yield return PlayUntil(bot, () => _focusStarts > 0);
                _player.GetComponent<PlayerHealth>().HitByObstacle();
                Assert.That(_lives.Lives, Is.EqualTo(2));
                yield return PlayUntil(bot, () => bot.Hits.Contains(HitCause.Bullet));

                Assert.That(_lives.Lives, Is.EqualTo(1), "A spike hit just before must not cancel the bullet");
            }
        }

        [UnityTest]
        public IEnumerator LastHeartLostToBulletEndsTheGameFrozen()
        {
            GameEvents.RaisePlayerHit(HitCause.Obstacle);
            GameEvents.RaisePlayerHit(HitCause.Obstacle);
            Assert.That(_lives.Lives, Is.EqualTo(1));
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = _ => AnswerPlan.Wrong;
                yield return PlayUntil(bot, () => bot.Hits.Contains(HitCause.Bullet));
                yield return null;
                yield return null;

                Assert.That(_game.State, Is.EqualTo(GameState.GameOver));
                Assert.That(Time.timeScale, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator RestartDuringFocusRestoresNormalTime()
        {
            using (var bot = new LevelBot())
            {
                bot.PickDelaySeconds = 3f;
                yield return PlayUntil(bot, () => _focusStarts > 0);
                yield return Frames(bot, 2);
            }

            _game.Restart();
            yield return null;
            yield return null;
            FindSceneObjects();

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(_baseFixedDelta).Within(1e-6f));
            Assert.That(WorldTime.FocusFactor, Is.EqualTo(1f));
        }

        private static EnemyEncounter ActiveEncounter() =>
            Object.FindObjectsByType<EnemyEncounter>(FindObjectsSortMode.None).First(e => e.IsFocusActive);

        private static IEnumerator PlayUntil(LevelBot bot, Func<bool> done)
        {
            int frames = 0;
            while (!done() && frames++ < MaxFrames)
            {
                bot.BeforeFrame();
                yield return null;
                bot.AfterFrame();
            }
            Assert.That(done(), Is.True, "Timed out waiting for the condition");
        }

        private static IEnumerator Frames(LevelBot bot, int count)
        {
            for (int i = 0; i < count; i++)
            {
                bot.BeforeFrame();
                yield return null;
                bot.AfterFrame();
            }
        }

        private void FindSceneObjects()
        {
            _game = Object.FindFirstObjectByType<GameManager>();
            _lives = Object.FindFirstObjectByType<LivesSystem>();
            _player = Object.FindFirstObjectByType<PlayerController>();
            _input = Object.FindFirstObjectByType<InputReader>();
        }
    }
}
