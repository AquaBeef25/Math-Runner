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
        private const float FocusScale = 0.5f;

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
                Assert.That(Time.timeScale, Is.EqualTo(FocusScale).Within(1e-5f));
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(_baseFixedDelta * FocusScale).Within(1e-5f));
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
                Assert.That(Time.timeScale, Is.EqualTo(FocusScale).Within(1e-5f), "Unpausing must return to slow-mo, not full speed");
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

        // No autopilot any more: the player keeps jumping while the question is open.
        [UnityTest]
        public IEnumerator PlayerCanJumpDuringFocus()
        {
            using (var bot = new LevelBot())
            {
                bot.PickDelaySeconds = 4f;
                yield return PlayUntil(bot, () => _focusStarts > 0);
                yield return Frames(bot, 3);
                bool jumped = false;
                _player.Jumped += () => jumped = true;
                float y0 = _player.transform.position.y;
                _input.NotifyJumpPressed();
                float peak = y0;
                for (int i = 0; i < 40; i++) { yield return null; peak = Mathf.Max(peak, _player.transform.position.y); }
                _input.NotifyJumpReleased();

                Assert.That(jumped, Is.True);
                Assert.That(peak - y0, Is.GreaterThan(1f));
            }
        }

        // Slow-mo only slows the jump down; it still goes exactly as high.
        [UnityTest]
        public IEnumerator JumpReachIsTheSameInSlowMotion()
        {
            using (var bot = new LevelBot())
            {
                bot.PickDelaySeconds = 5f;
                bot.ShouldJump = _ => false;
                yield return Frames(bot, 10);
                float normal = 0f;
                yield return MeasureFullJump(h => normal = h);
                yield return PlayUntil(bot, () => _focusStarts > 0);
                float slow = 0f;
                yield return MeasureFullJump(h => slow = h);

                // Within 5 %: slow-mo halves the physics step, so the arc is integrated a little more finely (measured 3.04 vs 3.12).
                Assert.That(slow, Is.EqualTo(normal).Within(normal * 0.05f));
            }
        }

        // A real-time tap must give the same jump in slow-mo: the hold is measured in the player's own seconds.
        [UnityTest]
        public IEnumerator ShortTapIsTheSameHeightInSlowMotion()
        {
            using (var bot = new LevelBot())
            {
                bot.PickDelaySeconds = 5f;
                bot.ShouldJump = _ => false;
                yield return Frames(bot, 10);
                float normal = 0f;
                yield return MeasureTap(8, h => normal = h); // 0.16 s of real time
                yield return PlayUntil(bot, () => _focusStarts > 0);
                float slow = 0f;
                yield return MeasureTap(8, h => slow = h);

                Assert.That(normal, Is.LessThan(2.7f), "a 0.16 s tap should be well short of a full jump");
                Assert.That(slow, Is.EqualTo(normal).Within(normal * 0.05f), $"tap height {normal:F2} at 1x vs {slow:F2} in focus");
            }
        }

        // Rising up through a one-way ledge must not count as standing on it.
        [UnityTest]
        public IEnumerator ShortHopUnderAOneWayPlatformIsNotGrounded()
        {
            for (int i = 0; i < 20; i++) yield return null;
            float groundY = _player.GetComponent<Collider2D>().bounds.min.y;
            float left = _player.transform.position.x + 2f;
            var ledge = new GameObject("TestLedge") { layer = LayerMask.NameToLayer("Ground") };
            ledge.transform.position = new Vector3(left + 1.5f, groundY + 2f, 0f);
            var col = ledge.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.4f);
            col.offset = new Vector2(0f, -0.2f);
            col.usedByEffector = true;
            ledge.AddComponent<PlatformEffector2D>().useOneWay = true;

            while (_player.transform.position.x < left + 0.3f) yield return null;
            int midAirLandings = 0;
            _player.Landed += _ => { if (_player.GetComponent<Collider2D>().bounds.min.y > groundY + 0.5f) midAirLandings++; };
            _input.NotifyJumpPressed();
            for (int i = 0; i < 5; i++) yield return null;
            _input.NotifyJumpReleased();
            int phantomGrounded = 0;
            for (int i = 0; i < 60; i++)
            {
                yield return null;
                float feet = _player.GetComponent<Collider2D>().bounds.min.y;
                if (_player.IsGrounded && feet > groundY + 0.5f && feet < groundY + 1.95f) phantomGrounded++;
            }

            Assert.That(phantomGrounded, Is.Zero, "grounded while inside the one-way ledge");
            Assert.That(midAirLandings, Is.Zero, "Landed fired in mid-air");
        }

        private IEnumerator MeasureTap(int frames, Action<float> report)
        {
            float y0 = _player.transform.position.y, peak = y0;
            _input.NotifyJumpPressed();
            for (int i = 0; i < frames; i++) { yield return null; peak = Mathf.Max(peak, _player.transform.position.y); }
            _input.NotifyJumpReleased();
            int guard = 0;
            do { yield return null; peak = Mathf.Max(peak, _player.transform.position.y); }
            while (!_player.IsGrounded && guard++ < 600);
            report(peak - y0);
        }

        private IEnumerator MeasureFullJump(Action<float> report)
        {
            float y0 = _player.transform.position.y, peak = y0;
            _input.NotifyJumpPressed();
            int frames = 0;
            do
            {
                yield return null;
                peak = Mathf.Max(peak, _player.transform.position.y);
            } while ((!_player.IsGrounded || frames++ < 5) && frames < 600);
            _input.NotifyJumpReleased();
            report(peak - y0);
        }

        // Random encounters: switch every encounter in the sandbox to one enemy kind.
        private static void UseEnemy(MathRunner.Level.EnemyKind kind)
        {
            foreach (var e in Object.FindObjectsByType<EnemyEncounter>(FindObjectsSortMode.None)) e.SetEnemy(kind, Vector2.zero);
        }

        [UnityTest]
        public IEnumerator BeeWrongCostsOneHeart() => WrongCostsOneHeart(MathRunner.Level.EnemyKind.Bee);

        [UnityTest]
        public IEnumerator FrogWrongCostsOneHeart() => WrongCostsOneHeart(MathRunner.Level.EnemyKind.Frog);

        private IEnumerator WrongCostsOneHeart(MathRunner.Level.EnemyKind kind)
        {
            UseEnemy(kind);
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = n => n == 0 ? AnswerPlan.Wrong : AnswerPlan.Correct;
                yield return PlayUntil(bot, () => _focusStarts > 0);
                var encounter = ActiveEncounter();
                Assert.That(encounter.Charger, Is.Not.Null, $"a {kind} should be attacking");
                Assert.That(encounter.Enemy == null, Is.True, "no alien");
                var charger = encounter.Charger;
                yield return PlayUntil(bot, () => bot.Hits.Contains(HitCause.Bullet));
                yield return Frames(bot, 150);

                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Wrong));
                Assert.That(bot.Hits.Count(h => h == HitCause.Bullet), Is.EqualTo(1));
                Assert.That(_lives.Lives, Is.EqualTo(2));
                Assert.That(charger == null, Is.True, $"the {kind} should have left after biting");
            }
        }

        [UnityTest]
        public IEnumerator BeeRightDefeatsIt()
        {
            UseEnemy(MathRunner.Level.EnemyKind.Bee);
            using (var bot = new LevelBot())
            {
                yield return PlayUntil(bot, () => _focusStarts > 0);
                var charger = ActiveEncounter().Charger;
                yield return PlayUntil(bot, () => bot.Results.Count > 0);
                yield return Frames(bot, 3);

                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Correct));
                Assert.That(_lives.Lives, Is.EqualTo(3));
                Assert.That(charger == null, Is.True, "the shockwave should pop the bee");
            }
        }

        [UnityTest]
        public IEnumerator AttackLandsWhenTimeRunsOut()
        {
            UseEnemy(MathRunner.Level.EnemyKind.Bee);
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
                    realSeconds += Time.captureDeltaTime;
                    bot.AfterFrame();
                }
                yield return Frames(bot, 2);

                Assert.That(bot.Results[1].Outcome, Is.EqualTo(AnswerOutcome.Missed));
                Assert.That(_lives.Lives, Is.EqualTo(2));
                Assert.That(realSeconds, Is.EqualTo(4f).Within(0.04f), "the bee arrives exactly when the time limit runs out");
            }
        }

        // Sets the encounters' enemies in running order (the rest stay aliens).
        private static void UseEnemies(params MathRunner.Level.EnemyKind[] kinds)
        {
            var ordered = Object.FindObjectsByType<EnemyEncounter>(FindObjectsSortMode.None).OrderBy(e => e.transform.position.x).ToList();
            for (int i = 0; i < ordered.Count; i++)
                ordered[i].SetEnemy(i < kinds.Length ? kinds[i] : MathRunner.Level.EnemyKind.Alien, Vector2.zero);
        }

        [UnityTest]
        public IEnumerator FlyWrongFollowsWithoutBiting()
        {
            UseEnemies(MathRunner.Level.EnemyKind.Fly);
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = n => n == 0 ? AnswerPlan.Wrong : AnswerPlan.Correct;
                yield return PlayUntil(bot, () => FollowingFly.Current != null);
                yield return Frames(bot, 50);

                Assert.That(FollowingFly.Current, Is.Not.Null, "the fly follows the runner");
                Assert.That(bot.Results[0].Outcome, Is.EqualTo(AnswerOutcome.Wrong));
                Assert.That(bot.Hits, Has.No.Member(HitCause.Bullet), "no bite yet");
                Assert.That(_lives.Lives, Is.EqualTo(3));
            }
        }

        [UnityTest]
        public IEnumerator FollowingFlyDiesOnNextRightAnswer()
        {
            UseEnemies(MathRunner.Level.EnemyKind.Fly);
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = n => n == 0 ? AnswerPlan.Wrong : AnswerPlan.Correct;
                yield return PlayUntil(bot, () => FollowingFly.Current != null);
                yield return PlayUntil(bot, () => bot.Results.Count > 1);
                yield return Frames(bot, 3);

                Assert.That(bot.Results[1].Outcome, Is.EqualTo(AnswerOutcome.Correct));
                Assert.That(FollowingFly.Current, Is.Null, "the explosion kills the following fly");
                Assert.That(_lives.Lives, Is.EqualTo(3));
            }
        }

        [UnityTest]
        public IEnumerator FollowingFlyBitesOnNextWrong()
        {
            UseEnemies(MathRunner.Level.EnemyKind.Fly);
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = n => n <= 1 ? AnswerPlan.Wrong : AnswerPlan.Correct;
                yield return PlayUntil(bot, () => FollowingFly.Current != null);
                yield return PlayUntil(bot, () => bot.Hits.Count(h => h == HitCause.Bullet) >= 2);
                yield return Frames(bot, 3);

                Assert.That(bot.Hits.Count(h => h == HitCause.Bullet), Is.EqualTo(2), "the alien's fireball and the fly's bite");
                Assert.That(_lives.Lives, Is.EqualTo(1), "it adds up: two hearts");
                Assert.That(FollowingFly.Current, Is.Null, "after biting, the fly flies away");
            }
        }

        [UnityTest]
        public IEnumerator FollowingFlyLeavesAtTheFinish()
        {
            UseEnemies(MathRunner.Level.EnemyKind.Fly);
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = n => n == 0 ? AnswerPlan.Wrong : AnswerPlan.Correct;
                yield return PlayUntil(bot, () => FollowingFly.Current != null);
                GameEvents.RaiseLevelCompleted();
                yield return Frames(bot, 3);

                Assert.That(FollowingFly.Current, Is.Null, "the fly leaves at the finish");
                Assert.That(_lives.Lives, Is.EqualTo(3), "without biting");
            }
        }

        [UnityTest]
        public IEnumerator FollowingFlyIsForgottenOnReload()
        {
            UseEnemies(MathRunner.Level.EnemyKind.Fly);
            using (var bot = new LevelBot())
            {
                bot.AnswerPlanFor = n => n == 0 ? AnswerPlan.Wrong : AnswerPlan.Correct;
                yield return PlayUntil(bot, () => FollowingFly.Current != null);
            }
            SceneManager.LoadScene(LevelScene);
            yield return null;
            yield return null;
            Assert.That(FollowingFly.Current, Is.Null, "a restart starts without a following fly");
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
