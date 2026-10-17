using System.Collections.Generic;

namespace MathRunner.Math
{
    // Builds plausible wrong answers: one "typical mistake" when available, then near misses.
    public class DistractorGenerator
    {
        private readonly System.Random _rng;

        public DistractorGenerator(System.Random rng)
        {
            _rng = rng;
        }

        public int[] Generate(MathProblem problem, int count)
        {
            int answer = problem.Answer;
            var chosen = new List<int>(count);

            var mistakes = MistakeCandidates(problem);
            var nearMisses = NearMissCandidates(answer);
            Shuffle(mistakes);
            Shuffle(nearMisses);

            PickFrom(mistakes, chosen, answer, System.Math.Min(1, count));
            PickFrom(nearMisses, chosen, answer, count);
            PickFrom(mistakes, chosen, answer, count);

            for (int k = 3; chosen.Count < count; k++)
            {
                TryAdd(answer + k, chosen, answer);
            }
            return chosen.ToArray();
        }

        private static void PickFrom(List<int> pool, List<int> chosen, int answer, int target)
        {
            foreach (int value in pool)
            {
                if (chosen.Count >= target) return;
                TryAdd(value, chosen, answer);
            }
        }

        // Rejects the mirror of an existing pick (e.g. 47 and 49 for 48) so the answer isn't always the middle value.
        private static void TryAdd(int value, List<int> chosen, int answer)
        {
            if (value < 0 || value == answer || chosen.Contains(value) || chosen.Contains(2 * answer - value)) return;
            chosen.Add(value);
        }

        private static List<int> NearMissCandidates(int answer)
        {
            var list = new List<int> { answer + 1, answer - 1, answer + 2, answer - 2 };
            if (answer >= 10) { list.Add(answer + 10); list.Add(answer - 10); }
            if (answer >= 100) { list.Add(answer + 100); list.Add(answer - 100); }
            return list;
        }

        private static List<int> MistakeCandidates(MathProblem p)
        {
            var o = p.Operands;
            var list = new List<int>();
            switch (p.Type)
            {
                case QuestionType.Add:
                    list.Add(System.Math.Abs(o[0] - o[1]));
                    break;
                case QuestionType.Subtract:
                    list.Add(o[0] + o[1]);
                    break;
                case QuestionType.Multiply:
                    list.Add(o[0] + o[1]);
                    list.Add(o[0] * (o[1] + 1));
                    list.Add(o[0] * (o[1] - 1));
                    list.Add((o[0] + 1) * o[1]);
                    break;
                case QuestionType.Divide:
                    list.Add(o[0] - o[1]);
                    break;
                case QuestionType.AddMultiply:
                    list.Add((o[0] + o[1]) * o[2]);
                    list.Add(o[0] + o[1] + o[2]);
                    break;
                case QuestionType.MultiplySubtract:
                    if (o[1] >= o[2]) list.Add(o[0] * (o[1] - o[2]));
                    list.Add(o[0] * o[1] + o[2]);
                    break;
                case QuestionType.DivideAdd:
                    list.Add(p.Answer - o[2]);
                    if (o[0] % (o[1] + o[2]) == 0) list.Add(o[0] / (o[1] + o[2]));
                    break;
            }

            int swapped = SwapLastTwoDigits(p.Answer);
            if (swapped >= 0) list.Add(swapped);
            return list;
        }

        // Only for 3+ digit answers: 134 -> 143 is a believable slip, but 16 -> 61 is obviously wrong.
        private static int SwapLastTwoDigits(int value)
        {
            if (value < 100) return -1;
            int ones = value % 10;
            int tens = value / 10 % 10;
            return value - ones - tens * 10 + ones * 10 + tens;
        }

        private void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
