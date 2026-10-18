using MathRunner.Audio;
using NUnit.Framework;
using UnityEditor;

namespace MathRunner.Tests.EditMode
{
    public class AudioMathTests
    {
        [TestCase(0, 1f)]
        [TestCase(1, 1f)]
        [TestCase(2, 1.06f)]
        [TestCase(5, 1.24f)]
        [TestCase(9, 1.48f)]
        [TestCase(50, 1.48f)] // capped at 8 steps
        public void StreakRaisesPitchUpToACap(int streak, float expected)
        {
            Assert.That(AudioMath.StreakPitch(streak, 0.06f, 8), Is.EqualTo(expected).Within(1e-4f));
        }

        [TestCase(3.02f, 2.98f, true)]  // crossed 3
        [TestCase(2.01f, 1.99f, true)]  // crossed 2
        [TestCase(1.01f, 0.99f, true)]  // crossed 1
        [TestCase(4.02f, 3.98f, false)] // 4 is above the tick window
        [TestCase(2.50f, 2.40f, false)] // no whole second crossed
        [TestCase(0.02f, -0.01f, false)] // timeout, not a tick
        [TestCase(2.98f, 3.02f, false)] // time never goes up
        [TestCase(4.50f, 2.90f, true)]  // a long frame that skips past 3 still ticks
        public void TicksOnlyForTheLastSeconds(float previous, float remaining, bool expected)
        {
            Assert.That(AudioMath.CrossedTick(previous, remaining, 3), Is.EqualTo(expected));
        }

        [Test]
        public void ShippedSoundSetHasAClipForEverySound()
        {
            var set = AssetDatabase.LoadAssetAtPath<SoundSet>("Assets/_Project/ScriptableObjects/Audio/SoundSet.asset");
            Assert.That(set, Is.Not.Null, "SoundSet asset missing");
            foreach (var sound in set.All) Assert.That(sound.ClipCount, Is.GreaterThan(0));
        }
    }
}
