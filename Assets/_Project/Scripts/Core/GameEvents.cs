using System;
using MathRunner.Math;

namespace MathRunner.Core
{
    public enum AnswerOutcome
    {
        Correct,
        Wrong,
        Missed
    }

    public enum HitCause
    {
        Obstacle,
        Fall
    }

    public enum GameState
    {
        Playing,
        Paused,
        LevelComplete,
        GameOver
    }

    public readonly struct QuestionResult
    {
        public QuestionResult(MathQuestion question, AnswerOutcome outcome, int chosenIndex, float timeLimit)
        {
            Question = question;
            Outcome = outcome;
            ChosenIndex = chosenIndex;
            TimeLimit = timeLimit;
        }

        public MathQuestion Question { get; }
        public AnswerOutcome Outcome { get; }
        public int ChosenIndex { get; } // -1 when missed
        public float TimeLimit { get; } // 0 = untimed
    }

    public readonly struct ScoreUpdate
    {
        public ScoreUpdate(int score, int delta, int streak)
        {
            Score = score;
            Delta = delta;
            Streak = streak;
        }

        public int Score { get; }
        public int Delta { get; }
        public int Streak { get; }
    }

    public readonly struct LevelResult
    {
        public LevelResult(int levelNumber, int score, int stars, bool newBest, int bestScore)
        {
            LevelNumber = levelNumber;
            Score = score;
            Stars = stars;
            NewBest = newBest;
            BestScore = bestScore;
        }

        public int LevelNumber { get; }
        public int Score { get; }
        public int Stars { get; }
        public bool NewBest { get; }
        public int BestScore { get; }
    }

    public static class GameEvents
    {
        // Time limit in seconds; 0 means untimed.
        public static event Action<MathQuestion, float> QuestionShown;
        public static event Action<QuestionResult> QuestionResolved;
        public static event Action<HitCause> PlayerHit;
        public static event Action<int> LivesChanged;
        public static event Action OutOfLives;
        public static event Action<ScoreUpdate> ScoreChanged;
        public static event Action LevelCompleted;
        public static event Action<GameState> GameStateChanged;
        public static event Action<LevelResult> ProgressSaved;

        public static void RaiseQuestionShown(MathQuestion question, float timeLimit) => QuestionShown?.Invoke(question, timeLimit);
        public static void RaiseQuestionResolved(QuestionResult result) => QuestionResolved?.Invoke(result);
        public static void RaisePlayerHit(HitCause cause) => PlayerHit?.Invoke(cause);
        public static void RaiseLivesChanged(int lives) => LivesChanged?.Invoke(lives);
        public static void RaiseOutOfLives() => OutOfLives?.Invoke();
        public static void RaiseScoreChanged(ScoreUpdate update) => ScoreChanged?.Invoke(update);
        public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();
        public static void RaiseGameStateChanged(GameState state) => GameStateChanged?.Invoke(state);
        public static void RaiseProgressSaved(LevelResult result) => ProgressSaved?.Invoke(result);
    }
}
