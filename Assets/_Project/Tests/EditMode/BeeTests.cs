using MathRunner.Level;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class BeeTests
    {
        [Test]
        public void BobStaysWithinAmplitude()
        {
            float lowest = float.MaxValue, highest = float.MinValue;
            for (float t = 0f; t < 5f; t += 0.01f)
            {
                float y = Bee.BobY(2f, 0.4f, 1.2f, t);
                lowest = System.Math.Min(lowest, y);
                highest = System.Math.Max(highest, y);
            }
            Assert.That(lowest, Is.GreaterThanOrEqualTo(1.6f - 1e-4f));
            Assert.That(highest, Is.LessThanOrEqualTo(2.4f + 1e-4f));
            Assert.That(highest - lowest, Is.GreaterThan(0.7f), "the bee should actually bob");
        }
    }
}
