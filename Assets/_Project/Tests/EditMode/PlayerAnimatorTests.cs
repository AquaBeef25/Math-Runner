using MathRunner.Player;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests
{
    public class PlayerAnimatorTests
    {
        [Test]
        public void RunsOnTheGroundAndIdlesWhenStopped()
        {
            Assert.That(PlayerAnimator.PickPose(true, new Vector2(6f, 0f), false), Is.EqualTo(PlayerPose.Run));
            Assert.That(PlayerAnimator.PickPose(true, Vector2.zero, false), Is.EqualTo(PlayerPose.Idle));
        }

        [Test]
        public void JumpsWhileRisingAndFallsWhileDropping()
        {
            Assert.That(PlayerAnimator.PickPose(false, new Vector2(6f, 5f), false), Is.EqualTo(PlayerPose.Jump));
            Assert.That(PlayerAnimator.PickPose(false, new Vector2(6f, -5f), false), Is.EqualTo(PlayerPose.Fall));
        }

        [Test]
        public void HurtOverridesEveryOtherPose()
        {
            Assert.That(PlayerAnimator.PickPose(true, new Vector2(6f, 0f), true), Is.EqualTo(PlayerPose.Hurt));
            Assert.That(PlayerAnimator.PickPose(false, new Vector2(6f, 5f), true), Is.EqualTo(PlayerPose.Hurt));
        }
    }
}
