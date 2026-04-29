using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace EFrameWork.Runtime.UI.Transitions
{
    internal sealed class ScaleFadeViewTransition : IUIViewTransition
    {
        private const float DefaultDuration = 0.25f;

        public UniTask PlayOpenAsync(BindingViewBase view)
        {
            var animRoot = view?.Binding?.GetAnimationRoot();
            if (animRoot == null)
            {
                return UniTask.CompletedTask;
            }

            float duration = GetDuration(view);

            animRoot.localScale = Vector3.one * 0.8f;
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                DOTween.Sequence()
                    .Join(animRoot.DOScale(1f, duration).SetEase(Ease.OutBack))
                    .Join(canvasGroup.DOFade(1f, duration));
            }
            else
            {
                animRoot.DOScale(1f, duration).SetEase(Ease.OutBack);
            }

            return UniTask.Delay(TimeSpan.FromSeconds(duration));
        }

        public UniTask PlayCloseAsync(BindingViewBase view)
        {
            var animRoot = view?.Binding?.GetAnimationRoot();
            if (animRoot == null)
            {
                return UniTask.CompletedTask;
            }

            float duration = GetDuration(view);

            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                DOTween.Sequence()
                    .Join(animRoot.DOScale(0.8f, duration).SetEase(Ease.InBack))
                    .Join(canvasGroup.DOFade(0f, duration));
            }
            else
            {
                animRoot.DOScale(0.8f, duration).SetEase(Ease.InBack);
            }

            return UniTask.Delay(TimeSpan.FromSeconds(duration));
        }

        public void Kill(BindingViewBase view)
        {
            var animRoot = view?.Binding?.GetAnimationRoot();
            if (animRoot == null)
            {
                return;
            }

            animRoot.DOKill(true);
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill(true);
            }
        }

        private static float GetDuration(BindingViewBase view)
        {
            float duration = view?.Config.AnimationDuration ?? DefaultDuration;
            return duration > 0f ? duration : DefaultDuration;
        }
    }
}