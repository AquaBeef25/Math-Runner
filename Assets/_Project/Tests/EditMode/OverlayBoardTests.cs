using System.Linq;
using MathRunner.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MathRunner.Tests
{
    // Pause, Level complete and Game over as chalkboards with Kenney buttons.
    public class OverlayBoardTests
    {
        private static readonly string[] Panels = { "PausePanel", "GameOverPanel", "LevelCompletePanel" };
        private Scene _scene;

        private Transform Hud(string name)
        {
            _scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + name + ".unity", OpenSceneMode.Additive);
            return _scene.GetRootGameObjects().First(g => g.name == "HUD").transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid()) EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void PauseBoardDrawsAboveQuestionBoard([Values("Game", "Sandbox_Level")] string scene)
        {
            var hud = Hud(scene);
            Assert.That(hud.Find("Overlays").GetSiblingIndex(), Is.GreaterThan(hud.Find("QuestionPanel").GetSiblingIndex()));
            Assert.That(hud.Find("Overlays").GetSiblingIndex(), Is.GreaterThan(hud.Find("AnswerButtons").GetSiblingIndex()));
        }

        [Test]
        public void LevelCompleteHasStarRow([Values("Game", "Sandbox_Level")] string scene)
        {
            var hud = Hud(scene);
            var stars = hud.Find("Overlays/LevelCompletePanel/Box/Stars").GetComponent<StarRow>();
            Assert.That(stars.GetComponentsInChildren<Image>(true).Length, Is.EqualTo(3));
            var controller = hud.GetComponentInChildren<OverlayController>(true);
            Assert.That(new SerializedObject(controller).FindProperty("_levelCompleteStars").objectReferenceValue, Is.SameAs(stars));
        }

        [Test]
        public void PanelsAreChalkboardsWithKenneyButtons([Values("Game", "Sandbox_Level")] string scene)
        {
            var hud = Hud(scene);
            var theme = UiTheme.Load();
            var kenney = System.Enum.GetValues(typeof(UiButtonColor)).Cast<UiButtonColor>().Select(theme.Button).ToList();
            foreach (var name in Panels)
            {
                var panel = hud.Find("Overlays/" + name);
                Assert.That(panel.GetComponent<Image>().raycastTarget, Is.True, name + " dim must block taps");
                var box = panel.Find("Box");
                Assert.That(box.GetComponent<Image>().sprite, Is.SameAs(theme.Board), name);
                Assert.That(box.Find("Title").GetComponent<TextMeshProUGUI>().font, Is.SameAs(theme.ChalkFont), name);
                foreach (var button in box.GetComponentsInChildren<Button>(true))
                {
                    Assert.That(kenney, Does.Contain(((Image)button.targetGraphic).sprite), button.name);
                    Assert.That(button.GetComponentInChildren<TextMeshProUGUI>(true).font, Is.SameAs(theme.ButtonFont), button.name);
                }
            }
        }
    }
}
