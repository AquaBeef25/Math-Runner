namespace MathRunner.Math
{
    // Operand meaning per type (A, B, C come from OperandRanges):
    //   Add            A + B
    //   Subtract       A - B   (larger operand shown first, never negative)
    //   Multiply       A × B
    //   Divide         (A·B) ÷ B = A        A is the quotient, B the divisor, so division is always exact
    //   AddMultiply    A + B × C            order of operations
    //   MultiplySubtract A × B - C          C is capped so the result is never negative
    //   DivideAdd      (A·B) ÷ B + C        A is the quotient, B the divisor
    public enum QuestionType
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        AddMultiply,
        MultiplySubtract,
        DivideAdd
    }

    public enum DifficultyTier
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }
}
