using MathRunner.Level;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class JumpReachTests
    {
        private static LevelPlanSettings S(float speed = 6f) => new LevelPlanSettings
        {
            RunSpeed = speed, JumpHeight = 3.2f, Gravity = -9.81f, GravityScale = 4f, FallMultiplier = 1.4f,
        };

        [Test]
        public void FullJumpOnFlatGroundMatchesTheMeasuredJump()
        {
            // The frame-stepped player measured ~0.76 s from take-off back to the same ground.
            Assert.That(JumpReach.AirTime(S(), 0f), Is.EqualTo(0.76f).Within(0.03f));
            Assert.That(JumpReach.Distance(S(), 0f), Is.EqualTo(0.76f * 6f).Within(0.2f));
        }

        [Test]
        public void CannotReachAStepTallerThanTheJump()
        {
            Assert.That(JumpReach.CanReach(S(), 1f, 4f), Is.False);
            Assert.That(JumpReach.CanReach(S(), 1f, 3.2f), Is.False, "needs margin above the exact jump height");
        }

        [Test]
        public void CanReachATwoTileStepTwoUnitsAway()
        {
            Assert.That(JumpReach.CanReach(S(), 2f, 2f), Is.True);
        }

        [Test]
        public void TooFarIsUnreachable()
        {
            Assert.That(JumpReach.CanReach(S(), JumpReach.Distance(S(), 0f), 0f), Is.False, "needs 15 % margin");
            Assert.That(JumpReach.CanReach(S(), JumpReach.Distance(S(), 0f) * 0.8f, 0f), Is.True);
        }

        [Test]
        public void RiseTimeGrowsWithHeight()
        {
            Assert.That(JumpReach.RiseTime(S(), 2f), Is.GreaterThan(JumpReach.RiseTime(S(), 1f)));
            Assert.That(JumpReach.RiseTime(S(), 1f), Is.GreaterThan(0f));
        }
    }
}
