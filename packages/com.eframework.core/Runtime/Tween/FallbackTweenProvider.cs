using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EFramework.Runtime.Tween
{
    internal sealed class FallbackTweenProvider : IEFrameTweenProvider
    {
        public string Name => "Fallback";

        public IEFrameTweenHandle Float(float from, float to, float duration, Action<float> setter, EFrameTweenOptions options)
        {
            return CreateTween(duration, setter, progress => Mathf.LerpUnclamped(from, to, progress), options);
        }

        public IEFrameTweenHandle Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> setter, EFrameTweenOptions options)
        {
            return CreateTween(duration, setter, progress => UnityEngine.Vector2.LerpUnclamped(from, to, progress), options);
        }

        public IEFrameTweenHandle Vector3(Vector3 from, Vector3 to, float duration, Action<Vector3> setter, EFrameTweenOptions options)
        {
            return CreateTween(duration, setter, progress => UnityEngine.Vector3.LerpUnclamped(from, to, progress), options);
        }

        public IEFrameTweenHandle Color(Color from, Color to, float duration, Action<Color> setter, EFrameTweenOptions options)
        {
            return CreateTween(duration, setter, progress => UnityEngine.Color.LerpUnclamped(from, to, progress), options);
        }

        public IEFrameTweenHandle Delay(float duration, Action callback, EFrameTweenOptions options)
        {
            var handle = new FallbackTweenHandle(duration, options, _ => { });
            handle.OnComplete(callback);
            handle.OnComplete(options.OnComplete);
            FallbackTweenRunner.Add(handle);
            return handle;
        }

        public void Kill(object target, bool complete = false)
        {
            FallbackTweenRunner.Kill(target, complete);
        }

        private static IEFrameTweenHandle CreateTween<T>(
            float duration,
            Action<T> setter,
            Func<float, T> evaluator,
            EFrameTweenOptions options)
        {
            if (setter == null)
            {
                return FallbackTweenHandle.Completed;
            }

            var handle = new FallbackTweenHandle(duration, options, progress =>
            {
                setter(evaluator(progress));
                options.OnUpdate?.Invoke();
            });
            handle.OnComplete(options.OnComplete);
            FallbackTweenRunner.Add(handle);
            return handle;
        }
    }

    internal sealed class FallbackTweenHandle : IEFrameTweenHandle
    {
        public static readonly IEFrameTweenHandle Completed = new CompletedTweenHandle();

        private readonly UniTaskCompletionSource m_completionSource = new();
        private readonly Action<float> m_applyProgress;
        private readonly EFrameTweenOptions m_options;
        private readonly float m_duration;
        private Action m_onComplete;
        private float m_elapsed;
        private bool m_completed;

        public FallbackTweenHandle(float duration, EFrameTweenOptions options, Action<float> applyProgress)
        {
            m_duration = Mathf.Max(0f, duration);
            m_options = options;
            m_applyProgress = applyProgress;
            IsActive = true;

            if (m_duration <= 0f)
            {
                Apply(1f);
                Complete();
            }
        }

        public object Target => m_options.Target;
        public bool IsActive { get; private set; }

        public IEFrameTweenHandle OnComplete(Action callback)
        {
            if (callback == null)
            {
                return this;
            }

            if (m_completed)
            {
                callback.Invoke();
                return this;
            }

            m_onComplete += callback;
            return this;
        }

        public UniTask ToUniTask()
        {
            return m_completionSource.Task;
        }

        public void Kill(bool complete = false)
        {
            if (!IsActive && !complete)
            {
                return;
            }

            if (complete && !m_completed)
            {
                Apply(1f);
                Complete();
                return;
            }

            IsActive = false;
            m_completed = true;
            m_completionSource.TrySetResult();
        }

        internal void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (!IsActive || m_completed)
            {
                return;
            }

            m_elapsed += m_options.IgnoreTimeScale ? unscaledDeltaTime : deltaTime;
            var progress = m_duration <= 0f ? 1f : Mathf.Clamp01(m_elapsed / m_duration);
            Apply(progress);

            if (progress >= 1f)
            {
                Complete();
            }
        }

        private void Apply(float progress)
        {
            var easedProgress = m_options.Curve != null
                ? m_options.Curve.Evaluate(progress)
                : EFrameEaseUtility.Evaluate(m_options.Ease, progress);
            m_applyProgress?.Invoke(easedProgress);
        }

        private void Complete()
        {
            if (m_completed)
            {
                return;
            }

            IsActive = false;
            m_completed = true;
            m_onComplete?.Invoke();
            m_completionSource.TrySetResult();
        }
    }

    internal sealed class CompletedTweenHandle : IEFrameTweenHandle
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

    internal sealed class FallbackTweenRunner : MonoBehaviour
    {
        private static FallbackTweenRunner s_instance;
        private readonly List<FallbackTweenHandle> m_handles = new(32);

        public static void Add(FallbackTweenHandle handle)
        {
            if (handle == null || !handle.IsActive)
            {
                return;
            }

            EnsureInstance().m_handles.Add(handle);
        }

        public static void Kill(object target, bool complete)
        {
            if (target == null || s_instance == null)
            {
                return;
            }

            for (var index = s_instance.m_handles.Count - 1; index >= 0; index--)
            {
                var handle = s_instance.m_handles[index];
                if (ReferenceEquals(handle.Target, target))
                {
                    handle.Kill(complete);
                    s_instance.m_handles.RemoveAt(index);
                }
            }
        }

        private static FallbackTweenRunner EnsureInstance()
        {
            if (s_instance != null)
            {
                return s_instance;
            }

            var runnerObject = new GameObject("[EFrameTweenRunner]");
            DontDestroyOnLoad(runnerObject);
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            s_instance = runnerObject.AddComponent<FallbackTweenRunner>();
            return s_instance;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var unscaledDeltaTime = Time.unscaledDeltaTime;
            for (var index = m_handles.Count - 1; index >= 0; index--)
            {
                var handle = m_handles[index];
                handle.Tick(deltaTime, unscaledDeltaTime);
                if (!handle.IsActive)
                {
                    m_handles.RemoveAt(index);
                }
            }
        }
    }
}
