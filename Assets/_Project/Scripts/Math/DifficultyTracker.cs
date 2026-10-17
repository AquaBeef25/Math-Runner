namespace MathRunner.Math
{
    // Adaptive tier within a level (GAME_DESIGN.md §2.5): N correct in a row steps up, any wrong answer steps down.
    public class DifficultyTracker
    {
        private readonly int _stepUpAfter;
        private int _correctInARow;

        public DifficultyTracker(DifficultyTier startTier = DifficultyTier.Normal, int stepUpAfter = 3)
        {
            Tier = startTier;
            _stepUpAfter = stepUpAfter;
        }

        public DifficultyTier Tier { get; private set; }

        public void RecordCorrect()
        {
            _correctInARow++;
            if (_correctInARow < _stepUpAfter) return;

            _correctInARow = 0;
            if (Tier < DifficultyTier.Hard) Tier++;
        }

        public void RecordWrong()
        {
            _correctInARow = 0;
            if (Tier > DifficultyTier.Easy) Tier--;
        }
    }
}
