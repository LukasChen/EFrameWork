using UnityEngine;

namespace EFramework.Runtime.Tween
{
    public enum EFrameEase
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        InBack,
        OutBack
    }

    public static class EFrameEaseUtility
    {
        public static float Evaluate(EFrameEase ease, float t)
        {
            t = Mathf.Clamp01(t);
            return ease switch
            {
                EFrameEase.InQuad => t * t,
                EFrameEase.OutQuad => 1f - (1f - t) * (1f - t),
                EFrameEase.InOutQuad => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f,
                EFrameEase.InBack => EvaluateInBack(t),
                EFrameEase.OutBack => EvaluateOutBack(t),
                _ => t
            };
        }

        private static float EvaluateInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        private static float EvaluateOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            var p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }
    }
}
