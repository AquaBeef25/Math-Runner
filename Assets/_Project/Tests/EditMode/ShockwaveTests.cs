using MathRunner.Enemies;
using NUnit.Framework;

namespace MathRunner.Tests.EditMode
{
    public class ShockwaveTests
    {
        [Test]
        public void GrowsFromZeroToTheFullRadius()
        {
            Assert.That(Shockwave.RadiusAt(0f, 9f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(Shockwave.RadiusAt(1f, 9f), Is.EqualTo(9f).Within(1e-5f));
        }

        [Test]
        public void KeepsGrowingThroughoutTheBurst()
        {
            float previous = -1f;
            for (int i = 0; i <= 20; i++)
            {
                float r = Shockwave.RadiusAt(i / 20f, 9f);
                Assert.That(r, Is.GreaterThan(previous));
                previous = r;
            }
        }

        [Test]
        public void ClampsOutsideTheBurst()
        {
            Assert.That(Shockwave.RadiusAt(-0.5f, 9f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(Shockwave.RadiusAt(1.5f, 9f), Is.EqualTo(9f).Within(1e-5f));
        }
    }
}
