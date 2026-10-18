using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Level
{
    // What a full jump can reach, from the same numbers the PlayerController uses. The planner only places steps,
    // gaps and platforms that pass CanReach, which keeps a 15 % safety margin on both height and distance.
    public static class JumpReach
    {
        private const float Margin = 0.85f;

        private static float G(LevelPlanSettings s) => Mathf.Abs(s.Gravity) * s.GravityScale;

        // Seconds from take-off to landing on ground heightDiff higher (negative = lower) on a full jump.
        public static float AirTime(LevelPlanSettings s, float heightDiff) =>
            JumpMath.AirTimeToLand(s.JumpHeight, s.Gravity, s.GravityScale, s.FallMultiplier, heightDiff);

        public static float Distance(LevelPlanSettings s, float heightDiff) => AirTime(s, heightDiff) * s.RunSpeed;

        public static bool CanReach(LevelPlanSettings s, float dx, float heightDiff) =>
            heightDiff <= s.JumpHeight * Margin && dx <= Distance(s, heightDiff) * Margin;

        // Seconds after take-off until the feet are `height` above the take-off ground (rising part of the jump).
        public static float RiseTime(LevelPlanSettings s, float height)
        {
            float g = G(s);
            float v0 = Mathf.Sqrt(2f * g * s.JumpHeight);
            float disc = v0 * v0 - 2f * g * height;
            return disc < 0f ? float.PositiveInfinity : (v0 - Mathf.Sqrt(disc)) / g;
        }
    }
}
