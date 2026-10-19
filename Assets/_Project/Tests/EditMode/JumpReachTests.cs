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
    
        // A short hop: the button comes up after `hold` seconds (never before the 0.08 s minimum), then the rising speed is
        // halved. Hand-checked: v0 = 15.85, g = 39.24.
        [Test]
        public void CutJumpPeakMatchesTheHop()
        {
            Assert.That(MathRunner.Player.JumpMath.CutJumpPeak(3.2f, -9.81f, 4f, 0.1f, 0.08f, 0.5f), Is.EqualTo(1.84f).Within(0.02f));
            Assert.That(MathRunner.Player.JumpMath.CutJumpPeak(3.2f, -9.81f, 4f, 0f, 0.08f, 0.5f), Is.EqualTo(1.66f).Within(0.02f), "the minimum tap");
            Assert.That(MathRunner.Player.JumpMath.CutJumpPeak(3.2f, -9.81f, 4f, 1f, 0.08f, 0.5f), Is.EqualTo(3.2f).Within(1e-3f), "held past the top = full jump");
        }

        [Test]
        public void CutJumpLandsSoonerThanAFullJump()
        {
            float hop = MathRunner.Player.JumpMath.CutJumpAirTime(3.2f, -9.81f, 4f, 1.4f, 0f, 0.08f, 0.5f, 0f);
            Assert.That(hop, Is.GreaterThan(0.3f));
            Assert.That(hop, Is.LessThan(JumpReach.AirTime(S(), 0f)));
            Assert.That(float.IsPositiveInfinity(MathRunner.Player.JumpMath.CutJumpAirTime(3.2f, -9.81f, 4f, 1.4f, 0f, 0.08f, 0.5f, 2.5f)), Is.True,
                "a ledge above the hop's peak is never landed on");
        }

        // The fork's middle ledge (+1) must be reachable by the shortest tap; its high ledge (+2.7) never by a hop.
        [Test]
        public void HopReachesTheMiddleLedgeButNotTheHighOne()
        {
            Assert.That(JumpReach.HopPeak(S()), Is.EqualTo(1.66f).Within(0.02f));
            Assert.That(JumpReach.CanHopReach(S(), 1f, 1f), Is.True);
            Assert.That(JumpReach.CanHopReach(S(), 1f, 2.7f), Is.False);
            Assert.That(JumpReach.CanReach(S(), 1f, 2.7f), Is.True, "but a full jump does");
        }
    }
}
