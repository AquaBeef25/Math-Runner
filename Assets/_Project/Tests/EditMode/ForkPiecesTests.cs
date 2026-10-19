using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Player;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace MathRunner.Tests
{
    // The things standing on a fork's routes: the chest at the end of the right one, the mud on a wrong one, the signs.
    public class ForkPiecesTests
    {
        private static Sprite Make(string name)
        {
            var sprite = Sprite.Create(new Texture2D(4, 4), new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 4f);
            sprite.name = name;
            return sprite;
        }

        private static (PlayerController Controller, PlayerHealth Health) Runner()
        {
            var go = new GameObject("Runner", typeof(Rigidbody2D), typeof(BoxCollider2D));
            return (go.AddComponent<PlayerController>(), go.AddComponent<PlayerHealth>());
        }

        [Test]
        public void ChestPaysOutOnlyOnceAndOnlyWhenArmed()
        {
            var chest = TreasureChest.Create(null, new Vector3(0f, 0f, 0f), Make("closed"), Make("open"), Make("coin"), 5, 10);
            int total = 0;
            void OnCoin(int v) => total += v;
            GameEvents.CoinCollected += OnCoin;
            try
            {
                chest.Touch();
                Assert.That(total, Is.EqualTo(0), "a chest on a wrong route stays shut");
                chest.Arm();
                chest.Touch();
                Assert.That(total, Is.EqualTo(50));
                Assert.That(chest.GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("open"));
                chest.Touch();
                Assert.That(total, Is.EqualTo(50), "only once");
            }
            finally
            {
                GameEvents.CoinCollected -= OnCoin;
                Object.DestroyImmediate(chest.gameObject);
            }
        }

        [Test]
        public void MudOnlySlowsAndOnlyWhenArmed()
        {
            var (controller, health) = Runner();
            var mud = MudPatch.Create(null, 0f, 3f, 0f, Make("mud"), new Color(0.35f, 0.22f, 0.12f));
            int hits = 0;
            void OnHit(HitCause c) { if (c == HitCause.Mud) hits++; }
            GameEvents.PlayerHit += OnHit;
            try
            {
                Assert.That(mud.GetComponent<SpriteRenderer>().enabled, Is.False, "hidden until the route is wrong");
                mud.Enter(controller, health);
                Assert.That(hits, Is.EqualTo(0));
                Assert.That(controller.SpeedFactor, Is.EqualTo(1f));

                mud.Arm();
                Assert.That(mud.GetComponent<SpriteRenderer>().enabled, Is.True);
                mud.Enter(controller, health);
                Assert.That(controller.SpeedFactor, Is.EqualTo(MudPatch.SlowFactor));
                Assert.That(hits, Is.EqualTo(0), "the heart is taken when the wrong route is chosen, not by the mud");
                mud.Exit(controller);
                Assert.That(controller.SpeedFactor, Is.EqualTo(1f));
            }
            finally
            {
                GameEvents.PlayerHit -= OnHit;
                Object.DestroyImmediate(mud.gameObject);
                Object.DestroyImmediate(controller.gameObject);
            }
        }

        // The answers live on the route signs now, so they are big and glow in their route's colour.
        [Test]
        public void SignShowsItsAnswerBigAndGlowing()
        {
            var sign = RouteSign.Create(null, Vector3.zero, Make("ring"), null, Color.cyan);
            sign.Show(42);
            var text = sign.GetComponentInChildren<TextMeshPro>();
            Assert.That(text.text, Is.EqualTo("42"));
            Assert.That(sign.Diameter, Is.GreaterThanOrEqualTo(1.2f), "bigger than the old 1-unit wooden sign");
            Assert.That(sign.Glow, Is.Not.Null);
            Assert.That(sign.Glow.color.r, Is.EqualTo(Color.cyan.r).Within(1e-3f));
            Assert.That(sign.Glow.color.b, Is.EqualTo(Color.cyan.b).Within(1e-3f));
            Assert.That(sign.Glow.transform.lossyScale.x, Is.GreaterThan(sign.Diameter), "the halo reaches past the sign");
            Object.DestroyImmediate(sign.gameObject);
        }
    }
}
