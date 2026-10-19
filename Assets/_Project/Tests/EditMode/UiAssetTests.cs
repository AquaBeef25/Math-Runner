using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MathRunner.Tests
{
    // The chalkboard UI's imported art: fonts, the generated chalk sprites and the 9-slice borders on the Kenney buttons.
    public class UiAssetTests
    {
        private const string Fonts = "Assets/_Project/Art/UI/Fonts/";
        private const string Ui = "Assets/_Project/Art/UI/";
        private const string Pack = "Assets/_Project/Art/ThirdParty/Kenney/UIPack/PNG/";
        private static readonly string[] Colors = { "Blue", "Green", "Yellow", "Red", "Grey" };

        private static TMP_FontAsset Font(string name) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fonts + name + ".asset");

        [Test]
        public void ChalkFontsExist([Values("KenneyFuture SDF", "PatrickHand SDF")] string name)
        {
            var font = Font(name);
            Assert.That(font, Is.Not.Null, name);
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
        }

        [Test]
        public void ChalkboardIsNineSliced()
        {
            var board = AssetDatabase.LoadAssetAtPath<Sprite>(Ui + "chalkboard.png");
            Assert.That(board, Is.Not.Null);
            for (int i = 0; i < 4; i++) Assert.That(board.border[i], Is.GreaterThan(0f), "border " + i);
        }

        [Test]
        public void ChalkSpritesExist([Values("chalk_ring", "chalk_line", "chalk_tick", "chalk_cross")] string name)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(Ui + name + ".png"), Is.Not.Null, name);
        }

        [Test]
        public void KenneyButtonsAreNineSliced([Values("button_rectangle_depth_gloss", "button_square_depth_flat")] string shape)
        {
            foreach (var color in Colors)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Pack + color + "/Default/" + shape + ".png");
                Assert.That(sprite, Is.Not.Null, color + " " + shape);
                Assert.That(sprite.border, Is.EqualTo(new Vector4(16f, 20f, 16f, 14f)), color + " " + shape);
            }
        }

        [Test]
        public void ChalkGlyphsIncludeMathSymbols()
        {
            var font = Font("PatrickHand SDF");
            Assert.That(font, Is.Not.Null);
            Assert.That(font.HasCharacters("0123456789+-×÷=?!", out var missing, false, true), Is.True,
                "missing: " + (missing == null ? "" : string.Join(",", missing)));
        }
    }
}
