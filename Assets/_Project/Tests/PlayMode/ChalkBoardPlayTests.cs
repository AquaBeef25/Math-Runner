using System;
using System.Collections;
using System.Linq;
using MathRunner.Core;
using MathRunner.Math;
using MathRunner.Save;
using MathRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MathRunner.Tests.PlayMode
{
    // The chalkboard question board reacting to the real question events in a running scene.
    public class ChalkBoardPlayTests
    {
        private string _savePath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mathrunner-chalk-" + Guid.NewGuid() + ".json");
            SaveSystem.UseFile(_savePath);
            SceneManager.LoadScene("Sandbox_Level");
            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (System.IO.File.Exists(_savePath)) System.IO.File.Delete(_savePath);
            SaveSystem.UseFile(null);
        }

        private static Transform Board() => GameObject.Find("HUD").transform.Find("QuestionPanel/Panel");
        private static Transform Answer(int i) => Board().Find("Answers/Answer" + (i + 1));

        private static MathQuestion Question() =>
            new MathQuestion(new MathProblem(QuestionType.Add, 7, 3, 4), new[] { 6, 7, 8, 9 }, 1);

        [UnityTest]
        public IEnumerator UntimedQuestionHidesTimerLine()
        {
            GameEvents.RaiseQuestionShown(Question(), 0f);
            yield return null;
            Assert.That(Board().gameObject.activeInHierarchy, Is.True);
            Assert.That(Board().Find("TimerBar").gameObject.activeSelf, Is.False);
            for (int i = 0; i < 4; i++) Assert.That(Answer(i).gameObject.activeInHierarchy, Is.True, "answer " + i);
        }

        [UnityTest]
        public IEnumerator WrongPickShowsTickAndCross()
        {
            var q = Question();
            GameEvents.RaiseQuestionShown(q, 4f);
            yield return null;
            Assert.That(Board().Find("TimerBar").gameObject.activeSelf, Is.True);
            GameEvents.RaiseAnswerLocked(3);
            GameEvents.RaiseQuestionResolved(new QuestionResult(q, AnswerOutcome.Wrong, 3, 4f, 0.5f));
            yield return null;
            var theme = UiTheme.Load();
            var marks = Enumerable.Range(0, 4).Select(i => Answer(i).Find("Mark")).ToArray();
            Assert.That(marks[1].gameObject.activeSelf, Is.True);
            Assert.That(marks[1].GetComponent<Image>().sprite, Is.SameAs(theme.Tick));
            Assert.That(marks[3].gameObject.activeSelf, Is.True);
            Assert.That(marks[3].GetComponent<Image>().sprite, Is.SameAs(theme.Cross));
            Assert.That(marks[0].gameObject.activeSelf || marks[2].gameObject.activeSelf, Is.False);
        }
    }
}
