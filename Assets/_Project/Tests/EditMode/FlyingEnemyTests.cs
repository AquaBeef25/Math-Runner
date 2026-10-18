using MathRunner.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests.EditMode
{
    public class FlyingEnemyTests
    {
        private static readonly Vector2 Player = new Vector2(12f, 0.5f);
        private static readonly Vector2 Offset = new Vector2(7f, 4f);

        [Test]
        public void StartsExactlyAtTheHoverOffset()
        {
            Assert.That(FlyingEnemy.HoverPoint(Player, Offset, 0f, 0.25f, 1.5f), Is.EqualTo(Player + Offset));
        }

        // When the player jumps or climbs a step, the enemy follows smoothly instead of snapping.
        [Test]
        public void HoverHeightFollowsThePlayerSmoothly()
        {
            float y = 0f, velocity = 0f, previousGap = 5f;
            for (int i = 0; i < 100; i++) // 2 s of 0.02 s steps
            {
                y = FlyingEnemy.SmoothY(y, 5f, ref velocity, 0.25f, 0.02f);
                Assert.That(y, Is.LessThanOrEqualTo(5f + 1e-4f), "never overshoots");
                Assert.That(5f - y, Is.LessThanOrEqualTo(previousGap + 1e-4f), "always closing in");
                previousGap = 5f - y;
            }
            Assert.That(y, Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void BobsOnlyVerticallyWithinTheAmplitude()
        {
            for (int i = 0; i <= 50; i++)
            {
                var p = FlyingEnemy.HoverPoint(Player, Offset, i * 0.02f, 0.25f, 1.5f);
                Assert.That(p.x, Is.EqualTo(Player.x + Offset.x).Within(1e-5f));
                Assert.That(Mathf.Abs(p.y - (Player.y + Offset.y)), Is.LessThanOrEqualTo(0.25f + 1e-5f));
            }
        }
    }
}
