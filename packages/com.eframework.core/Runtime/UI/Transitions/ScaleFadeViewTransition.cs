using Cysharp.Threading.Tasks;
using EFramework.Runtime.Tween;
using UnityEngine;

namespace EFramework.Runtime.UI.Transitions
{
    public sealed class ScaleFadeViewTransition : IUIViewTransition
    {
        public const string Id = "ScaleFade";

        public UniTask PlayOpenAsync(BindingViewBase view)
        {
            var animRoot = view?.Binding?.GetAnimationRoot();
            if (animRoot == null)
            {
                return UniTask.CompletedTask;
            }

            float duration = GetDuration(view, true);

            Kill(view);
            animRoot.localScale = Vector3.one * 0.8f;
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            var scaleTween = EFrameTween.Vector3(
                animRoot.localScale,
                Vector3.one,
                duration,
                value => animRoot.localScale = value,
                new EFrameTweenOptions
                {
                    Ease = EFrameEase.OutBack,
                    Target = animRoot
                });

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                var fadeTween = EFrameTween.Float(
                    canvasGroup.alpha,
                    1f,
                    duration,
                    value => canvasGroup.alpha = value,
                    new EFrameTweenOptions
                    {
                        Ease = EFrameEase.Linear,
                        Target = canvasGroup
                    });
                return UniTask.WhenAll(scaleTween.ToUniTask(), fadeTween.ToUniTask());
            }

            return scaleTween.ToUniTask();
        }

        public UniTask PlayCloseAsync(BindingViewBase view)
        {
            var animRoot = view?.Binding?.GetAnimationRoot();
            if (animRoot == null)
            {
                return UniTask.CompletedTask;
            }

            float duration = GetDuration(view, false);

            Kill(view);
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            var scaleTween = EFrameTween.Vector3(
                animRoot.localScale,
                Vector3.one * 0.8f,
                duration,
                value => animRoot.localScale = value,
                new EFrameTweenOptions
                {
                    Ease = EFrameEase.InBack,
                    Target = animRoot
                });

            if (canvasGroup != null)
            {
                var fadeTween = EFrameTween.Float(
                    canvasGroup.alpha,
                    0f,
                    duration,
                    value => canvasGroup.alpha = value,
                    new EFrameTweenOptions
                    {
                        Ease = EFrameEase.Linear,
                        Target = canvasGroup
                    });
                return UniTask.WhenAll(scaleTween.ToUniTask(), fadeTween.ToUniTask());
            }

            return scaleTween.ToUniTask();
        }

        public void Kill(BindingViewBase view)
        {
            var animRoot = view?.Binding?.GetAnimationRoot();
            if (animRoot == null)
            {
                return;
            }

            EFrameTween.Kill(animRoot);
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                EFrameTween.Kill(canvasGroup);
            }
        }

        private static float GetDuration(BindingViewBase view, bool opening)
        {
            float duration = view?.Config.GetTransitionDuration(opening) ?? ViewTransitionConfig.DefaultDuration;
            return duration > 0f ? duration : ViewTransitionConfig.DefaultDuration;
        }
    }
}
