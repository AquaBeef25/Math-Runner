using System;
using MathRunner.Level;
using MathRunner.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MathRunner.Tests
{
    public class UiThemeTests
    {
        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        private static void Same(Color a, Color b, string what)
        {
            Assert.That(a.r, Is.EqualTo(b.r).Within(1f / 255f), what);
            Assert.That(a.g, Is.EqualTo(b.g).Within(1f / 255f), what);
            Assert.That(a.b, Is.EqualTo(b.b).Within(1f / 255f), what);
        }

        [Test]
        public void AnswerColorsFollowKeyOrder()
        {
            var theme = UiTheme.Load();
            Assert.That(theme, Is.Not.Null);
            string[] expected = { "#8FD0FF", "#A6EC9B", "#FFE08A", "#FF9D9D" };
            for (int i = 0; i < 4; i++) Same(theme.AnswerColor(i), Hex(expected[i]), "answer " + i);
            Same(theme.AnswerColor(9), Hex(expected[3]), "clamped");
        }

        [Test]
        public void ThemeAssetIsComplete()
        {
            var t = UiTheme.Load();
            Assert.That(t, Is.Not.Null);
            Same(t.ChalkWhite, Hex("#F6F3E8"), "chalk white");
            Assert.That(t.ButtonFont, Is.Not.Null, "ButtonFont");
            Assert.That(t.ChalkFont, Is.Not.Null, "ChalkFont");
            var sprites = new[] { t.Board, t.Ring, t.Line, t.Tick, t.Cross, t.HeartFull, t.HeartEmpty, t.Coin, t.Star, t.StarEmpty, t.RoundButton, t.PlayIcon, t.RepeatIcon };
            for (int i = 0; i < sprites.Length; i++) Assert.That(sprites[i], Is.Not.Null, "sprite " + i);
            foreach (UiButtonColor c in Enum.GetValues(typeof(UiButtonColor)))
            {
                Assert.That(t.Button(c), Is.Not.Null, "button " + c);
                Assert.That(t.SquareButton(c), Is.Not.Null, "square " + c);
            }
        }

        [Test]
        public void OnlySandIsDesert()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<TerrainSet>("Assets/_Project/ScriptableObjects/Terrain/Grass.asset").IsDesert, Is.False);
            Assert.That(AssetDatabase.LoadAssetAtPath<TerrainSet>("Assets/_Project/ScriptableObjects/Terrain/Sand.asset").IsDesert, Is.True);
        }
    }
}
