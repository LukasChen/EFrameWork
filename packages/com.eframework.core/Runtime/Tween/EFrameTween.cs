using System;
using UnityEngine;

namespace EFramework.Runtime.Tween
{
    public static class EFrameTween
    {
        private static readonly IEFrameTweenProvider s_fallbackProvider = new FallbackTweenProvider();
        private static IEFrameTweenProvider s_provider = s_fallbackProvider;
        private static int s_providerPriority;

        public static string ProviderName => s_provider.Name;
        public static bool IsFallbackProvider => ReferenceEquals(s_provider, s_fallbackProvider);

        public static void RegisterProvider(IEFrameTweenProvider provider, int priority)
        {
            if (provider == null || priority < s_providerPriority)
            {
                return;
            }

            s_provider = provider;
            s_providerPriority = priority;
        }

        public static void ResetProvider()
        {
            s_provider = s_fallbackProvider;
            s_providerPriority = 0;
        }

        public static IEFrameTweenHandle Float(float from, float to, float duration, Action<float> setter, EFrameTweenOptions options = default)
        {
            return s_provider.Float(from, to, duration, setter, NormalizeOptions(options));
        }

        public static IEFrameTweenHandle Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> setter, EFrameTweenOptions options = default)
        {
            return s_provider.Vector2(from, to, duration, setter, NormalizeOptions(options));
        }

        public static IEFrameTweenHandle Vector3(Vector3 from, Vector3 to, float duration, Action<Vector3> setter, EFrameTweenOptions options = default)
        {
            return s_provider.Vector3(from, to, duration, setter, NormalizeOptions(options));
        }

        public static IEFrameTweenHandle Color(Color from, Color to, float duration, Action<Color> setter, EFrameTweenOptions options = default)
        {
            return s_provider.Color(from, to, duration, setter, NormalizeOptions(options));
        }

        public static IEFrameTweenHandle Delay(float duration, Action callback, EFrameTweenOptions options = default)
        {
            return s_provider.Delay(duration, callback, NormalizeOptions(options));
        }

        public static void Kill(object target, bool complete = false)
        {
            if (target == null)
            {
                return;
            }

            s_provider.Kill(target, complete);
        }

        private static EFrameTweenOptions NormalizeOptions(EFrameTweenOptions options)
        {
            if (options.Curve == null && options.Ease == default)
            {
                options.Ease = EFrameEase.Linear;
            }

            return options;
        }
    }
}
