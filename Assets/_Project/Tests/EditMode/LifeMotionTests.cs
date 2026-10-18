using System.Linq;
using MathRunner.Level;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests
{
    // The harmless moving life: its motion maths, its sprite sets, and that a built piece can never touch the runner.
    public class LifeMotionTests
    {
        [Test]
        public void FlyerStaysInsideItsLoop()
        {
            for (float t = 0f; t < 10f; t += 0.01f)
            {
                var o = LifeMotion.FlyerOffset(t, 0.8f, 0.25f);
                Assert.That(Mathf.Abs(o.x), Is.LessThanOrEqualTo(0.8f + 1e-4f));
                Assert.That(Mathf.Abs(o.y), Is.LessThanOrEqualTo(0.4f + 1e-4f));
            }
        }

        [Test]
        public void HopReturnsToTheGround()
        {
            float max = 0f;
            for (float t = 0f; t < 9f; t += 0.01f)
            {
                float h = LifeMotion.HopHeight(t, 3f, 0.8f);
                Assert.That(h, Is.GreaterThanOrEqualTo(0f));
                max = Mathf.Max(max, h);
            }
            Assert.That(max, Is.EqualTo(0.8f).Within(0.05f));
            for (int n = 0; n < 3; n++) Assert.That(LifeMotion.HopHeight(n * 3f, 3f, 0.8f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(LifeMotion.HopHeight(1f, 3f, 0.8f), Is.EqualTo(0f), "sits still most of the time");
        }

        [Test]
        public void SwingStaysWithinItsAngle()
        {
            float max = 0f;
            for (float t = 0f; t < 10f; t += 0.01f)
            {
                float a = LifeMotion.SwingAngle(t, 12f, 0.3f);
                Assert.That(Mathf.Abs(a), Is.LessThanOrEqualTo(12f + 1e-4f));
                max = Mathf.Max(max, Mathf.Abs(a));
            }
            Assert.That(max, Is.GreaterThan(11f), "it actually swings");
        }

        [Test]
        public void FishIsHiddenMostOfTheTimeAndArcsWhenVisible()
        {
            int visible = 0, samples = 0;
            for (float t = 0f; t < 14f; t += 0.01f, samples++)
            {
                var (offset, shown) = LifeMotion.FishLeap(t, 3.5f, 2f, 1.8f);
                if (!shown) continue;
                visible++;
                Assert.That(offset.y, Is.InRange(-1e-4f, 1.8f + 1e-4f));
                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(1f + 1e-4f));
            }
            Assert.That(visible, Is.GreaterThan(0));
            Assert.That(visible / (float)samples, Is.LessThan(0.5f));
        }

        private static Sprite Make(string name)
        {
            var sprite = Sprite.Create(new Texture2D(4, 4), new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 4f);
            sprite.name = name;
            return sprite;
        }

        private static LifeSet TestSet()
        {
            var set = ScriptableObject.CreateInstance<LifeSet>();
            set.SetKind(LifeKind.Walker, 1f, new LifeSet.Variant(Make("snail_a"), Make("snail_b")), new LifeSet.Variant(Make("mouse_a"), Make("mouse_b")));
            set.SetKind(LifeKind.Hopper, 1f, new LifeSet.Variant(Make("frog_idle"), Make("frog_jump")));
            set.SetKind(LifeKind.Flyer, 1f, new LifeSet.Variant(Make("fly_a"), Make("fly_b")));
            set.SetKind(LifeKind.Flag, 1f, new LifeSet.Variant(Make("flag_a"), Make("flag_b")));
            set.SetKind(LifeKind.Torch, 1f, new LifeSet.Variant(Make("torch_a"), Make("torch_b")));
            set.SetKind(LifeKind.Swing, 1f, new LifeSet.Variant(Make("chain"), Make("crate")));
            set.SetFish(1f, new LifeSet.Variant(Make("fish_a"), Make("fish_b")));
            set.SetEmotes(Make("emote_heart"), Make("emote_exclamation"));
            return set;
        }

        [Test]
        public void LifeSetVariantsMatchItsSprites()
        {
            var set = TestSet();
            set.SetKind(LifeKind.Torch, 1f); // weight but no sprites: can't appear
            Assert.That(set.Variants[(int)LifeKind.Walker], Is.EqualTo(2));
            Assert.That(set.Variants[(int)LifeKind.Flyer], Is.EqualTo(1));
            Assert.That(set.Weights[(int)LifeKind.Torch], Is.EqualTo(0f));
            Assert.That(set.Weights[(int)LifeKind.Walker], Is.EqualTo(1f));
            Assert.That(set.Frames(LifeKind.Walker, 1)[0].name, Is.EqualTo("mouse_a"));
            Object.DestroyImmediate(set);
        }

        [Test]
        public void BuiltLifeHasNoColliders()
        {
            var set = TestSet();
            var parent = new GameObject("LifeRoot").transform;
            var player = new GameObject("Player").transform;
            foreach (LifeKind kind in System.Enum.GetValues(typeof(LifeKind)))
                LifeFactory.Build(kind, 0, 5f, 1f, set, player, parent);
            LifeFactory.BuildFish(10f, 13f, -0.6f, set, parent);

            Assert.That(parent.childCount, Is.EqualTo(7));
            Assert.That(parent.GetComponentsInChildren<Collider2D>(true), Is.Empty, "life must never touch the runner");
            Assert.That(parent.GetComponentsInChildren<Obstacle>(true), Is.Empty);
            Assert.That(parent.GetComponentsInChildren<Transform>(true).All(t => t.gameObject.layer == 0), Is.True, "life stays on the Default layer");
            Assert.That(parent.GetComponentsInChildren<SpriteRenderer>(true).Any(r => r.sprite != null && r.sprite.name == "crate"), Is.True);
            Object.DestroyImmediate(parent.gameObject);
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(set);
        }
    }
}
