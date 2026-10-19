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
    // The chalkboard HUD as built in the scenes: Kenney hearts, chalk texts on a board strip, a round pause button.
    public class HudTests
    {
        private Scene _scene;

        private Transform TopBar(string name)
        {
            _scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + name + ".unity", OpenSceneMode.Additive);
            return _scene.GetRootGameObjects().First(g => g.name == "HUD").transform.Find("TopBar");
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid()) EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void HeartsShowLives([Values("Game", "Sandbox_Level")] string scene)
        {
            var bar = TopBar(scene);
            var hearts = bar.GetComponentInChildren<HeartRow>(true);
            Assert.That(hearts, Is.Not.Null);
            Assert.That(new SerializedObject(bar.GetComponent<HudView>()).FindProperty("_hearts").objectReferenceValue, Is.SameAs(hearts));
            var images = hearts.GetComponentsInChildren<Image>(true);
            Assert.That(images.Length, Is.EqualTo(3));
            hearts.Show(2);
            var theme = UiTheme.Load();
            Assert.That(images.Select(i => i.sprite), Is.EqualTo(new[] { theme.HeartFull, theme.HeartFull, theme.HeartEmpty }));
        }

        [Test]
        public void HudTextsUseChalkFont([Values("Game", "Sandbox_Level")] string scene)
        {
            var bar = TopBar(scene);
            foreach (var name in new[] { "ScoreText", "StreakText", "LevelText", "CoinText", "ScorePopup" })
            {
                var text = bar.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == name);
                if (name == "ScorePopup" && text == null) continue; // only scenes with FeedbackFX have the popup
                Assert.That(text, Is.Not.Null, name);
                Assert.That(text.font, Is.SameAs(UiTheme.Load().ChalkFont), name);
            }
        }

        [Test]
        public void PauseIsARoundKenneyButton([Values("Game", "Sandbox_Level")] string scene)
        {
            var pause = TopBar(scene).Find("PauseButton").GetComponent<Button>();
            Assert.That(((Image)pause.targetGraphic).sprite, Is.SameAs(UiTheme.Load().RoundButton));
        }

        [Test]
        public void HudGraphicsIgnoreTaps([Values("Game", "Sandbox_Level")] string scene)
        {
            var bar = TopBar(scene);
            var buttonGraphics = bar.GetComponentsInChildren<Button>(true).Select(b => b.targetGraphic).ToList();
            foreach (var g in bar.GetComponentsInChildren<Graphic>(true))
                if (!buttonGraphics.Contains(g))
                    Assert.That(g.raycastTarget, Is.False, g.name);
        }
    }
}
