using MathRunner.Level;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathRunner.Core
{
    public static class SceneFlow
    {
        public const string MainMenu = "MainMenu";
        public const string LevelSelect = "LevelSelect";
        public const string Game = "Game";

        public static void Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }

        public static void PlayLevel(LevelConfig level)
        {
            LevelSelection.Current = level;
            Load(Game);
        }
    }
}
