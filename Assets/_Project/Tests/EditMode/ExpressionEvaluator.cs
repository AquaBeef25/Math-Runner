using System.Collections.Generic;
using NUnit.Framework;

namespace MathRunner.Tests
{
    // Independently evaluates a question's display text ("4 + 6 × 3 = ?") with normal operator precedence,
    // so tests catch any mismatch between what the player sees and the stored answer.
    public static class ExpressionEvaluator
    {
        public static int Evaluate(string text)
        {
            Assert.That(text, Does.EndWith(" = ?"), $"Unexpected format: '{text}'");
            string[] tokens = text.Substring(0, text.Length - 4).Split(' ');
            Assert.That(tokens.Length % 2, Is.EqualTo(1), $"Unexpected token count: '{text}'");

            var values = new List<int> { int.Parse(tokens[0]) };
            var ops = new List<string>();
            for (int i = 1; i < tokens.Length; i += 2)
            {
                string op = tokens[i];
                int n = int.Parse(tokens[i + 1]);
                if (op == "×")
                {
                    values[values.Count - 1] *= n;
                }
                else if (op == "÷")
                {
                    int left = values[values.Count - 1];
                    Assert.That(n, Is.Not.Zero, $"Division by zero in '{text}'");
                    Assert.That(left % n, Is.Zero, $"Division is not exact in '{text}'");
                    values[values.Count - 1] = left / n;
                }
                else if (op == "+" || op == "-")
                {
                    ops.Add(op);
                    values.Add(n);
                }
                else
                {
                    Assert.Fail($"Unknown operator '{op}' in '{text}'");
                }
            }

            int result = values[0];
            for (int i = 0; i < ops.Count; i++)
            {
                result = ops[i] == "+" ? result + values[i + 1] : result - values[i + 1];
            }
            return result;
        }
    }
}
