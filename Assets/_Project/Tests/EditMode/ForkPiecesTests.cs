using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Player;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace MathRunner.Tests
{
    // The things standing on a fork's routes: the chest at the end of the right one, the spikes on a wrong one, the signs.
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

        // Spikes on a wrong route are the look of the mistake: hidden until armed, then they pop up out of the ground.
        // They have no collider: the heart was already taken when the route was chosen, so they can't take a second.
        [Test]
        public void SpikesHideUntilArmedAndNeverHit()
        {
            var spikes = SpikeTrap.Create(null, 0f, 3f, 1f, Make("spikes"));
            try
            {
                var sr = spikes.GetComponent<SpriteRenderer>();
                Assert.That(sr.enabled, Is.False, "hidden until the route is wrong");
                Assert.That(spikes.IsArmed, Is.False);
                Assert.That(spikes.GetComponentsInChildren<Collider2D>(true), Is.Empty, "looks only");
                Assert.That(sr.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(sr.size.x, Is.EqualTo(3f).Within(1e-4f), "spans the trap");

                spikes.Arm();
                Assert.That(sr.enabled, Is.True);
                Assert.That(spikes.IsArmed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(spikes.gameObject);
            }
        }

        // They rise from below the surface to standing on it, and stay there.
        [Test]
        public void SpikesPopUpOutOfTheGround()
        {
            Assert.That(SpikeTrap.RiseOffset(0f), Is.EqualTo(-SpikeTrap.SinkDepth).Within(1e-4f), "start buried");
            Assert.That(SpikeTrap.RiseOffset(SpikeTrap.RiseSeconds * 0.5f), Is.GreaterThan(-SpikeTrap.SinkDepth).And.LessThan(0.1f));
            Assert.That(SpikeTrap.RiseOffset(SpikeTrap.RiseSeconds), Is.EqualTo(0f).Within(1e-4f), "standing on the surface");
            Assert.That(SpikeTrap.RiseOffset(5f), Is.EqualTo(0f).Within(1e-4f));
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
