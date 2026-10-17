using System;

namespace MathRunner.Math
{
    // Default question rules for levels 1–10 (GAME_DESIGN.md §2.4). Each call returns a fresh copy,
    // so LevelConfig assets can be seeded from it and then tuned in the Inspector.
    public static class LevelCurriculum
    {
        public const int LevelCount = 10;

        public static QuestionSettings ForLevel(int level)
        {
            switch (level)
            {
                case 1: // Addition, single digit — approved example 5 + 3
                    return new QuestionSettings(
                        Rule(QuestionType.Add, R(1, 5, 1, 5), R(1, 9, 1, 9), R(4, 9, 4, 9)));

                case 2: // Subtraction, single digit, never negative
                    return new QuestionSettings(
                        Rule(QuestionType.Subtract, R(1, 5, 1, 5), R(1, 9, 1, 9), R(5, 9, 3, 9)));

                case 3: // Add/Sub within 20
                    return new QuestionSettings(
                        Rule(QuestionType.Add, R(1, 10, 1, 5), R(5, 14, 2, 6), R(8, 12, 6, 8)),
                        Rule(QuestionType.Subtract, R(5, 12, 1, 5), R(10, 20, 2, 9), R(13, 20, 5, 9)));

                case 4: // Two-digit add/sub
                    return new QuestionSettings(
                        Rule(QuestionType.Add, R(10, 50, 10, 40), R(10, 60, 10, 39), R(30, 99, 20, 99)),
                        Rule(QuestionType.Subtract, R(20, 60, 10, 19), R(30, 99, 10, 60), R(50, 99, 25, 99)));

                case 5: // Multiplication tables 2–9 — approved example 6 × 8
                    return new QuestionSettings(
                        Rule(QuestionType.Multiply, R(2, 5, 2, 5), R(2, 9, 2, 9), R(6, 9, 6, 9)));

                case 6: // Exact division from the tables (A = quotient, B = divisor)
                    return new QuestionSettings(
                        Rule(QuestionType.Divide, R(2, 5, 2, 5), R(2, 9, 2, 9), R(6, 9, 6, 9)));

                case 7: // Mixed × and ÷ up to 12
                    return new QuestionSettings(
                        Rule(QuestionType.Multiply, R(2, 10, 2, 10), R(2, 12, 2, 12), R(7, 12, 7, 12)),
                        Rule(QuestionType.Divide, R(2, 10, 2, 10), R(2, 12, 2, 12), R(7, 12, 7, 12)));

                case 8: // Two-step, order of operations
                    return new QuestionSettings(
                        Rule(QuestionType.AddMultiply, R(1, 9, 2, 5, 2, 5), R(1, 20, 2, 9, 2, 9), R(10, 50, 4, 12, 4, 9)),
                        Rule(QuestionType.MultiplySubtract, R(2, 5, 2, 5, 1, 5), R(2, 9, 2, 9, 1, 10), R(6, 12, 6, 9, 5, 20)));

                case 9: // Three-digit add/sub, two-digit × one-digit
                    return new QuestionSettings(
                        Rule(QuestionType.Add, R(100, 300, 100, 300), R(100, 599, 100, 399), R(300, 699, 150, 299)),
                        Rule(QuestionType.Subtract, R(200, 500, 100, 199), R(300, 999, 100, 299), R(500, 999, 150, 499)),
                        Rule(QuestionType.Multiply, R(11, 20, 2, 5), R(11, 50, 2, 9), R(25, 99, 6, 9)));

                case 10: // Three-digit ÷ one-digit + c. Approved example 453 ÷ 5 + 7 isn't whole; 455 ÷ 5 + 7 (A=91, B=5, C=7) is the whole-number equivalent.
                    // Ranges keep the dividend (A·B) between 100 and 999 in every tier.
                    return new QuestionSettings(
                        Rule(QuestionType.DivideAdd, R(50, 99, 2, 5, 1, 9), R(25, 110, 4, 9, 1, 20), R(17, 111, 6, 9, 10, 50)));

                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, $"Level must be 1–{LevelCount}.");
            }
        }

        private static QuestionRule Rule(QuestionType type, OperandRanges easy, OperandRanges normal, OperandRanges hard) =>
            new QuestionRule(type, easy, normal, hard);

        private static OperandRanges R(int aMin, int aMax, int bMin, int bMax, int cMin = 0, int cMax = 0) =>
            new OperandRanges(new IntRange(aMin, aMax), new IntRange(bMin, bMax), new IntRange(cMin, cMax));
    }
}
