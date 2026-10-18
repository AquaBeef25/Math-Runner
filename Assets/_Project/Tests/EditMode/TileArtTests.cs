using System.Linq;
using MathRunner.Level;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests
{
    // Art-only checks for the ground pieces: which tile ends up where.
    public class TileArtTests
    {
        private static Sprite Make(string name)
        {
            var tex = new Texture2D(4, 4);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sprite.name = name;
            return sprite;
        }

        private static SpriteRenderer Piece(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.drawMode = SpriteDrawMode.Tiled;
            return sr;
        }

        private static (TiledStrip Strip, SpriteRenderer Left, SpriteRenderer Middle, SpriteRenderer Right) Strip(bool withSingle)
        {
            var root = new GameObject("Strip");
            var left = Piece(root.transform, "Left");
            var middle = Piece(root.transform, "Middle");
            var right = Piece(root.transform, "Right");
            var strip = root.AddComponent<TiledStrip>();
            strip.SetPieces(left, middle, right);
            strip.SetSprites(Make("left"), Make("middle"), Make("right"));
            if (withSingle) strip.SetSingle(Make("single"));
            return (strip, left, middle, right);
        }

        [Test]
        public void OneTileWideStripUsesTheSingleTile()
        {
            var s = Strip(true);
            s.Strip.SetSize(1f, 1f);
            Assert.That(s.Left.enabled, Is.False);
            Assert.That(s.Right.enabled, Is.False);
            Assert.That(s.Middle.enabled, Is.True);
            Assert.That(s.Middle.sprite.name, Is.EqualTo("single"));
            Assert.That(s.Middle.size.x, Is.EqualTo(1f).Within(1e-4f));
            Object.DestroyImmediate(s.Strip.gameObject);
        }

        [Test]
        public void CapsNeverShrinkBelowATileSoTheirOuterEdgesShow()
        {
            var s = Strip(false);
            s.Strip.SetSize(1.5f, 1f);
            Assert.That(s.Left.size.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(s.Right.size.x, Is.EqualTo(1f).Within(1e-4f));
            // Each cap's outer edge sits on the strip's edge (they overlap in the middle).
            Assert.That(s.Left.transform.localPosition.x - 0.5f, Is.EqualTo(-0.75f).Within(1e-4f));
            Assert.That(s.Right.transform.localPosition.x + 0.5f, Is.EqualTo(0.75f).Within(1e-4f));
            Object.DestroyImmediate(s.Strip.gameObject);
        }

        [Test]
        public void WideStripKeepsCapsAndMiddle()
        {
            var s = Strip(true);
            s.Strip.SetSize(4f, 1f);
            Assert.That(s.Left.enabled && s.Right.enabled && s.Middle.enabled, Is.True);
            Assert.That(s.Middle.sprite.name, Is.EqualTo("middle"));
            Assert.That(s.Middle.size.x, Is.EqualTo(2f).Within(1e-4f));
            Object.DestroyImmediate(s.Strip.gameObject);
        }

        // Kenney's long ramp: the low half (b) is only a grass wedge; its grass band runs on into the tile below (c).
        [Test]
        public void RampLowHalfHasItsGrassBandTileBelow([Values(true, false)] bool down)
        {
            var go = new GameObject("Ramp");
            var ramp = go.AddComponent<RampPiece>();
            ramp.SetSprites(Make("high"), Make("low"), Make("under"), Make("fill"), Make("flat"));
            if (down) ramp.SetRamp(0f, 4f, 2f, 0f, -3f);
            else ramp.SetRamp(0f, 4f, 0f, 2f, -3f);

            var tiles = go.GetComponentsInChildren<SpriteRenderer>();
            var lows = tiles.Where(t => t.sprite.name == "low").ToList();
            var unders = tiles.Where(t => t.sprite.name == "under").ToList();
            Assert.That(lows.Count, Is.EqualTo(2));
            Assert.That(unders.Count, Is.EqualTo(2), "every low-half tile needs the band tile under it");
            foreach (var low in lows)
            {
                var under = unders.Single(u => Mathf.Abs(u.transform.localPosition.x - low.transform.localPosition.x) < 1e-4f);
                Assert.That(under.transform.localPosition.y, Is.EqualTo(low.transform.localPosition.y - 1f).Within(1e-4f));
                Assert.That(under.flipX, Is.EqualTo(low.flipX));
            }
            // No plain dirt drawn over the band tile.
            foreach (var fill in tiles.Where(t => t.sprite.name == "fill"))
                foreach (var under in unders.Where(u => Mathf.Abs(u.transform.localPosition.x - fill.transform.localPosition.x) < 1e-4f))
                    Assert.That(fill.transform.localPosition.y + fill.size.y * 0.5f, Is.LessThanOrEqualTo(under.transform.localPosition.y - 0.5f + 1e-4f));
            Object.DestroyImmediate(go);
        }
    }
}
