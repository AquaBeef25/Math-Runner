using System.Linq;
using MathRunner.Core;
using MathRunner.Math;
using MathRunner.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MathRunner.Tests
{
    // The chalkboard question board as built in the scenes: question, timer line and the four answer circles on one board.
    public class QuestionBoardTests
    {
        private Scene _scene;

        private void Open(string name) =>
            _scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + name + ".unity", OpenSceneMode.Additive);

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid()) EditorSceneManager.CloseScene(_scene, true);
        }

        private Transform Hud() => _scene.GetRootGameObjects().First(g => g.name == "HUD").transform;
        private Transform Board() => Hud().Find("QuestionPanel/Panel");
        private AnswerButtons Answers() => Hud().GetComponentInChildren<AnswerButtons>(true);
        private Button[] AnswerButtonList() =>
            Enumerable.Range(1, 4).Select(i => Board().Find("Answers/Answer" + i).GetComponent<Button>()).ToArray();

        [Test]
        public void RingAlphaFadesTheOthers()
        {
            Assert.That(AnswerButtons.RingAlpha(0, 2), Is.EqualTo(0.4f));
            Assert.That(AnswerButtons.RingAlpha(2, 2), Is.EqualTo(1f));
            Assert.That(AnswerButtons.RingAlpha(1, -1), Is.EqualTo(1f));
        }

        [Test]
        public void QuestionBoardTextAutoSizes([Values("Game", "Sandbox_Level", "Sandbox_QuestionZone")] string scene)
        {
            Open(scene);
            var question = Board().Find("QuestionText").GetComponent<TextMeshProUGUI>();
            var labels = AnswerButtonList().Select(b => b.transform.Find("Label").GetComponent<TextMeshProUGUI>()).Append(question);
            foreach (var label in labels)
            {
                Assert.That(label.enableAutoSizing, Is.True, label.name);
                Assert.That(label.fontSizeMin, Is.LessThanOrEqualTo(18f), label.name);
                Assert.That(label.font, Is.SameAs(UiTheme.Load().ChalkFont), label.name);
            }
        }

        [Test]
        public void AnswerCirclesSitInsideTheBoard([Values("Game", "Sandbox_Level", "Sandbox_QuestionZone")] string scene)
        {
            Open(scene);
            var board = Board();
            Assert.That(board.GetComponent<Image>().sprite, Is.SameAs(UiTheme.Load().Board));
            foreach (var button in AnswerButtonList())
            {
                Assert.That(button.transform.IsChildOf(board), Is.True, button.name);
                Assert.That(button.targetGraphic.GetComponent<Image>().sprite, Is.SameAs(UiTheme.Load().Ring), button.name);
                Assert.That(Answers().Owns(button), Is.True, button.name);
            }
        }

        [Test]
        public void UnusedCirclesHide()
        {
            Open("Game");
            var question = new MathQuestion(new MathProblem(QuestionType.Add, 5, 2, 3), new[] { 4, 5, 6 }, 1);
            var answers = Answers();
            answers.Show(question);
            var buttons = AnswerButtonList();
            Assert.That(buttons[0].gameObject.activeSelf && buttons[1].gameObject.activeSelf && buttons[2].gameObject.activeSelf, Is.True);
            Assert.That(buttons[3].gameObject.activeSelf, Is.False);
            answers.Lock(1);
            answers.Resolve(new QuestionResult(question, AnswerOutcome.Correct, 1, 0f, 0f));
            var marks = buttons.Select(b => b.transform.Find("Mark").gameObject).ToArray();
            Assert.That(marks.Count(m => m.activeSelf), Is.EqualTo(1), "only the tick on the right answer");
            Assert.That(marks[1].activeSelf, Is.True);
            Assert.That(marks[1].GetComponent<Image>().sprite, Is.SameAs(UiTheme.Load().Tick));
        }

        [Test]
        public void NonButtonGraphicsIgnoreTaps([Values("Game", "Sandbox_Level", "Sandbox_QuestionZone")] string scene)
        {
            Open(scene);
            var buttonGraphics = Board().GetComponentsInChildren<Button>(true).Select(b => b.targetGraphic).ToList();
            foreach (var g in Board().GetComponentsInChildren<Graphic>(true))
                if (!buttonGraphics.Contains(g))
                    Assert.That(g.raycastTarget, Is.False, g.name);
        }
    }
}
