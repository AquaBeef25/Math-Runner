using MathRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.Tests
{
    public class ChalkUiLogicTests
    {
        private static readonly AnswerMark N = AnswerMark.None, T = AnswerMark.Tick, X = AnswerMark.Cross;

        [Test]
        public void CorrectPickGetsOnlyATick() =>
            Assert.That(AnswerMarks.For(4, 2, 2), Is.EqualTo(new[] { N, N, T, N }));

        [Test]
        public void WrongPickGetsACrossAndTheAnswerATick() =>
            Assert.That(AnswerMarks.For(4, 1, 3), Is.EqualTo(new[] { N, T, N, X }));

        [Test]
        public void MissedShowsOnlyTheAnswer() =>
            Assert.That(AnswerMarks.For(4, 0, -1), Is.EqualTo(new[] { T, N, N, N }));

        [Test]
        public void MarksIgnoreMissingChoices() =>
            Assert.That(AnswerMarks.For(3, 1, 3), Is.EqualTo(new[] { N, T, N, N }));

        [Test]
        public void HeartStatesFillFromTheLeft()
        {
            Assert.That(HeartRow.States(2, 3), Is.EqualTo(new[] { true, true, false }));
            Assert.That(HeartRow.States(-1, 3), Is.EqualTo(new[] { false, false, false }));
            Assert.That(HeartRow.States(5, 3), Is.EqualTo(new[] { true, true, true }));
        }

        [Test]
        public void StarRowShowsSprites()
        {
            var root = new GameObject("Stars");
            var images = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Star" + i, typeof(RectTransform));
                go.transform.SetParent(root.transform, false);
                images[i] = go.AddComponent<Image>();
            }
            var full = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            var empty = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
            var row = root.AddComponent<StarRow>();
            row.SetImages(images, full, empty);
            row.Show(2);
            Assert.That(images[0].sprite, Is.SameAs(full));
            Assert.That(images[1].sprite, Is.SameAs(full));
            Assert.That(images[2].sprite, Is.SameAs(empty));
            Assert.That(row.Stars, Is.EqualTo(2));
            Object.DestroyImmediate(root);
        }
    }
}
