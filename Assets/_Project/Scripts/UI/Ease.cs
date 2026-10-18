using UnityEngine;

namespace MathRunner.UI
{
    // Easing curves for the tiny in-house tweens: t runs 0..1 and the result is 0 at t=0 and 1 at t=1.
    public static class Ease
    {
        private const float BackOvershoot = 1.70158f;

        public static float Linear(float t) => Mathf.Clamp01(t);

        public static float OutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        public static float InQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        // Overshoots past 1 a little before settling, for a "pop" feel.
        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * ((BackOvershoot + 1f) * t + BackOvershoot);
        }

        // 0 → 1 → 0 over t, peaking at t=0.5; used for flashes and pulses.
        public static float Pulse(float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Clamp01(Mathf.Sin(t * Mathf.PI)); // sin(π) is a hair below 0 in floats
        }
    }
}
