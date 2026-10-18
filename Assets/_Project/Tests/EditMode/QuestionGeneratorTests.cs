using System.Linq;
using MathRunner.Math;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class QuestionGeneratorTests
    {
        private const int SamplesPerCase = 500;

        [Test]
        public void EveryLevelHasValidSettings([Range(1, LevelCurriculum.LevelCount)] int level)
        {
            var errors = LevelCurriculum.ForLevel(level).Validate();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        // Enemy encounters show four answers, so every shipped level must be able to produce four sensible choices.
        [Test]
        public void EveryLevelGivesFourDistinctNonNegativeChoices([Values] DifficultyTier tier)
        {
            foreach (var level in MathRunner.Level.LevelCatalog.Load().Levels)
            {
                var generator = new QuestionGenerator(new System.Random(level.LevelNumber * 7 + (int)tier), 4);
                for (int i = 0; i < 300; i++)
                {
                    var q = generator.Generate(level.Questions, tier);
                    string ctx = $"Level {level.LevelNumber} {tier}: '{q.Text}' [{string.Join(", ", q.Choices)}]";
                    Assert.That(q.Choices.Count, Is.EqualTo(4), ctx);
                    Assert.That(q.Choices.Distinct().Count(), Is.EqualTo(4), ctx);
                    Assert.That(q.Choices.All(c => c >= 0), Is.True, ctx);
                    Assert.That(q.Choices[q.CorrectIndex], Is.EqualTo(q.Answer), ctx);
                }
            }
        }

        [Test]
        public void GeneratedQuestionsAreCorrectAndWellFormed(
            [Range(1, LevelCurriculum.LevelCount)] int level,
            [Values] DifficultyTier tier)
        {
            var settings = LevelCurriculum.ForLevel(level);
            var generator = new QuestionGenerator(new System.Random(1000 + level * 10 + (int)tier));

            for (int i = 0; i < SamplesPerCase; i++)
            {
                var q = generator.Generate(settings, tier);
                string ctx = $"L{level} {tier}: '{q.Text}' choices [{string.Join(", ", q.Choices)}]";

                Assert.That(ExpressionEvaluator.Evaluate(q.Text), Is.EqualTo(q.Answer), ctx);
                Assert.That(q.Answer, Is.GreaterThanOrEqualTo(0), ctx);
                Assert.That(q.Choices.Count, Is.EqualTo(3), ctx);
                Assert.That(q.Choices, Is.Unique, ctx);
                Assert.That(q.Choices, Is.All.GreaterThanOrEqualTo(0), ctx);
                Assert.That(q.Choices[q.CorrectIndex], Is.EqualTo(q.Answer), ctx);
                Assert.That(q.Choices.Count(c => c == q.Answer), Is.EqualTo(1), ctx);
                Assert.That(q.Problem.Operands, Is.All.GreaterThanOrEqualTo(0), ctx);
            }
        }

        [Test]
        public void SameSeedGivesSameQuestions()
        {
            var settings = LevelCurriculum.ForLevel(8);
            var a = new QuestionGenerator(new System.Random(42));
            var b = new QuestionGenerator(new System.Random(42));

            for (int i = 0; i < 100; i++)
            {
                var qa = a.Generate(settings, DifficultyTier.Normal);
                var qb = b.Generate(settings, DifficultyTier.Normal);
                Assert.That(qb.Text, Is.EqualTo(qa.Text));
                Assert.That(qb.Choices, Is.EqualTo(qa.Choices));
                Assert.That(qb.CorrectIndex, Is.EqualTo(qa.CorrectIndex));
            }
        }

        [Test]
        public void NoQuestionRepeatsImmediately([Range(1, LevelCurriculum.LevelCount)] int level)
        {
            var settings = LevelCurriculum.ForLevel(level);
            var generator = new QuestionGenerator(new System.Random(7 + level));
            string last = null;
            for (int i = 0; i < 300; i++)
            {
                var q = generator.Generate(settings, DifficultyTier.Easy);
                Assert.That(q.Text, Is.Not.EqualTo(last), $"L{level} repeated '{q.Text}' at sample {i}");
                last = q.Text;
            }
        }

        [Test]
        public void CorrectAnswerPositionIsSpreadAcrossPlatforms()
        {
            var settings = LevelCurriculum.ForLevel(5);
            var generator = new QuestionGenerator(new System.Random(3));
            var counts = new int[3];
            for (int i = 0; i < 3000; i++) counts[generator.Generate(settings, DifficultyTier.Normal).CorrectIndex]++;
            Assert.That(counts, Is.All.InRange(850, 1150), $"Position counts: {string.Join(", ", counts)}");
        }

        [Test]
        public void Levels1And2UseSingleDigits([Values(1, 2)] int level, [Values] DifficultyTier tier)
        {
            var settings = LevelCurriculum.ForLevel(level);
            var generator = new QuestionGenerator(new System.Random(level));
            for (int i = 0; i < SamplesPerCase; i++)
            {
                var q = generator.Generate(settings, tier);
                Assert.That(q.Problem.Operands, Is.All.InRange(0, 9), q.Text);
            }
        }

        [Test]
        public void Level10DividesAThreeDigitNumberExactly([Values] DifficultyTier tier)
        {
            var settings = LevelCurriculum.ForLevel(10);
            var generator = new QuestionGenerator(new System.Random(10));
            for (int i = 0; i < SamplesPerCase; i++)
            {
                var q = generator.Generate(settings, tier);
                int dividend = q.Problem.Operands[0], divisor = q.Problem.Operands[1];
                Assert.That(dividend, Is.InRange(100, 999), q.Text);
                Assert.That(dividend % divisor, Is.Zero, q.Text);
            }
        }

        // Traceability to the instructor-approved examples in GAME_DESIGN.md §1.
        [Test]
        public void ApprovedExamplesFitTheirLevelRanges()
        {
            AssertFits(1, QuestionType.Add, 5, 3);       // 5 + 3
            AssertFits(5, QuestionType.Multiply, 6, 8);  // 6 × 8
            AssertFits(10, QuestionType.DivideAdd, 91, 5, 7); // 455 ÷ 5 + 7 (whole-number version of 453 ÷ 5 + 7)
        }

        [Test]
        public void ValidateRejectsBadSettings()
        {
            var zeroDivisor = new QuestionSettings(new QuestionRule(QuestionType.Divide,
                Ranges(1, 5, 0, 5), Ranges(1, 5, 1, 5), Ranges(1, 5, 1, 5)));
            Assert.That(zeroDivisor.Validate(), Has.Some.Contains("divisor"));

            var inverted = new QuestionSettings(new QuestionRule(QuestionType.Add,
                Ranges(9, 1, 0, 5), Ranges(1, 5, 1, 5), Ranges(1, 5, 1, 5)));
            Assert.That(inverted.Validate(), Has.Some.Contains("greater than max"));

            Assert.That(new QuestionSettings().Validate(), Has.Some.Contains("No question rules"));
        }

        private static void AssertFits(int level, QuestionType type, int a, int b, int c = 0)
        {
            var rule = LevelCurriculum.ForLevel(level).Rules.First(r => r.Type == type);
            var r = rule.ForTier(DifficultyTier.Normal);
            Assert.That(r.A.Contains(a) && r.B.Contains(b) && (!QuestionRule.UsesThirdOperand(type) || r.C.Contains(c)),
                $"Level {level} {type} Normal ranges don't include ({a}, {b}, {c})");
        }

        private static OperandRanges Ranges(int aMin, int aMax, int bMin, int bMax) =>
            new OperandRanges(new IntRange(aMin, aMax), new IntRange(bMin, bMax));
    }
}
