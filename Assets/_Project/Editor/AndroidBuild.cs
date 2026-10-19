using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MathRunner.EditorTools
{
    // One-click Android builds (menu Math Runner > Build Android ...). Only the playable scenes ship; the sandboxes stay
    // in Build Settings for the tests. Output goes to Builds/Android/ (git-ignored). See docs/MOBILE_DEPLOY.md.
    public static class AndroidBuild
    {
        // Same order as Build Settings: GameManager.Restart reloads by build index.
        public static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/LevelSelect.unity",
            "Assets/_Project/Scenes/Game.unity"
        };

        private const string OutputFolder = "Builds/Android";

        [MenuItem("Math Runner/Build Android APK (test on a phone)")]
        public static void BuildApk() => Build(false);

        [MenuItem("Math Runner/Build Android App Bundle (Google Play)")]
        public static void BuildBundle() => Build(true);

        private static void Build(bool appBundle)
        {
            EditorUserBuildSettings.buildAppBundle = appBundle;
            Directory.CreateDirectory(OutputFolder);
            string file = Path.Combine(OutputFolder, "MathRunner-" + PlayerSettings.bundleVersion + (appBundle ? ".aab" : ".apk"));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = file,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Android build ready: {Path.GetFullPath(file)} ({new FileInfo(file).Length / (1024f * 1024f):0.0} MB)");
                EditorUtility.RevealInFinder(file);
            }
            else
            {
                Debug.LogError($"Android build {summary.result}: {summary.totalErrors} error(s). See the Console above.");
            }
        }
    }
}
