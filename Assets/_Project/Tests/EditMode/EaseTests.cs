using MathRunner.UI;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class EaseTests
    {
        [Test]
        public void CurvesStartAtZeroAndEndAtOne([Values(0, 1, 2, 3)] int curve)
        {
            System.Func<float, float> ease = curve switch { 0 => Ease.Linear, 1 => Ease.OutQuad, 2 => Ease.InQuad, _ => Ease.OutBack };
            Assert.That(ease(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(ease(1f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void InputIsClampedToTheZeroToOneRange()
        {
            Assert.That(Ease.OutQuad(-2f), Is.EqualTo(0f));
            Assert.That(Ease.OutQuad(3f), Is.EqualTo(1f));
            Assert.That(Ease.OutBack(5f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void OutBackOvershootsBeforeSettling()
        {
            Assert.That(Ease.OutBack(0.7f), Is.GreaterThan(1f));
        }

        [Test]
        public void OutQuadIsAheadOfLinearAndInQuadBehind()
        {
            Assert.That(Ease.OutQuad(0.5f), Is.EqualTo(0.75f).Within(1e-5f));
            Assert.That(Ease.InQuad(0.5f), Is.EqualTo(0.25f).Within(1e-5f));
        }

        [Test]
        public void PulsePeaksInTheMiddleAndReturnsToZero()
        {
            Assert.That(Ease.Pulse(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(Ease.Pulse(0.5f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(Ease.Pulse(1f), Is.EqualTo(0f).Within(1e-5f));
        }
    }
}
