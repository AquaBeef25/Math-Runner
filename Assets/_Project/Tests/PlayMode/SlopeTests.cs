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
            while (_player.transform.position.x < DownEnd - 0.3f)
            {
                yield return null;
                if (!_player.IsGrounded) airborne++;
                float gap = _col.bounds.min.y - Surface(_player.transform.position.x);
                worstGap = Mathf.Max(worstGap, gap);
            }
            Assert.That(airborne, Is.Zero, "left the ground running over the ramps");
            // A box on a 27° slope rests on its corner, so its bottom can sit up to half-width × slope above the centre line.
            Assert.That(worstGap, Is.LessThanOrEqualTo(0.3f), "launched off a crest");
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
