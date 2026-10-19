using System.Linq;
using MathRunner.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace MathRunner.Tests
{
    // The Android build: only the playable scenes, landscape only, a proper name, id and icon.
    public class MobileBuildTests
    {
        [Test]
        public void AndroidBuildShipsOnlyThePlayableScenes()
        {
            Assert.That(AndroidBuild.Scenes, Is.EqualTo(new[]
            {
                "Assets/_Project/Scenes/MainMenu.unity",
                "Assets/_Project/Scenes/LevelSelect.unity",
                "Assets/_Project/Scenes/Game.unity"
            }));
            // GameManager.Restart reloads by build index, so they must keep the same indices as in Build Settings.
            var listed = EditorBuildSettings.scenes.Select(s => s.path).Take(3);
            Assert.That(listed, Is.EqualTo(AndroidBuild.Scenes));
        }

        [Test]
        public void GameIsLandscapeOnly()
        {
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft && PlayerSettings.allowedAutorotateToLandscapeRight, Is.True);
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.AutoRotation));
        }

        [Test]
        public void AppHasANameIdAndIcon()
        {
            Assert.That(PlayerSettings.productName, Is.EqualTo("Math Runner"));
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), Is.EqualTo("com.marvxs.mathrunner"));
            var icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            Assert.That(icons.Length > 0 && icons[0] != null, Is.True, "default icon");
        }

        [Test]
        public void HudStaysClearOfNotches()
        {
            Assert.That(PlayerSettings.Android.renderOutsideSafeArea, Is.False);
        }
    }
}
