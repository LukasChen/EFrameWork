using UnityEngine;

namespace EFramework.Runtime.Tween
{
    // Keep names and numeric order aligned with DG.Tweening.Ease for direct DOTween adapter mapping.
    public enum EFrameEase
    {
        Unset,
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InElastic,
        OutElastic,
        InOutElastic,
        InBack,
        OutBack,
        InOutBack,
        InBounce,
        OutBounce,
        InOutBounce,
        Flash,
        InFlash,
        OutFlash,
        InOutFlash,
        INTERNAL_Zero,
        INTERNAL_Custom
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
                EFrameEase.InSine => 1f - Mathf.Cos(t * Mathf.PI * 0.5f),
                EFrameEase.OutSine => Mathf.Sin(t * Mathf.PI * 0.5f),
                EFrameEase.InOutSine => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f,
                EFrameEase.InCubic => t * t * t,
                EFrameEase.OutCubic => 1f - Mathf.Pow(1f - t, 3f),
                EFrameEase.InOutCubic => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f,
                EFrameEase.InQuart => t * t * t * t,
                EFrameEase.OutQuart => 1f - Mathf.Pow(1f - t, 4f),
                EFrameEase.InOutQuart => t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f,
                EFrameEase.InQuint => t * t * t * t * t,
                EFrameEase.OutQuint => 1f - Mathf.Pow(1f - t, 5f),
                EFrameEase.InOutQuint => t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) * 0.5f,
                EFrameEase.InExpo => t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f),
                EFrameEase.OutExpo => t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t),
                EFrameEase.InOutExpo => EvaluateInOutExpo(t),
                EFrameEase.InCirc => 1f - Mathf.Sqrt(1f - t * t),
                EFrameEase.OutCirc => Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f)),
                EFrameEase.InOutCirc => t < 0.5f
                    ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) * 0.5f
                    : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) * 0.5f,
                EFrameEase.InBack => EvaluateInBack(t),
                EFrameEase.OutBack => EvaluateOutBack(t),
                EFrameEase.InOutBack => EvaluateInOutBack(t),
                EFrameEase.InElastic => EvaluateInElastic(t),
                EFrameEase.OutElastic => EvaluateOutElastic(t),
                EFrameEase.InOutElastic => EvaluateInOutElastic(t),
                EFrameEase.InBounce => 1f - EvaluateOutBounce(1f - t),
                EFrameEase.OutBounce => EvaluateOutBounce(t),
                EFrameEase.InOutBounce => t < 0.5f
                    ? (1f - EvaluateOutBounce(1f - 2f * t)) * 0.5f
                    : (1f + EvaluateOutBounce(2f * t - 1f)) * 0.5f,
                EFrameEase.INTERNAL_Zero => 0f,
                _ => t
            };
        }

        private static float EvaluateInOutExpo(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
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

        private static float EvaluateInOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            return t < 0.5f
                ? Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2) * 0.5f
                : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (2f * t - 2f) + c2) + 2f) * 0.5f;
        }

        private static float EvaluateInElastic(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            const float c4 = 2f * Mathf.PI / 3f;
            return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
        }

        private static float EvaluateOutElastic(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        private static float EvaluateInOutElastic(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            const float c5 = 2f * Mathf.PI / 4.5f;
            return t < 0.5f
                ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) * 0.5f
                : Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5) * 0.5f + 1f;
        }

        private static float EvaluateOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }

            if (t < 2f / d1)
            {
                var p = t - 1.5f / d1;
                return n1 * p * p + 0.75f;
            }

            if (t < 2.5f / d1)
            {
                var p = t - 2.25f / d1;
                return n1 * p * p + 0.9375f;
            }

            var last = t - 2.625f / d1;
            return n1 * last * last + 0.984375f;
        }
    }
}
