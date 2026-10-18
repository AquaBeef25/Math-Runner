using MathRunner.Core;
using NUnit.Framework;

namespace MathRunner.Tests.EditMode
{
    public class WorldTimeTests
    {
        [TestCase(true, 0.2f, 0f)]
        [TestCase(true, 1f, 0f)]
        [TestCase(false, 0.2f, 0.2f)]
        [TestCase(false, 1f, 1f)]
        public void PauseWinsOverFocus(bool paused, float focus, float expected)
        {
            Assert.That(WorldTime.Combine(paused, focus), Is.EqualTo(expected).Within(1e-6f));
        }
    }
}
