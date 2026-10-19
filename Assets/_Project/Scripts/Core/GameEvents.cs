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
        Fall,
        Bullet,
        WrongRoute // a wrong route at a fork in the path
    }

    // How the current question is answered: tapping a circle (enemy encounter) or taking a route (fork in the path).
    public enum AnswerMode
    {
        Tap,
        Route
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
        public QuestionResult(MathQuestion question, AnswerOutcome outcome, int chosenIndex, float timeLimit, float timeRemainingFraction)
        {
            Question = question;
            Outcome = outcome;
            ChosenIndex = chosenIndex;
            TimeLimit = timeLimit;
            TimeRemainingFraction = timeRemainingFraction;
        }

        public MathQuestion Question { get; }
        public AnswerOutcome Outcome { get; }
        public int ChosenIndex { get; } // -1 when missed
        public float TimeLimit { get; } // 0 = untimed
        public float TimeRemainingFraction { get; } // share of the countdown left when the answer was picked; 0 if untimed or missed
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
        // A request to pick answer i (keys, answer buttons, tests); the active Question Zone decides whether to accept it.
        public static event Action<int> AnswerChosen;
        // The active Question Zone accepted answer i; the choice is final for this question.
        public static event Action<int> AnswerLocked;
        public static event Action<HitCause> PlayerHit;
        public static event Action<int> LivesChanged;
        public static event Action OutOfLives;
        public static event Action<ScoreUpdate> ScoreChanged;
        public static event Action LevelCompleted;
        public static event Action<GameState> GameStateChanged;
        public static event Action<LevelResult> ProgressSaved;
        // An enemy fired: the world slows to this factor (e.g. 0.2) until FocusEnded.
        public static event Action<float> FocusStarted;
        public static event Action FocusEnded;
        // A correct answer's shockwave destroyed the enemy at this position.
        public static event Action<UnityEngine.Vector3> EnemyDefeated;
        // A coin was picked up; the value goes to the score only.
        public static event Action<int> CoinCollected;
        public static event Action<AnswerMode> AnswerModeSet;
        // Two hearts at once: the following fly joined a wrong answer's attack (for the "x2" indicator).
        public static event Action DoubleHit;

        public static void RaiseQuestionShown(MathQuestion question, float timeLimit) => QuestionShown?.Invoke(question, timeLimit);
        public static void RaiseQuestionResolved(QuestionResult result) => QuestionResolved?.Invoke(result);
        public static void RaiseAnswerChosen(int choiceIndex) => AnswerChosen?.Invoke(choiceIndex);
        public static void RaiseAnswerLocked(int choiceIndex) => AnswerLocked?.Invoke(choiceIndex);
        public static void RaisePlayerHit(HitCause cause) => PlayerHit?.Invoke(cause);
        public static void RaiseLivesChanged(int lives) => LivesChanged?.Invoke(lives);
        public static void RaiseOutOfLives() => OutOfLives?.Invoke();
        public static void RaiseScoreChanged(ScoreUpdate update) => ScoreChanged?.Invoke(update);
        public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();
        public static void RaiseGameStateChanged(GameState state) => GameStateChanged?.Invoke(state);
        public static void RaiseProgressSaved(LevelResult result) => ProgressSaved?.Invoke(result);
        public static void RaiseFocusStarted(float factor) => FocusStarted?.Invoke(factor);
        public static void RaiseFocusEnded() => FocusEnded?.Invoke();
        public static void RaiseEnemyDefeated(UnityEngine.Vector3 position) => EnemyDefeated?.Invoke(position);
        public static void RaiseCoinCollected(int value) => CoinCollected?.Invoke(value);
        public static void RaiseAnswerModeSet(AnswerMode mode) => AnswerModeSet?.Invoke(mode);
        public static void RaiseDoubleHit() => DoubleHit?.Invoke();
    }
}
