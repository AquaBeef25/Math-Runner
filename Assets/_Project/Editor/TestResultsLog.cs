using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace MathRunner.EditorTools
{
    // Writes a summary of every Test Runner run to Logs/TestResults-<Mode>.txt. Registered on every domain reload,
    // so PlayMode results survive the reload that entering Play mode causes.
    [InitializeOnLoad]
    internal static class TestResultsLog
    {
        static TestResultsLog()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Writer());
        }

        private sealed class Writer : ICallbacks
        {
            private string _mode = "Unknown";

            public void RunStarted(ITestAdaptor testsToRun)
            {
                _mode = testsToRun.TestMode.ToString();
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} {_mode}: {result.ResultState} " +
                              $"(passed {result.PassCount}, failed {result.FailCount}, skipped {result.SkipCount}, {result.Duration:F1}s)");
                AppendLeaves(result, sb);
                string dir = Path.Combine(Application.dataPath, "..", "Logs");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, $"TestResults-{_mode}.txt"), sb.ToString());
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }

            private static void AppendLeaves(ITestResultAdaptor node, StringBuilder sb)
            {
                if (!node.HasChildren)
                {
                    sb.AppendLine($"  {node.TestStatus,-8} {node.FullName} ({node.Duration:F2}s)");
                    if (node.TestStatus == TestStatus.Failed) sb.AppendLine("           " + node.Message?.Trim().Replace("\n", "\n           "));
                    return;
                }
                foreach (var child in node.Children) AppendLeaves(child, sb);
            }
        }
    }
}
