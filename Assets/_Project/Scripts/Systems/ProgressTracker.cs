using MathRunner.Core;
using MathRunner.Save;
using UnityEngine;

namespace MathRunner.Systems
{
    // Saves the result when a level is completed: best score, stars, and unlocking the next level.
    public class ProgressTracker : MonoBehaviour
    {
        [SerializeField] private QuestionDirector _director;
        [SerializeField] private ScoreSystem _score;
        [SerializeField] private LivesSystem _lives;

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.LevelComplete) return;

            int level = _director.Level;
            int stars = StarRating.For(_score.CorrectCount, _score.AnsweredCount, _lives.Lives, _lives.StartingLives);
            bool newBest = SaveSystem.Data.RecordCompletion(level, _score.Score, stars);
            SaveSystem.Save();
            GameEvents.RaiseProgressSaved(new LevelResult(level, _score.Score, stars, newBest, SaveSystem.Data.GetRecord(level).BestScore));
        }
    }
}
