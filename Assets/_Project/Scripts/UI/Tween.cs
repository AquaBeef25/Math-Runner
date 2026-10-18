using System;
using System.Collections;
using MathRunner.Core;
using UnityEngine;

namespace MathRunner.UI
{
    // Coroutine tweens (no package needed). Run on WorldTime.RealDeltaTime: frozen by pause, unaffected by the Focus Zone slow-down.
    // Usage: StartCoroutine(Tween.Run(0.25f, Ease.OutBack, k => rect.localScale = Vector3.one * k));
    public static class Tween
    {
        public static IEnumerator Run(float seconds, Func<float, float> ease, Action<float> apply, Action onDone = null)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                apply(ease(elapsed / seconds));
                yield return null;
                elapsed += WorldTime.RealDeltaTime; // UI keeps its real speed during the Focus Zone; still stops on pause
            }
            apply(ease(1f));
            onDone?.Invoke();
        }

        public static IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float seconds, Func<float, float> ease) =>
            Run(seconds, ease, k => target.localScale = Vector3.LerpUnclamped(from, to, k));
    }
}
