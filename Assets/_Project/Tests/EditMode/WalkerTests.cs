using MathRunner.Level;
using NUnit.Framework;

namespace MathRunner.Tests.EditMode
{
    public class WalkerTests
    {
        [Test]
        public void WalkerTurnsAtItsBounds()
        {
            bool hitLeft = false, hitRight = false;
            for (int i = 0; i <= 400; i++)
            {
                float x = Walker.PatrolX(10f, 11.5f, 1f, i * 0.01f);
                Assert.That(x, Is.InRange(10f - 1e-4f, 11.5f + 1e-4f));
                if (x < 10.01f) hitLeft = true;
                if (x > 11.49f) hitRight = true;
            }
            Assert.That(hitLeft && hitRight, Is.True, "patrols the whole range in one 3 s round trip");
        }

        [Test]
        public void StartsAtTheLeftBound()
        {
            Assert.That(Walker.PatrolX(4f, 5.5f, 1f, 0f), Is.EqualTo(4f).Within(1e-5f));
        }
    }
}
