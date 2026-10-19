using System.Linq;
using MathRunner.Audio;
using MathRunner.Level;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathRunner.Tests
{
    // Background music: one looping tune per world, the meadow tune on the menus, soft fades between them.
    public class MusicTests
    {
        private const string Meadow = "HappyClappyLoop";
        private const string Desert = "desert_loop";
        private Scene _scene;

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid()) EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void EveryLevelPlaysItsWorldsTune()
        {
            foreach (var level in LevelCatalog.Load().Levels)
            {
                Assert.That(level.Music, Is.Not.Null, "level " + level.LevelNumber);
                bool desert = level.Terrain != null && level.Terrain.IsDesert;
                Assert.That(level.Music.name, Is.EqualTo(desert ? Desert : Meadow), "level " + level.LevelNumber);
            }
        }

        [Test]
        public void MenusPlayTheMeadowTune([Values("MainMenu", "LevelSelect")] string name)
        {
            _scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + name + ".unity", OpenSceneMode.Additive);
            var cue = _scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MusicCue>(true)).SingleOrDefault();
            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.Clip, Is.Not.Null);
            Assert.That(cue.Clip.name, Is.EqualTo(Meadow));
        }

        [Test]
        public void FadeMovesTowardTheTargetAndStops()
        {
            Assert.That(MusicPlayer.Fade(0f, 1f, 0.25f, 0.5f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(MusicPlayer.Fade(0.9f, 1f, 0.25f, 0.5f), Is.EqualTo(1f));
            Assert.That(MusicPlayer.Fade(1f, 0f, 1f, 0.5f), Is.EqualTo(0f));
            Assert.That(MusicPlayer.Fade(0.3f, 0.3f, 0.1f, 0.5f), Is.EqualTo(0.3f));
            Assert.That(MusicPlayer.Fade(0.2f, 1f, 0.1f, 0f), Is.EqualTo(1f), "no fade time = jump straight there");
        }

        [Test]
        public void MusicLoopsAndStreamsOnPhones([Values(Meadow, Desert)] string clip)
        {
            var path = UnityEditor.AssetDatabase.FindAssets(clip + " t:AudioClip", new[] { "Assets/_Project/Audio/Music" })
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Single();
            var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
            var settings = importer.GetOverrideSampleSettings("Android");
            Assert.That(settings.loadType, Is.EqualTo(AudioClipLoadType.Streaming));
            Assert.That(settings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis));
        }
    }
}
