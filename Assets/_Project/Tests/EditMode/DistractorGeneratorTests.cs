using System.Linq;
using MathRunner.Math;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class DistractorGeneratorTests
    {
        [Test]
        public void AnswerOfZeroStillGetsValidDistractors()
        {
            var problem = new MathProblem(QuestionType.Subtract, 0, 4, 4);
            for (int seed = 0; seed < 50; seed++)
            {
                var d = new DistractorGenerator(new System.Random(seed)).Generate(problem, 2);
                Assert.That(d.Length, Is.EqualTo(2));
                Assert.That(d, Is.Unique);
                Assert.That(d, Is.All.GreaterThan(0));
            }
        }

        [Test]
        public void NeverPicksAPairMirroredAroundTheAnswer()
        {
            var problem = new MathProblem(QuestionType.Add, 48, 20, 28);
            for (int seed = 0; seed < 200; seed++)
            {
                var d = new DistractorGenerator(new System.Random(seed)).Generate(problem, 2);
                Assert.That(d[0] + d[1], Is.Not.EqualTo(2 * problem.Answer), $"Seed {seed}: {d[0]}, {d[1]}");
            }
        }

        [Test]
        public void OffersTheLeftToRightMistakeForOrderOfOperations()
        {
            var problem = new MathProblem(QuestionType.AddMultiply, 22, 4, 6, 3); // (4 + 6) × 3 = 30
            bool seen = Enumerable.Range(0, 50)
                .Any(seed => new DistractorGenerator(new System.Random(seed)).Generate(problem, 2).Contains(30));
            Assert.That(seen, Is.True);
        }

        [Test]
        public void OffersANeighbouringTimesTableMistake()
        {
            var problem = new MathProblem(QuestionType.Multiply, 48, 6, 8); // 6 × 7 = 42, 6 × 9 = 54, 7 × 8 = 56
            bool seen = Enumerable.Range(0, 50)
                .Any(seed => new DistractorGenerator(new System.Random(seed)).Generate(problem, 2).Intersect(new[] { 42, 54, 56 }).Any());
            Assert.That(seen, Is.True);
        }

        [Test]
        public void SwapsDigitsOnlyForThreeDigitAnswers()
        {
            var twoDigit = new MathProblem(QuestionType.Subtract, 16, 19, 3);
            var threeDigit = new MathProblem(QuestionType.DivideAdd, 134, 855, 9, 39);
            for (int seed = 0; seed < 50; seed++)
            {
                Assert.That(new DistractorGenerator(new System.Random(seed)).Generate(twoDigit, 2), Has.None.EqualTo(61));
            }
            bool seen = Enumerable.Range(0, 50)
                .Any(seed => new DistractorGenerator(new System.Random(seed)).Generate(threeDigit, 2).Contains(143));
            Assert.That(seen, Is.True);
        }

        [Test]
        public void CanProduceMoreDistractorsThanCandidates()
        {
            var problem = new MathProblem(QuestionType.Add, 1, 0, 1);
            var d = new DistractorGenerator(new System.Random(1)).Generate(problem, 6);
            Assert.That(d.Length, Is.EqualTo(6));
            Assert.That(d, Is.Unique);
            Assert.That(d, Is.All.GreaterThanOrEqualTo(0));
            Assert.That(d, Has.None.EqualTo(1));
        }
    }
}
