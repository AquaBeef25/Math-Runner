namespace MathRunner.Level
{
    // One route of a fork: where it runs and how high, the jump that reaches it (take-off X and hold in real seconds; NaN /
    // 0 for the low road, which needs no jump), and where its mud (shown when it's a wrong answer) and chest sit.
    public readonly struct ForkLane
    {
        public ForkLane(float left, float right, float top, float takeoffX, float holdSeconds, float mudLeft, float mudRight, float chestX)
        {
            Left = left;
            Right = right;
            Top = top;
            TakeoffX = takeoffX;
            HoldSeconds = holdSeconds;
            MudLeft = mudLeft;
            MudRight = mudRight;
            ChestX = chestX;
        }

        public float Left { get; }
        public float Right { get; }
        public float Top { get; }
        public float TakeoffX { get; }
        public float HoldSeconds { get; }
        public float MudLeft { get; }
        public float MudRight { get; }
        public float ChestX { get; }
    }
}
