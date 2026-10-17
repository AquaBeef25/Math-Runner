namespace MathRunner.Systems
{
    // GAME_DESIGN.md §2.3: base × level, flat bonus on timed questions, and a bonus per correct answer already in the streak.
    public static class ScoreCalculator
    {
        public static int PointsForCorrect(int level, int streakBefore, bool timed, int basePoints, int timedBonus, int streakStep) =>
            basePoints * level + (timed ? timedBonus : 0) + streakStep * streakBefore;
    }
}
