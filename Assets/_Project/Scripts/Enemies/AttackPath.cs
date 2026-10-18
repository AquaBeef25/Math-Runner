using UnityEngine;

namespace MathRunner.Enemies
{
    // The paths bee, frog and fly take to the runner, pure so they can be tested. All arrive exactly when their time
    // runs out: the base point homes in like the fireball, and any extra (leap height, buzz wobble) is zero at arrival.
    public static class AttackPath
    {
        // Same homing rule as the fireball: cover this step's share of the remaining distance to the (moving) target.
        public static Vector2 Charge(Vector2 position, Vector2 target, float remainingSeconds, float deltaSeconds) =>
            EnemyBullet.Step(position, target, remainingSeconds, deltaSeconds);

        // Height of a leap above the straight line at progress u (0 → 1): a parabola peaking at `peak` halfway.
        public static float LeapHeight(float u, float peak)
        {
            u = Mathf.Clamp01(u);
            return peak * 4f * u * (1f - u);
        }

        // Hopping from `from` to `to` in `hops` small hops (progress k 0 → 1): touches down between hops, lands on `to`.
        public static Vector2 HopApproach(Vector2 from, Vector2 to, float k, float hopHeight, int hops)
        {
            k = Mathf.Clamp01(k);
            float u = Mathf.Repeat(k * Mathf.Max(1, hops), 1f);
            if (k >= 1f) u = 0f;
            return Vector2.Lerp(from, to, k) + new Vector2(0f, hopHeight * 4f * u * (1f - u));
        }

        // A fly's side-to-side wobble, fading out as it closes in (remainingFraction 1 → 0).
        public static Vector2 Buzz(float t, float amplitude, float hz, float remainingFraction)
        {
            float a = 2f * Mathf.PI * hz * t;
            return new Vector2(0f, Mathf.Sin(a)) * (amplitude * Mathf.Clamp01(remainingFraction));
        }
    }
}
