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

        // A side is outlined only where nothing stands next to it: above a lower neighbour's top, never against a higher
        // or equal one, all the way down over a gap.
        [Test]
        public void EdgeOutlineStartsAboveTheNeighbour()
        {
            Assert.That(GroundSegment.EdgeFrom(2f, 1f), Is.EqualTo(1f));
            Assert.That(float.IsPositiveInfinity(GroundSegment.EdgeFrom(1f, 2f)), Is.True, "lower side against a wall: no outline");
            Assert.That(float.IsPositiveInfinity(GroundSegment.EdgeFrom(1f, 1f)), Is.True);
            Assert.That(float.IsNegativeInfinity(GroundSegment.EdgeFrom(1f, null)), Is.True, "over a gap: outlined all the way down");
        }

        private static (GroundSegment Ground, TiledStrip Top) Ground()
        {
            var go = new GameObject("Ground");
            go.AddComponent<BoxCollider2D>();
            var top = Strip(true);
            var fill = Strip(true);
            top.Strip.transform.SetParent(go.transform, false);
            fill.Strip.transform.SetParent(go.transform, false);
            var ground = go.AddComponent<GroundSegment>();
            ground.SetStrips(top.Strip, fill.Strip);
            return (ground, top.Strip);
        }

        private static bool Capped(SpriteRenderer piece) => piece.enabled && (piece.sprite.name == "left" || piece.sprite.name == "right" || piece.sprite.name == "single");

        [Test]
        public void TallStepOutlinesOnlyTheExposedPartOfItsSide()
        {
            var g = Ground();
            // Top 2 next to a neighbour at 0 on the left, a wall at 3 on the right.
            g.Ground.SetRect(0f, -4f, 3f, 2f, GroundSegment.EdgeFrom(2f, 0f), GroundSegment.EdgeFrom(2f, 3f));
            var bands = g.Ground.FillBands;
            Assert.That(bands.Count, Is.EqualTo(2));
            var low = bands.OrderBy(b => b.transform.localPosition.y).First();
            var high = bands.OrderBy(b => b.transform.localPosition.y).Last();
            var lowPieces = low.GetComponentsInChildren<SpriteRenderer>();
            var highPieces = high.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(Capped(lowPieces.Single(p => p.name == "Left")), Is.False, "below the neighbour's top the dirt runs on");
            Assert.That(Capped(highPieces.Single(p => p.name == "Left")), Is.True, "above it the side is outlined");
            Assert.That(Capped(highPieces.Single(p => p.name == "Right")), Is.False, "against the taller wall: no outline");
            var topPieces = g.Top.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(Capped(topPieces.Single(p => p.name == "Left")), Is.True);
            Assert.That(Capped(topPieces.Single(p => p.name == "Right")), Is.False);
            Object.DestroyImmediate(g.Ground.gameObject);
        }

        [Test]
        public void OneTilePillarDirtHasNoSideOutlinesBelowTheGround()
        {
            var g = Ground();
            g.Ground.SetRect(0f, -4f, 1f, 1f, GroundSegment.EdgeFrom(1f, 0f), GroundSegment.EdgeFrom(1f, 0f));
            Assert.That(g.Ground.FillBands.Count, Is.EqualTo(1));
            var fill = g.Ground.FillBands[0].GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(fill.Any(p => p.enabled && p.sprite.name == "single"), Is.False, "the outlined single column is only for the exposed part");
            Assert.That(fill.Where(p => p.enabled).All(p => p.sprite.name == "middle"), Is.True);
            var top = g.Top.GetComponentsInChildren<SpriteRenderer>(true).Where(p => p.enabled).ToList();
            Assert.That(top.Count, Is.EqualTo(1));
            Assert.That(top[0].sprite.name, Is.EqualTo("single"), "the grass top keeps both rounded corners");
            Object.DestroyImmediate(g.Ground.gameObject);
        }

        [Test]
        public void GroundTakesItsLevelsTerrainTiles()
        {
            var g = Ground();
            var sand = ScriptableObject.CreateInstance<TerrainSet>();
            sand.SetGround(Make("sand_top_left"), Make("sand_top"), Make("sand_top_right"), Make("sand_top_single"),
                Make("sand_left"), Make("sand_center"), Make("sand_right"), Make("sand_single"));
            g.Ground.SetTerrain(sand);
            g.Ground.SetRect(0f, -4f, 1f, 1f, GroundSegment.EdgeFrom(1f, null), GroundSegment.EdgeFrom(1f, null));
            var topPiece = g.Top.GetComponentsInChildren<SpriteRenderer>().Single(p => p.enabled);
            Assert.That(topPiece.sprite.name, Is.EqualTo("sand_top_single"));
            g.Ground.SetRect(0f, -4f, 5f, 1f, GroundSegment.EdgeFrom(1f, null), GroundSegment.EdgeFrom(1f, null));
            var names = g.Top.GetComponentsInChildren<SpriteRenderer>().Where(p => p.enabled).Select(p => p.sprite.name).ToList();
            Assert.That(names, Is.EquivalentTo(new[] { "sand_top_left", "sand_top", "sand_top_right" }));
            var fill = g.Ground.FillBands[0].GetComponentsInChildren<SpriteRenderer>().Where(p => p.enabled).Select(p => p.sprite.name);
            Assert.That(fill, Is.EquivalentTo(new[] { "sand_left", "sand_center", "sand_right" }));
            Object.DestroyImmediate(g.Ground.gameObject);
            Object.DestroyImmediate(sand);
        }

        // The ramp's side under the flat ground it joins must not be a vertical face reaching the surface: the runner's box
        // snagged on that hidden corner and stopped dead (Level 4, a down-ramp after flat ground).
        [Test]
        public void RampHasNoVerticalFaceAtItsStart([Values(true, false)] bool down)
        {
            var go = new GameObject("Ramp");
            var ramp = go.AddComponent<RampPiece>();
            if (down) ramp.SetRamp(479.6f, 481.6f, 1f, 0f, -3f);
            else ramp.SetRamp(479.6f, 481.6f, 0f, 1f, -3f);
            var path = go.GetComponent<PolygonCollider2D>().GetPath(0);
            float top = down ? 1f : 0f;
            for (int i = 0; i < path.Length; i++)
            {
                var a = path[i];
                var b = path[(i + 1) % path.Length];
                bool vertical = Mathf.Abs(a.x - b.x) < 1e-4f;
                bool reachesSurfaceAtStart = Mathf.Abs(a.x) < 1e-4f && (Mathf.Abs(a.y - top) < 1e-4f || Mathf.Abs(b.y - top) < 1e-4f);
                Assert.That(vertical && reachesSurfaceAtStart, Is.False, $"vertical face {a}->{b} ends at the surface where the ramp starts");
            }
            Object.DestroyImmediate(go);
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
