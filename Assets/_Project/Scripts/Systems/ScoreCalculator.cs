namespace MathRunner.Systems
{
    // GAME_DESIGN.md §2.3: base × level, a speed bonus on timed questions (share of the countdown left when the answer
    // was picked), and a bonus per correct answer already in the streak.
    public static class ScoreCalculator
    {
        public static int PointsForCorrect(int level, int streakBefore, float timeRemainingFraction, int basePoints, int timedBonus, int streakStep)
        {
            float fraction = System.Math.Max(0f, System.Math.Min(1f, timeRemainingFraction));
            return basePoints * level + (int)System.Math.Round(timedBonus * fraction, System.MidpointRounding.AwayFromZero) + streakStep * streakBefore;
        }
    }
}
