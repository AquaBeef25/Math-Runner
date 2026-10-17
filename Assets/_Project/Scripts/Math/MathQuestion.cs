using System.Collections.Generic;

namespace MathRunner.Math
{
    public readonly struct MathProblem
    {
        public MathProblem(QuestionType type, int answer, params int[] operands)
        {
            Type = type;
            Answer = answer;
            Operands = operands;
            Text = Format(type, operands);
        }

        public QuestionType Type { get; }
        public int Answer { get; }
        public IReadOnlyList<int> Operands { get; }
        public string Text { get; }

        // ASCII "-" rather than U+2212 so the default TextMeshPro font can render it.
        private static string Format(QuestionType type, int[] o) => type switch
        {
            QuestionType.Add => $"{o[0]} + {o[1]} = ?",
            QuestionType.Subtract => $"{o[0]} - {o[1]} = ?",
            QuestionType.Multiply => $"{o[0]} × {o[1]} = ?",
            QuestionType.Divide => $"{o[0]} ÷ {o[1]} = ?",
            QuestionType.AddMultiply => $"{o[0]} + {o[1]} × {o[2]} = ?",
            QuestionType.MultiplySubtract => $"{o[0]} × {o[1]} - {o[2]} = ?",
            QuestionType.DivideAdd => $"{o[0]} ÷ {o[1]} + {o[2]} = ?",
            _ => "?"
        };
    }

    public sealed class MathQuestion
    {
        public MathQuestion(MathProblem problem, int[] choices, int correctIndex)
        {
            Problem = problem;
            Choices = choices;
            CorrectIndex = correctIndex;
        }

        public MathProblem Problem { get; }
        public string Text => Problem.Text;
        public int Answer => Problem.Answer;
        public IReadOnlyList<int> Choices { get; }
        public int CorrectIndex { get; }

        public bool IsCorrect(int choiceIndex) => choiceIndex == CorrectIndex;
    }
}
