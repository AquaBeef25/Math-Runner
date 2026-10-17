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

    public readonly struct QuestionResult
    {
        public QuestionResult(MathQuestion question, AnswerOutcome outcome, int chosenIndex)
        {
            Question = question;
            Outcome = outcome;
            ChosenIndex = chosenIndex;
        }

        public MathQuestion Question { get; }
        public AnswerOutcome Outcome { get; }
        public int ChosenIndex { get; } // -1 when missed
    }

    public static class GameEvents
    {
        // Time limit in seconds; 0 means untimed.
        public static event Action<MathQuestion, float> QuestionShown;
        public static event Action<QuestionResult> QuestionResolved;

        public static void RaiseQuestionShown(MathQuestion question, float timeLimit) => QuestionShown?.Invoke(question, timeLimit);

        public static void RaiseQuestionResolved(QuestionResult result) => QuestionResolved?.Invoke(result);
    }
}
