using MathRunner.Core;
using UnityEngine;

namespace MathRunner.Systems
{
    public class ScoreSystem : MonoBehaviour
    {
        [SerializeField] private QuestionDirector _director;
        [SerializeField] private int _basePoints = 100;
        [SerializeField] private int _timedBonus = 50;
        [SerializeField] private int _streakStep = 25;

        public int Score { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int CorrectCount { get; private set; }
        public int AnsweredCount { get; private set; }

        private void OnEnable()
        {
            GameEvents.QuestionResolved += OnQuestionResolved;
        }

        private void OnDisable()
        {
            GameEvents.QuestionResolved -= OnQuestionResolved;
        }

        private void Start()
        {
            GameEvents.RaiseScoreChanged(new ScoreUpdate(Score, 0, Streak));
        }

        private void OnQuestionResolved(QuestionResult result)
        {
            AnsweredCount++;
            int delta = 0;
            if (result.Outcome == AnswerOutcome.Correct)
            {
                float speed = result.TimeLimit > 0f ? result.TimeRemainingFraction : 0f;
                delta = ScoreCalculator.PointsForCorrect(_director.Level, Streak, speed, _basePoints, _timedBonus, _streakStep);
                Score += delta;
                Streak++;
                CorrectCount++;
                if (Streak > BestStreak) BestStreak = Streak;
            }
            else
            {
                Streak = 0;
            }
            GameEvents.RaiseScoreChanged(new ScoreUpdate(Score, delta, Streak));
        }
    }
}
