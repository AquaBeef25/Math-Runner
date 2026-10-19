using MathRunner.Core;
using UnityEngine;

namespace MathRunner.Level
{
    // Pure rules of a fork in the path: which route the runner took, whether that was right, and how much of the
    // question's time was left (for the speed bonus).
    public static class ForkLogic
    {
        // Half-way between the routes' heights above the split's ground (high +2.7, middle +1, low −1).
        private const float HighBand = (LevelPlanner.ForkHighRise + LevelPlanner.ForkMiddleRise) * 0.5f;
        private const float MiddleBand = 0.5f;
        private const float RisingSpeed = 0.1f;

        // The route the feet are standing on, or −1 while airborne or still rising (a hop passing up through a one-way
        // ledge has not chosen it yet).
        public static int LaneFor(float feetY, float baseTop, bool grounded, float verticalSpeed)
        {
            if (!grounded || verticalSpeed > RisingSpeed) return -1;
            float height = feetY - baseTop;
            if (height >= HighBand) return ForkPlan.High;
            if (height >= MiddleBand) return ForkPlan.Middle;
            return ForkPlan.Low;
        }

        public static AnswerOutcome Outcome(int lane, int correctIndex) =>
            lane == correctIndex ? AnswerOutcome.Correct : AnswerOutcome.Wrong;

        // Share of the countdown left when the route was taken; 0 for untimed questions.
        public static float RemainingFraction(float elapsedRealSeconds, float limit) =>
            limit > 0f ? Mathf.Clamp01(1f - elapsedRealSeconds / limit) : 0f;
    }
}
