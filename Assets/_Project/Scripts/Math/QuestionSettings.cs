using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathRunner.Math
{
    [Serializable]
    public struct IntRange
    {
        [SerializeField] private int _min;
        [SerializeField] private int _max;

        public IntRange(int min, int max)
        {
            _min = min;
            _max = max;
        }

        public int Min => _min;
        public int Max => _max;

        public bool Contains(int value) => value >= _min && value <= _max;

        public int Pick(System.Random rng) => rng.Next(_min, _max + 1);
    }

    [Serializable]
    public class OperandRanges
    {
        [SerializeField] private IntRange _a;
        [SerializeField] private IntRange _b;
        [SerializeField] private IntRange _c;

        public OperandRanges() { }

        public OperandRanges(IntRange a, IntRange b, IntRange c = default)
        {
            _a = a;
            _b = b;
            _c = c;
        }

        public IntRange A => _a;
        public IntRange B => _b;
        public IntRange C => _c;
    }

    [Serializable]
    public class QuestionRule
    {
        [SerializeField] private QuestionType _type;
        [SerializeField] private OperandRanges _easy;
        [SerializeField] private OperandRanges _normal;
        [SerializeField] private OperandRanges _hard;

        public QuestionRule() { }

        public QuestionRule(QuestionType type, OperandRanges easy, OperandRanges normal, OperandRanges hard)
        {
            _type = type;
            _easy = easy;
            _normal = normal;
            _hard = hard;
        }

        public QuestionType Type => _type;

        public OperandRanges ForTier(DifficultyTier tier) => tier switch
        {
            DifficultyTier.Easy => _easy,
            DifficultyTier.Hard => _hard,
            _ => _normal
        };

        public static bool UsesThirdOperand(QuestionType type) =>
            type == QuestionType.AddMultiply || type == QuestionType.MultiplySubtract || type == QuestionType.DivideAdd;

        public static bool IsDivision(QuestionType type) =>
            type == QuestionType.Divide || type == QuestionType.DivideAdd;
    }

    [Serializable]
    public class QuestionSettings
    {
        [SerializeField] private List<QuestionRule> _rules = new List<QuestionRule>();

        public QuestionSettings() { }

        public QuestionSettings(params QuestionRule[] rules)
        {
            _rules = new List<QuestionRule>(rules);
        }

        public IReadOnlyList<QuestionRule> Rules => _rules;

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (_rules == null || _rules.Count == 0)
            {
                errors.Add("No question rules.");
                return errors;
            }

            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                foreach (DifficultyTier tier in Enum.GetValues(typeof(DifficultyTier)))
                {
                    var r = rule.ForTier(tier);
                    string where = $"Rule {i} ({rule.Type}) {tier}";
                    if (r == null)
                    {
                        errors.Add($"{where}: missing ranges.");
                        continue;
                    }
                    CheckRange(errors, where, "A", r.A);
                    CheckRange(errors, where, "B", r.B);
                    if (QuestionRule.UsesThirdOperand(rule.Type)) CheckRange(errors, where, "C", r.C);
                    if (QuestionRule.IsDivision(rule.Type) && r.B.Min < 1) errors.Add($"{where}: divisor B must be at least 1.");
                }
            }
            return errors;
        }

        private static void CheckRange(List<string> errors, string where, string name, IntRange range)
        {
            if (range.Min < 0) errors.Add($"{where}: {name} must not be negative.");
            if (range.Min > range.Max) errors.Add($"{where}: {name} min ({range.Min}) is greater than max ({range.Max}).");
        }
    }
}
