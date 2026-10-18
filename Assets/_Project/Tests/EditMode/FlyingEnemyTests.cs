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
