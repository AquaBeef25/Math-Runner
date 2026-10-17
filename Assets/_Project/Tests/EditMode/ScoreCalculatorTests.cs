using MathRunner.Systems;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class ScoreCalculatorTests
    {
        [Test]
        public void BasePointsScaleWithLevel()
        {
            Assert.That(ScoreCalculator.PointsForCorrect(1, 0, false, 100, 50, 25), Is.EqualTo(100));
            Assert.That(ScoreCalculator.PointsForCorrect(5, 0, false, 100, 50, 25), Is.EqualTo(500));
            Assert.That(ScoreCalculator.PointsForCorrect(10, 0, false, 100, 50, 25), Is.EqualTo(1000));
        }

        [Test]
        public void TimedQuestionsAddAFlatBonus()
        {
            Assert.That(ScoreCalculator.PointsForCorrect(5, 0, true, 100, 50, 25), Is.EqualTo(550));
        }

        [Test]
        public void StreakBonusCountsCorrectAnswersAlreadyInARow()
        {
            Assert.That(ScoreCalculator.PointsForCorrect(5, 1, false, 100, 50, 25), Is.EqualTo(525));
            Assert.That(ScoreCalculator.PointsForCorrect(5, 4, true, 100, 50, 25), Is.EqualTo(650));
        }
    }
}
