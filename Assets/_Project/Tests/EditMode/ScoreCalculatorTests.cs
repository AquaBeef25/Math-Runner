using MathRunner.Player;
using MathRunner.Systems;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class ScoreCalculatorTests
    {
        [Test]
        public void BasePointsScaleWithLevel()
        {
            Assert.That(ScoreCalculator.PointsForCorrect(1, 0, 0f, 100, 50, 25), Is.EqualTo(100));
            Assert.That(ScoreCalculator.PointsForCorrect(5, 0, 0f, 100, 50, 25), Is.EqualTo(500));
            Assert.That(ScoreCalculator.PointsForCorrect(10, 0, 0f, 100, 50, 25), Is.EqualTo(1000));
        }

        [Test]
        public void SpeedBonusScalesWithTimeLeftWhenPicking()
        {
            Assert.That(ScoreCalculator.PointsForCorrect(5, 0, 1f, 100, 50, 25), Is.EqualTo(550), "Instant pick");
            Assert.That(ScoreCalculator.PointsForCorrect(5, 0, 0.5f, 100, 50, 25), Is.EqualTo(525), "Half the time left");
            Assert.That(ScoreCalculator.PointsForCorrect(5, 0, 0.02f, 100, 50, 25), Is.EqualTo(501), "Last moment");
        }

        [Test]
        public void SpeedBonusFractionIsClamped()
        {
            Assert.That(ScoreCalculator.PointsForCorrect(5, 0, 1.5f, 100, 50, 25), Is.EqualTo(550));
            Assert.That(ScoreCalculator.PointsForCorrect(5, 0, -1f, 100, 50, 25), Is.EqualTo(500));
        }

        [Test]
        public void StreakBonusCountsCorrectAnswersAlreadyInARow()
        {
            Assert.That(ScoreCalculator.PointsForCorrect(5, 1, 0f, 100, 50, 25), Is.EqualTo(525));
            Assert.That(ScoreCalculator.PointsForCorrect(5, 4, 1f, 100, 50, 25), Is.EqualTo(650));
        }
    }

    public class JumpMathTests
    {
        // PlayerController defaults: jump height 3.2, gravity -9.81 × scale 4, falling ×1.4.
        private static float AirTime(float target) => JumpMath.AirTimeToLand(3.2f, -9.81f, 4f, 1.4f, target);

        [Test]
        public void AirTimeToAnswerPlatformMatchesTheMeasuredJump()
        {
            // The frame-stepped bot measured ~0.65 s from take-off to landing on a 1.4-unit platform.
            Assert.That(AirTime(1.4f), Is.EqualTo(0.65f).Within(0.02f));
        }

        [Test]
        public void AirTimeBackToTheGroundMatchesTheMeasuredFullJump()
        {
            Assert.That(AirTime(0f), Is.EqualTo(0.76f).Within(0.03f));
        }

        [Test]
        public void HigherTargetsAreReachedSooner()
        {
            Assert.That(AirTime(2f), Is.LessThan(AirTime(1f)));
            Assert.That(AirTime(10f), Is.EqualTo(AirTime(3.2f)), "A target above the apex can't shorten the jump below the rise time");
        }
    }
}
