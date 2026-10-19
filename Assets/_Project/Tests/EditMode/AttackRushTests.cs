using MathRunner.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests
{
    // A wrong answer makes the fireball or the bee strike right away instead of at the deadline.
    public class AttackRushTests
    {
        [Test]
        public void RushShortensTheBulletsFlight()
        {
            var target = new GameObject("Target").transform;
            var bullet = new GameObject("Bullet").AddComponent<EnemyBullet>();
            bullet.Launch(target, 3f);
            Assert.That(bullet.RemainingSeconds, Is.EqualTo(3f).Within(1e-4f));
            bullet.Rush(0.4f);
            Assert.That(bullet.RemainingSeconds, Is.EqualTo(0.4f).Within(1e-4f));
            bullet.Rush(1f);
            Assert.That(bullet.RemainingSeconds, Is.EqualTo(0.4f).Within(1e-4f), "never slower than it already was");
            Object.DestroyImmediate(bullet.gameObject);
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void RushShortensTheChargersAttack()
        {
            var target = new GameObject("Target").transform;
            var bee = new GameObject("Bee").AddComponent<ChargingEnemy>();
            bee.Launch(target, 3f);
            bee.Rush(0.4f);
            Assert.That(bee.RemainingSeconds, Is.EqualTo(0.4f).Within(1e-4f));
            Object.DestroyImmediate(bee.gameObject);
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void SettingsHaveAShortRush()
        {
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterSettings>("Assets/_Project/ScriptableObjects/Encounters/EncounterSettings.asset");
            Assert.That(settings.WrongRushSeconds, Is.InRange(0.2f, 0.6f));
        }
    }
}
