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

        // Peak height of a cut jump (a hop): it rises at full speed until the button comes up after `holdSeconds` (never
        // before `minJump`), then the rising speed is multiplied by `cutMult`, like PlayerController. Held past the top it
        // is a full jump.
        public static float CutJumpPeak(float jumpHeight, float gravity, float gravityScale, float holdSeconds, float minJump, float cutMult)
        {
            float g = Mathf.Abs(gravity) * gravityScale;
            float v0 = Mathf.Sqrt(2f * jumpHeight * g);
            float t = Mathf.Max(minJump, holdSeconds);
            float v = v0 - g * t;
            if (v <= 0f) return jumpHeight;
            float h = v0 * t - 0.5f * g * t * t;
            v *= cutMult;
            return h + v * v / (2f * g);
        }

        // Seconds from take-off until a cut jump's feet come back down to targetHeight (above the take-off ground);
        // infinity when the hop never gets that high.
        public static float CutJumpAirTime(float jumpHeight, float gravity, float gravityScale, float fallMultiplier,
            float holdSeconds, float minJump, float cutMult, float targetHeight)
        {
            float peak = CutJumpPeak(jumpHeight, gravity, gravityScale, holdSeconds, minJump, cutMult);
            if (targetHeight > peak) return float.PositiveInfinity;
            float g = Mathf.Abs(gravity) * gravityScale;
            float v0 = Mathf.Sqrt(2f * jumpHeight * g);
            float t = Mathf.Max(minJump, holdSeconds);
            float rise = v0 - g * t <= 0f ? v0 / g : t + (v0 - g * t) * cutMult / g;
            float fall = Mathf.Sqrt(2f * (peak - targetHeight) / (g * fallMultiplier));
            return rise + fall;
        }
    }
}
