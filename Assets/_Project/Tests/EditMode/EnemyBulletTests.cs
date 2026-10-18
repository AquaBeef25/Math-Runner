using MathRunner.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests.EditMode
{
    public class EnemyBulletTests
    {
        [Test]
        public void LastStepLandsExactlyOnTheTarget()
        {
            var target = new Vector2(3f, -1f);
            Assert.That(EnemyBullet.Step(new Vector2(10f, 5f), target, 0.1f, 0.1f), Is.EqualTo(target));
            Assert.That(EnemyBullet.Step(new Vector2(10f, 5f), target, 0.1f, 0.5f), Is.EqualTo(target));
        }

        [Test]
        public void HalfTheRemainingTimeCoversHalfTheDistance()
        {
            var p = EnemyBullet.Step(new Vector2(0f, 0f), new Vector2(10f, 4f), 2f, 1f);
            Assert.That(p.x, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(p.y, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void NeverOvershootsTheTarget()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 50; i++)
            {
                var from = new Vector2((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 10f);
                var to = new Vector2((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 10f);
                float remaining = 0.01f + (float)rng.NextDouble() * 3f;
                float dt = (float)rng.NextDouble() * 4f;
                var p = EnemyBullet.Step(from, to, remaining, dt);
                Assert.That(Vector2.Distance(from, p), Is.LessThanOrEqualTo(Vector2.Distance(from, to) + 1e-4f));
            }
        }
    }
}
