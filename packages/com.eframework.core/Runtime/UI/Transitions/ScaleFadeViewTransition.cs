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

            Kill(view);
            animRoot.localScale = Vector3.one * 0.8f;
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            Sequence sequence = DOTween.Sequence().SetTarget(animRoot);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                sequence
                    .Join(animRoot.DOScale(1f, duration).SetEase(Ease.OutBack).SetTarget(animRoot))
                    .Join(canvasGroup.DOFade(1f, duration));
            }
            else
            {
                sequence.Join(animRoot.DOScale(1f, duration).SetEase(Ease.OutBack).SetTarget(animRoot));
            }

            return AwaitTweenAsync(sequence);
        }

        public UniTask PlayCloseAsync(BindingViewBase view)
        {
            var animRoot = view?.Binding?.GetAnimationRoot();
            if (animRoot == null)
            {
                return UniTask.CompletedTask;
            }

            float duration = GetDuration(view);

            Kill(view);
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            Sequence sequence = DOTween.Sequence().SetTarget(animRoot);
            if (canvasGroup != null)
            {
                sequence
                    .Join(animRoot.DOScale(0.8f, duration).SetEase(Ease.InBack).SetTarget(animRoot))
                    .Join(canvasGroup.DOFade(0f, duration));
            }
            else
            {
                sequence.Join(animRoot.DOScale(0.8f, duration).SetEase(Ease.InBack).SetTarget(animRoot));
            }

            return AwaitTweenAsync(sequence);
        }

        public void Kill(BindingViewBase view)
        {
            var animRoot = view?.Binding?.GetAnimationRoot();
            if (animRoot == null)
            {
                return;
            }

            animRoot.DOKill(false);
            var canvasGroup = animRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill(false);
            }
        }

        private static UniTask AwaitTweenAsync(Tween tween)
        {
            if (tween == null || !tween.IsActive())
            {
                return UniTask.CompletedTask;
            }

            var completionSource = new UniTaskCompletionSource();

            tween.OnComplete(() => completionSource.TrySetResult());
            tween.OnKill(() => completionSource.TrySetResult());

            return completionSource.Task;
        }

        private static float GetDuration(BindingViewBase view)
        {
            float duration = view?.Config.AnimationDuration ?? DefaultDuration;
            return duration > 0f ? duration : DefaultDuration;
        }
    }
}
