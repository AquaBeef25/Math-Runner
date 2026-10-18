using System.Collections;
using MathRunner.Level;
using MathRunner.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MathRunner.Tests.PlayMode
{
    // Ramps built at runtime in Sandbox_Level (flat ground at y 0 until x 70; the first spike is at x 12):
    // up 1 tile from x 4 to 6, flat at 1 until 8, down to 0 by 10.
    public class SlopeTests
    {
        private const float UpStart = 4f, UpEnd = 6f, DownStart = 8f, DownEnd = 10f;

        private PlayerController _player;
        private Rigidbody2D _rb;
        private Collider2D _col;
        private MathRunner.Input.InputReader _input;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.captureDeltaTime = 0.02f;
            SceneManager.LoadScene("Sandbox_Level");
            yield return null;
            yield return null;
            _player = Object.FindFirstObjectByType<PlayerController>();
            _rb = _player.GetComponent<Rigidbody2D>();
            _col = _player.GetComponent<Collider2D>();
            _input = Object.FindFirstObjectByType<MathRunner.Input.InputReader>();
            BuildRamp(UpStart, UpEnd, 0f, 1f);
            BuildRamp(UpEnd, DownStart, 1f, 1f);
            BuildRamp(DownStart, DownEnd, 1f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        // Sandbox_Level has a real pit from x 70 to 73; fill it with water and run in without jumping.
        [UnityTest]
        public IEnumerator WaterCostsOneHeartAndRespawns()
        {
            var lives = Object.FindFirstObjectByType<MathRunner.Systems.LivesSystem>();
            var pool = new GameObject("TestWater");
            pool.AddComponent<WaterPool>().SetRect(70f, 73f, -0.6f, -4f);
            int falls = 0;
            System.Action<MathRunner.Core.HitCause> onHit = c => { if (c == MathRunner.Core.HitCause.Fall) falls++; };
            MathRunner.Core.GameEvents.PlayerHit += onHit;
            try
            {
                yield return RunTo(69f);
                int livesBefore = lives.Lives;
                int guard = 0;
                while (falls == 0 && guard++ < 300) yield return null;
                yield return null;

                Assert.That(falls, Is.EqualTo(1), "falling into the water should count as one fall");
                Assert.That(lives.Lives, Is.EqualTo(livesBefore - 1));
                Assert.That(_player.transform.position.y, Is.GreaterThan(-0.5f), "respawned out of the water (at the last checkpoint)");
            }
            finally { MathRunner.Core.GameEvents.PlayerHit -= onHit; }
        }

        private static void BuildRamp(float start, float end, float from, float to)
        {
            var go = new GameObject("TestRamp") { layer = LayerMask.NameToLayer("Ground") };
            go.AddComponent<RampPiece>().SetRamp(start, end, from, to, -1f);
        }

        private static float Surface(float x)
        {
            if (x <= UpStart || x >= DownEnd) return 0f;
            if (x < UpEnd) return (x - UpStart) / (UpEnd - UpStart);
            if (x <= DownStart) return 1f;
            return 1f - (x - DownStart) / (DownEnd - DownStart);
        }

        private IEnumerator RunTo(float x)
        {
            int guard = 0;
            while (_player.transform.position.x < x && guard++ < 2000) yield return null;
        }

        [UnityTest]
        public IEnumerator RunnerKeepsSpeedOverARamp()
        {
            yield return RunTo(UpStart - 0.5f);
            float minSpeed = float.MaxValue;
            while (_player.transform.position.x < DownEnd + 0.5f)
            {
                yield return null;
                minSpeed = Mathf.Min(minSpeed, _rb.linearVelocity.x);
            }
            Assert.That(minSpeed, Is.GreaterThanOrEqualTo(_player.RunSpeed * 0.95f), "the runner slowed down on the ramp");
        }

        [UnityTest]
        public IEnumerator RunnerStaysGroundedOverACrest()
        {
            yield return RunTo(UpStart + 0.3f);
            int airborne = 0;
            float worstGap = 0f;
            string where = "";
            float worstX = 0f;
            while (_player.transform.position.x < DownEnd - 0.3f)
            {
                yield return null;
                if (!_player.IsGrounded)
                {
                    airborne++;
                    if (where.Length < 200) where += $"{_player.transform.position.x:F2} ";
                }
                float gap = _col.bounds.min.y - Surface(_player.transform.position.x);
                if (gap > worstGap) { worstGap = gap; worstX = _player.transform.position.x; }
            }
            Assert.That(airborne, Is.Zero, "left the ground running over the ramps at x: " + where);
            // A box on a 27° slope rests on its corner, so its bottom sits half-width × slope above the centre line; allow
            // one physics step of climb on top of that (a real launch also shows up as airborne frames above).
            float allowed = _col.bounds.extents.x * 0.5f + 0.1f;
            Assert.That(worstGap, Is.LessThanOrEqualTo(allowed), $"launched off a crest at x {worstX:F2}");
        }

        [UnityTest]
        public IEnumerator JumpFromARampReachesFullHeight()
        {
            yield return RunTo((UpStart + UpEnd) * 0.5f);
            float y0 = _col.bounds.min.y, peak = y0;
            _input.NotifyJumpPressed();
            int frames = 0;
            do
            {
                yield return null;
                peak = Mathf.Max(peak, _col.bounds.min.y);
            } while ((!_player.IsGrounded || frames++ < 5) && frames < 300);
            _input.NotifyJumpReleased();
            Assert.That(peak - y0, Is.GreaterThanOrEqualTo(2.95f));
        }
    }
}
