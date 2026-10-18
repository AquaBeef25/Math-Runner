using MathRunner.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests
{
    // How bee, frog and fly reach the runner: always exactly when the question's time runs out.
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
        public void LeapPeaksAboveBothEnds()
        {
            Assert.That(AttackPath.LeapHeight(0f, 2f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(AttackPath.LeapHeight(1f, 2f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(AttackPath.LeapHeight(0.5f, 2f), Is.EqualTo(2f).Within(1e-5f));
            for (float u = 0.05f; u < 1f; u += 0.05f) Assert.That(AttackPath.LeapHeight(u, 2f), Is.GreaterThan(0f));
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

        // The frog arrives hopping along the ground: never below the ground line, small hops, and it lands exactly on its spot.
        [Test]
        public void HopApproachStaysOnTheGround()
        {
            var from = new Vector2(14f, 0f);
            var to = new Vector2(6f, 0f);
            for (float k = 0f; k <= 1f; k += 0.01f)
            {
                var p = AttackPath.HopApproach(from, to, k, 0.8f, 3);
                Assert.That(p.y, Is.GreaterThanOrEqualTo(-1e-4f), "never below the ground");
                Assert.That(p.y, Is.LessThanOrEqualTo(0.8f + 1e-4f), "small hops, not flying");
            }
            Assert.That(Vector2.Distance(AttackPath.HopApproach(from, to, 1f, 0.8f, 3), to), Is.LessThan(1e-4f));
            Assert.That(AttackPath.HopApproach(from, to, 1f / 3f, 0.8f, 3).y, Is.EqualTo(0f).Within(1e-4f), "touches down between hops");
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
