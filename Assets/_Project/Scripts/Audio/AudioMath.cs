using UnityEngine;

namespace MathRunner.Audio
{
    // Pure helpers so the sound rules can be tested without playing audio.
    public static class AudioMath
    {
        // Each correct answer in a row raises the pitch a step, up to a cap, so streaks sound like they're climbing.
        public static float StreakPitch(int streak, float step, int maxSteps)
        {
            int steps = Mathf.Clamp(streak - 1, 0, maxSteps);
            return 1f + steps * step;
        }

        // Whether the countdown crossed a whole second (at or below `fromSecond`) between two frames, e.g. 3.02 → 2.98 ticks for "3".
        // Reaching 0 is the timeout, not a tick.
        public static bool CrossedTick(float previousRemaining, float remaining, int fromSecond)
        {
            if (remaining <= 0f || remaining >= previousRemaining) return false;
            int after = Mathf.CeilToInt(remaining); // the highest whole second crossed this frame
            return after < Mathf.CeilToInt(previousRemaining) && after <= fromSecond;
        }
    }
}
