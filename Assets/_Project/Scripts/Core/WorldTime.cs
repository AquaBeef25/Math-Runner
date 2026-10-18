using UnityEngine;

namespace MathRunner.Core
{
    // The world's speed has two parts: pause (0 or 1) and the Focus Zone slow-down. GameManager is the only writer.
    public static class WorldTime
    {
        // 1 normally, e.g. 0.2 during a Focus Zone.
        public static float FocusFactor { get; internal set; } = 1f;

        public static float Combine(bool paused, float focusFactor) => paused ? 0f : focusFactor;

        // Seconds of the player's own (unslowed) time this frame, for countdowns that must not stretch in slow-mo.
        // 0 while paused, because Time.deltaTime is.
        public static float RealDeltaTime => FocusFactor > 0f ? Time.deltaTime / FocusFactor : 0f;
    }
}
