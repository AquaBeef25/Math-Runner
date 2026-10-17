using UnityEngine;

namespace MathRunner.Player
{
    public static class JumpMath
    {
        // Seconds from take-off until the feet come back down to targetHeight (relative to the take-off ground)
        // on a full-height jump. Rising uses normal gravity, falling uses gravity × fallMultiplier, like PlayerController.
        public static float AirTimeToLand(float jumpHeight, float gravity, float gravityScale, float fallMultiplier, float targetHeight)
        {
            float g = Mathf.Abs(gravity) * gravityScale;
            float riseTime = Mathf.Sqrt(2f * jumpHeight / g);
            float drop = Mathf.Max(0f, jumpHeight - targetHeight);
            float fallTime = Mathf.Sqrt(2f * drop / (g * fallMultiplier));
            return riseTime + fallTime;
        }
    }
}
