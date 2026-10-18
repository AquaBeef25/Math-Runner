using MathRunner.Level;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests.EditMode
{
    public class ParallaxLayerTests
    {
        private const float Tile = 8f;

        [TestCase(0f, 0f)]
        [TestCase(37.5f, 4.7f)]
        [TestCase(-12f, -3f)]
        [TestCase(260f, 9f)]
        public void LayerStaysWithinHalfATileOfTheCamera(float camX, float camY)
        {
            var p = ParallaxLayer.Position(new Vector2(camX, camY), new Vector2(0.6f, 0.85f), new Vector2(3f, -2f), Tile);
            Assert.That(Mathf.Abs(p.x - camX), Is.LessThanOrEqualTo(Tile * 0.5f + 1e-4f));
        }

        [TestCase(0f)]
        [TestCase(37.5f)]
        [TestCase(260f)]
        public void WrappingOnlyShiftsByWholeTiles(float camX)
        {
            var follow = new Vector2(0.6f, 0.85f);
            var anchor = new Vector2(3f, -2f);
            var p = ParallaxLayer.Position(new Vector2(camX, 0f), follow, anchor, Tile);
            float unwrapped = anchor.x + camX * follow.x;
            float tiles = (p.x - unwrapped) / Tile;
            Assert.That(tiles, Is.EqualTo(Mathf.Round(tiles)).Within(1e-3f));
        }

        [Test]
        public void ScreenFixedLayerMovesExactlyWithTheCamera()
        {
            var p = ParallaxLayer.Position(new Vector2(123.4f, 5.5f), Vector2.one, Vector2.zero, 0f);
            Assert.That(p.x, Is.EqualTo(123.4f).Within(1e-4f));
            Assert.That(p.y, Is.EqualTo(5.5f).Within(1e-4f));
        }

        [Test]
        public void VerticalFollowsAFractionOfTheCamera()
        {
            var p = ParallaxLayer.Position(new Vector2(0f, 10f), new Vector2(0.6f, 0.85f), new Vector2(0f, -2f), Tile);
            Assert.That(p.y, Is.EqualTo(-2f + 8.5f).Within(1e-4f));
        }

        [Test]
        public void EveryLevelHasABackgroundTheme()
        {
            foreach (var level in LevelCatalog.Load().Levels)
            {
                Assert.That(level.Background, Is.Not.Null, $"Level {level.LevelNumber} has no background theme");
                Assert.That(level.Background.Horizon, Is.Not.Null, $"{level.Background.name} has no horizon sprite");
                Assert.That(level.Background.Fill, Is.Not.Null, $"{level.Background.name} has no fill sprite");
            }
        }
    }
}
