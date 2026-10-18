using UnityEngine;

namespace MathRunner.Level
{
    // The maths behind the harmless moving life, pure so it can be tested. Times are in world (scaled) seconds, so life
    // slows down in the Focus Zone with everything else.
    public static class LifeMotion
    {
        private const float HopSeconds = 0.4f;
        private const float LeapSeconds = 0.9f;

        // A lazy figure-eight around a spot: |x| ≤ radius, |y| ≤ radius / 2.
        public static Vector2 FlyerOffset(float t, float radius, float hz)
        {
            float a = 2f * Mathf.PI * hz * t;
            return new Vector2(radius * Mathf.Sin(a), radius * 0.5f * Mathf.Sin(2f * a));
        }

        // Sits still, then one parabolic hop of `height` in the last 0.4 s of every period.
        public static float HopHeight(float t, float period, float height)
        {
            float start = Mathf.Max(0f, period - HopSeconds);
            float tau = Mathf.Repeat(t, period);
            if (tau < start) return 0f;
            float u = (tau - start) / (period - start);
            return height * 4f * u * (1f - u);
        }

        public static float SwingAngle(float t, float maxDegrees, float hz) => maxDegrees * Mathf.Sin(2f * Mathf.PI * hz * t);

        // Hidden under the water most of the period, then one leap across `width`, peaking at `height` above the surface.
        public static (Vector2 Offset, bool Visible) FishLeap(float t, float period, float width, float height)
        {
            float start = Mathf.Max(0f, period - LeapSeconds);
            float tau = Mathf.Repeat(t, period);
            if (tau < start) return (Vector2.zero, false);
            float u = (tau - start) / (period - start);
            return (new Vector2((u - 0.5f) * width, height * 4f * u * (1f - u)), true);
        }
    }
}
