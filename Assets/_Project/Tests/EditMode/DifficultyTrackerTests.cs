using MathRunner.Math;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class DifficultyTrackerTests
    {
        [Test]
        public void StartsAtNormal()
        {
            Assert.That(new DifficultyTracker().Tier, Is.EqualTo(DifficultyTier.Normal));
        }

        [Test]
        public void ThreeCorrectInARowStepsUp()
        {
            var t = new DifficultyTracker();
            t.RecordCorrect();
            t.RecordCorrect();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Normal));
            t.RecordCorrect();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Hard));
        }

        [Test]
        public void NeverGoesAboveHard()
        {
            var t = new DifficultyTracker();
            for (int i = 0; i < 12; i++) t.RecordCorrect();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Hard));
        }

        [Test]
        public void WrongAnswerStepsDownAndNeverBelowEasy()
        {
            var t = new DifficultyTracker(DifficultyTier.Hard);
            t.RecordWrong();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Normal));
            t.RecordWrong();
            t.RecordWrong();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Easy));
        }

        [Test]
        public void WrongAnswerResetsTheStreak()
        {
            var t = new DifficultyTracker();
            t.RecordCorrect();
            t.RecordCorrect();
            t.RecordWrong();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Easy));
            t.RecordCorrect();
            t.RecordCorrect();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Easy));
            t.RecordCorrect();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Normal));
        }

        [Test]
        public void StreakStartsOverAfterSteppingUp()
        {
            var t = new DifficultyTracker(DifficultyTier.Easy);
            for (int i = 0; i < 3; i++) t.RecordCorrect();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Normal));
            t.RecordCorrect();
            t.RecordCorrect();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Normal));
            t.RecordCorrect();
            Assert.That(t.Tier, Is.EqualTo(DifficultyTier.Hard));
        }
    }
}
