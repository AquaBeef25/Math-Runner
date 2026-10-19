using MathRunner.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests
{
    // How bee and fly reach the runner: always exactly when the question's time runs out.
    public class AttackPathTests
    {
        [Test]
        public void AttackArrivesExactlyAtTheDeadline([Values(1f, 3f, 6f)] float seconds)
        {
            var position = new Vector2(10f, 4f);
            var target = new Vector2(0f, 0.5f);
            float remaining = seconds;
            const float dt = 0.02f;
            while (remaining > 0f)
            {
                target += new Vector2(6f * dt, 0f); // the runner keeps running
                position = AttackPath.Charge(position, target, remaining, dt);
                remaining -= dt;
            }
            Assert.That(Vector2.Distance(position, target), Is.LessThan(0.05f));
        }

        [Test]
        public void BuzzIsStillAtArrival()
        {
            for (float t = 0f; t < 3f; t += 0.13f)
            {
                Assert.That(AttackPath.Buzz(t, 0.6f, 3f, 0f).magnitude, Is.EqualTo(0f).Within(1e-5f), "no wobble when it arrives");
                Assert.That(AttackPath.Buzz(t, 0.6f, 3f, 1f).magnitude, Is.LessThanOrEqualTo(0.6f + 1e-4f));
            }
        }

        [Test]
        public void ChargingEnemyHasNoSolidCollider()
        {
            var go = new GameObject("Bee");
            go.AddComponent<ChargingEnemy>();
            foreach (var c in go.GetComponentsInChildren<Collider2D>(true)) Assert.That(c.isTrigger, Is.True);
            Object.DestroyImmediate(go);
        }
    }
}
