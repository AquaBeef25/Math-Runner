namespace MathRunner.Math
{
    public class QuestionGenerator
    {
        private const int MaxRepeatRerolls = 5;

        private readonly System.Random _rng;
        private readonly DistractorGenerator _distractors;
        private readonly int _choiceCount;
        private string _lastText;

        public QuestionGenerator(System.Random rng, int choiceCount = 3)
        {
            _rng = rng;
            _distractors = new DistractorGenerator(rng);
            _choiceCount = choiceCount;
        }

        public MathQuestion Generate(QuestionSettings settings, DifficultyTier tier)
        {
            MathProblem problem = default;
            for (int attempt = 0; attempt <= MaxRepeatRerolls; attempt++)
            {
                var rule = settings.Rules[_rng.Next(settings.Rules.Count)];
                problem = Build(rule.Type, rule.ForTier(tier));
                if (problem.Text != _lastText) break;
            }
            _lastText = problem.Text;

            int[] wrong = _distractors.Generate(problem, _choiceCount - 1);
            var choices = new int[_choiceCount];
            int correctIndex = _rng.Next(_choiceCount);
            for (int i = 0, w = 0; i < _choiceCount; i++)
            {
                choices[i] = i == correctIndex ? problem.Answer : wrong[w++];
            }
            return new MathQuestion(problem, choices, correctIndex);
        }

        private MathProblem Build(QuestionType type, OperandRanges r)
        {
            int a = r.A.Pick(_rng);
            int b = r.B.Pick(_rng);
            int c = r.C.Pick(_rng);

            switch (type)
            {
                case QuestionType.Add:
                    return new MathProblem(type, a + b, a, b);
                case QuestionType.Subtract:
                    if (b > a) (a, b) = (b, a);
                    return new MathProblem(type, a - b, a, b);
                case QuestionType.Multiply:
                    return new MathProblem(type, a * b, a, b);
                case QuestionType.Divide:
                    return new MathProblem(type, a, a * b, b);
                case QuestionType.AddMultiply:
                    return new MathProblem(type, a + b * c, a, b, c);
                case QuestionType.MultiplySubtract:
                    c = System.Math.Min(c, a * b);
                    return new MathProblem(type, a * b - c, a, b, c);
                case QuestionType.DivideAdd:
                    return new MathProblem(type, a + c, a * b, b, c);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
