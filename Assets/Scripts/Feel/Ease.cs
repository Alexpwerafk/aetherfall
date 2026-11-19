using System;
using System.Collections;
using UnityEngine;

namespace Aetherfall.Feel
{
    /// <summary>
    /// Standard easing curves and unscaled-safe coroutine tween helper.
    /// </summary>
    public static class Ease
    {
        public static float OutCubic(float t)
        {
            float p = 1f - Mathf.Clamp01(t);
            return 1f - p * p * p;
        }

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        public static float OutElastic(float t)
        {
            float x = Mathf.Clamp01(t);
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;
            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f;
        }

        public static float InOutSine(float t)
        {
            return -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(t)) - 1f) * 0.5f;
        }

        public static IEnumerator Tween(
            float duration,
            Action<float> apply,
            Func<float, float> ease,
            bool unscaled = true)
        {
            if (duration <= 0.0001f)
            {
                apply?.Invoke(1f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                apply?.Invoke(ease(u));
                yield return null;
            }
            apply?.Invoke(1f);
        }
    }
}
