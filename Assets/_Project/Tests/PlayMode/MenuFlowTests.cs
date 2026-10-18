using System.Collections;
using System.Linq;
using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Save;
using MathRunner.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MathRunner.Tests.PlayMode
{
    public class MenuFlowTests
    {
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mathrunner-menu-" + System.Guid.NewGuid() + ".json");
            SaveSystem.UseFile(_savePath);
            Time.captureDeltaTime = 0.02f;
        }

        [TearDown]
        public void TearDown()
        {
            LevelSelection.Current = null;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            if (System.IO.File.Exists(_savePath)) System.IO.File.Delete(_savePath);
            SaveSystem.UseFile(null);
        }

        [UnityTest]
        public IEnumerator PlayButtonOpensLevelSelectWithOnlyLevelOneUnlocked()
        {
            yield return Load(SceneFlow.MainMenu);
            GameObject.Find("PlayButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneFlow.LevelSelect));
            var buttons = LevelButtons();
            Assert.That(buttons.Length, Is.EqualTo(5));
            Assert.That(buttons[0].interactable, Is.True);
            Assert.That(buttons.Skip(1).All(b => !b.interactable), Is.True, "Levels 2-5 should be locked on a fresh save");
            Assert.That(Detail(buttons[1]), Is.EqualTo("Locked"));
        }

        [UnityTest]
        public IEnumerator CompletedLevelShowsItsStarsAndUnlocksTheNext()
        {
            SaveSystem.Data.RecordCompletion(1, 900, 2);
            yield return Load(SceneFlow.LevelSelect);

            var buttons = LevelButtons();
            Assert.That(Detail(buttons[0]), Does.Contain("2 of 3 stars").And.Contain("900"));
            Assert.That(buttons[1].interactable, Is.True);
            Assert.That(Detail(buttons[1]), Is.EqualTo("New!"));
            Assert.That(buttons[2].interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator ClickingALevelLoadsTheGameWithThatLevel()
        {
            SaveSystem.Data.RecordCompletion(1, 100, 1);
            yield return Load(SceneFlow.LevelSelect);
            LevelButtons()[1].onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneFlow.Game));
            Assert.That(Object.FindFirstObjectByType<LevelBuilder>().Level.LevelNumber, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator NextLevelButtonAfterFinishingLevelOneStartsLevelTwo()
        {
            LevelSelection.Current = LevelCatalog.Load().Levels[0];
            yield return Load(SceneFlow.Game);
            var game = Object.FindFirstObjectByType<GameManager>();
            using (var bot = new LevelBot())
            {
                int frames = 0;
                while (game.State == GameState.Playing && frames++ < 10000)
                {
                    bot.BeforeFrame();
                    yield return null;
                    bot.AfterFrame();
                }
            }
            Assert.That(game.State, Is.EqualTo(GameState.LevelComplete));
            var result = GameObject.Find("HUD").transform.Find("Overlays/LevelCompletePanel/Box/Result").GetComponent<TextMeshProUGUI>();
            Assert.That(result.text, Does.Contain("3 of 3 stars").And.Contain("New best score!"));

            var next = GameObject.Find("HUD").transform.Find("Overlays/LevelCompletePanel/Box/NextLevelButton").GetComponent<Button>();
            Assert.That(next.gameObject.activeInHierarchy, Is.True);
            next.onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(Object.FindFirstObjectByType<LevelBuilder>().Level.LevelNumber, Is.EqualTo(2));
            Assert.That(Time.timeScale, Is.EqualTo(1f), "The next level must not start frozen");
        }

        private static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
        }

        private static Button[] LevelButtons() =>
            Object.FindObjectsByType<LevelButton>(FindObjectsSortMode.None)
                .Where(b => b.name.StartsWith("Level "))
                .OrderBy(b => int.Parse(b.name.Substring(6)))
                .Select(b => b.GetComponent<Button>())
                .ToArray();

        private static string Detail(Button b) => b.transform.Find("Detail").GetComponent<TextMeshProUGUI>().text;
    }
}
