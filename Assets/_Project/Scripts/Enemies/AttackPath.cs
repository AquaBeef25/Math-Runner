using UnityEngine;

namespace MathRunner.Enemies
{
    // The paths bee and fly take to the runner, pure so they can be tested. All arrive exactly when their time
    // runs out: the base point homes in like the fireball, and any extra (the buzz wobble) is zero at arrival.
    public static class AttackPath
    {
        // Same homing rule as the fireball: cover this step's share of the remaining distance to the (moving) target.
        public static Vector2 Charge(Vector2 position, Vector2 target, float remainingSeconds, float deltaSeconds) =>
            EnemyBullet.Step(position, target, remainingSeconds, deltaSeconds);

        // A fly's side-to-side wobble, fading out as it closes in (remainingFraction 1 → 0).
        public static Vector2 Buzz(float t, float amplitude, float hz, float remainingFraction)
        {
            float a = 2f * Mathf.PI * hz * t;
            return new Vector2(0f, Mathf.Sin(a)) * (amplitude * Mathf.Clamp01(remainingFraction));
        }
    }
}
