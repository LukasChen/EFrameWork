using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EFramework.Runtime.Tween;
using UnityEngine;
using DotweenTween = DG.Tweening.Tween;

namespace EFramework.Runtime.Tween.Dotween
{
    internal sealed class DotweenTweenProvider : IEFrameTweenProvider
    {
        public string Name => "DOTween";

        public IEFrameTweenHandle Float(float from, float to, float duration, Action<float> setter, EFrameTweenOptions options)
        {
            if (setter == null)
            {
                return DotweenTweenHandle.Completed;
            }

            var tween = DOTween.To(
                () => 0f,
                progress => setter(Mathf.LerpUnclamped(from, to, EvaluateProgress(options, progress))),
                1f,
                duration);
            return Configure(tween, options);
        }

        public IEFrameTweenHandle Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> setter, EFrameTweenOptions options)
        {
            if (setter == null)
            {
                return DotweenTweenHandle.Completed;
            }

            var tween = DOTween.To(
                () => 0f,
                progress => setter(UnityEngine.Vector2.LerpUnclamped(from, to, EvaluateProgress(options, progress))),
                1f,
                duration);
            return Configure(tween, options);
        }

        public IEFrameTweenHandle Vector3(Vector3 from, Vector3 to, float duration, Action<Vector3> setter, EFrameTweenOptions options)
        {
            if (setter == null)
            {
                return DotweenTweenHandle.Completed;
            }

            var tween = DOTween.To(
                () => 0f,
                progress => setter(UnityEngine.Vector3.LerpUnclamped(from, to, EvaluateProgress(options, progress))),
                1f,
                duration);
            return Configure(tween, options);
        }

        public IEFrameTweenHandle Color(Color from, Color to, float duration, Action<Color> setter, EFrameTweenOptions options)
        {
            if (setter == null)
            {
                return DotweenTweenHandle.Completed;
            }

            var tween = DOTween.To(
                () => 0f,
                progress => setter(UnityEngine.Color.LerpUnclamped(from, to, EvaluateProgress(options, progress))),
                1f,
                duration);
            return Configure(tween, options);
        }

        public IEFrameTweenHandle Delay(float duration, Action callback, EFrameTweenOptions options)
        {
            var tween = DOVirtual.DelayedCall(duration, () => callback?.Invoke(), options.IgnoreTimeScale);
            return Configure(tween, options);
        }

        public void Kill(object target, bool complete = false)
        {
            if (target != null)
            {
                DOTween.Kill(target, complete);
            }
        }

        private static IEFrameTweenHandle Configure(DotweenTween tween, EFrameTweenOptions options)
        {
            if (tween == null)
            {
                return DotweenTweenHandle.Completed;
            }

            tween.SetEase(options.Curve != null ? Ease.Linear : ToDotweenEase(options.Ease));

            if (options.Target != null)
            {
                tween.SetTarget(options.Target);
            }

            if (options.IgnoreTimeScale)
            {
                tween.SetUpdate(true);
            }

            if (options.OnUpdate != null)
            {
                tween.OnUpdate(() => options.OnUpdate.Invoke());
            }

            return new DotweenTweenHandle(tween).OnComplete(options.OnComplete);
        }

        private static float EvaluateProgress(EFrameTweenOptions options, float progress)
        {
            return options.Curve != null ? options.Curve.Evaluate(progress) : progress;
        }

        private static Ease ToDotweenEase(EFrameEase ease)
        {
            return ease == EFrameEase.Unset ? Ease.Linear : (Ease)ease;
        }
    }

    internal sealed class DotweenTweenHandle : IEFrameTweenHandle
    {
        public static readonly IEFrameTweenHandle Completed = new CompletedDotweenTweenHandle();

        private readonly DotweenTween m_tween;
        private UniTaskCompletionSource m_completionSource;
        private Action m_onComplete;
        private bool m_completedNaturally;

        public DotweenTweenHandle(DotweenTween tween)
        {
            m_tween = tween;
            if (m_tween != null)
            {
                m_tween.OnComplete(Complete);
                m_tween.OnKill(ResolveAwaiters);
            }
        }

        public bool IsActive => m_tween != null && m_tween.IsActive();

        public IEFrameTweenHandle OnComplete(Action callback)
        {
            if (callback == null)
            {
                return this;
            }

            if (m_completedNaturally)
            {
                callback.Invoke();
                return this;
            }

            m_onComplete += callback;
            return this;
        }

        public UniTask ToUniTask()
        {
            if (!IsActive)
            {
                return UniTask.CompletedTask;
            }

            m_completionSource ??= new UniTaskCompletionSource();
            return m_completionSource.Task;
        }

        public void Kill(bool complete = false)
        {
            if (IsActive)
            {
                KillTween(m_tween, complete);
            }
        }

        private void Complete()
        {
            if (m_completedNaturally)
            {
                return;
            }

            m_completedNaturally = true;
            m_onComplete?.Invoke();
            ResolveAwaiters();
        }

        private void ResolveAwaiters()
        {
            m_completionSource?.TrySetResult();
        }

        private static void KillTween(DotweenTween tween, bool complete)
        {
            if (tween == null)
            {
                return;
            }

            tween.Kill(complete);
        }
    }

    internal sealed class CompletedDotweenTweenHandle : IEFrameTweenHandle
    {
        public bool IsActive => false;

        public IEFrameTweenHandle OnComplete(Action callback)
        {
            callback?.Invoke();
            return this;
        }

        public UniTask ToUniTask()
        {
            return UniTask.CompletedTask;
        }

        public void Kill(bool complete = false)
        {
        }
    }

    internal static class DotweenTweenAutoRegister
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterProvider()
        {
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(500, 50);
            EFrameTween.RegisterProvider(new DotweenTweenProvider(), 100);
        }
    }
}
