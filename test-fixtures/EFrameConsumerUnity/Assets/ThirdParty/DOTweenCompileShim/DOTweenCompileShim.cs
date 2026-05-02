using System;
using UnityEngine;

namespace DG.Tweening
{
    public delegate void TweenCallback();

    public enum Ease
    {
        Linear,
        InBack,
        OutBack,
        OutCubic,
        OutQuad
    }

    public enum LogBehaviour
    {
        ErrorsOnly
    }

    public class Tween
    {
        public bool IsActive()
        {
            return false;
        }

        public Tween From(float fromValue)
        {
            return this;
        }

        public Tween OnComplete(TweenCallback action)
        {
            return this;
        }

        public Tween OnKill(TweenCallback action)
        {
            return this;
        }

        public Tween OnUpdate(TweenCallback action)
        {
            return this;
        }

        public Tween SetEase(Ease ease)
        {
            return this;
        }

        public Tween SetEase(Ease ease, float overshoot)
        {
            return this;
        }

        public Tween SetLink(GameObject target)
        {
            return this;
        }

        public Tween SetTarget(object target)
        {
            return this;
        }

        public Tween SetUpdate(bool isIndependentUpdate)
        {
            return this;
        }
    }

    public sealed class Sequence : Tween
    {
        public Sequence Append(Tween tween)
        {
            return this;
        }

        public Sequence Join(Tween tween)
        {
            return this;
        }

        public new Sequence SetTarget(object target)
        {
            return this;
        }
    }

    public static class DOTween
    {
        public static void Init(bool recycleAllByDefault = false, bool useSafeMode = true, LogBehaviour logBehaviour = LogBehaviour.ErrorsOnly)
        {
        }

        public static void Kill(object target)
        {
        }

        public static Sequence Sequence()
        {
            return new Sequence();
        }

        public static void SetTweensCapacity(int tweenersCapacity, int sequencesCapacity)
        {
        }

        public static Tween To(Func<float> getter, Action<float> setter, float endValue, float duration)
        {
            setter?.Invoke(endValue);
            return new Tween();
        }

        public static Tween To<T>(Func<T> getter, Action<T> setter, T endValue, float duration)
        {
            setter?.Invoke(endValue);
            return new Tween();
        }
    }

    public static class DOVirtual
    {
        public static Tween DelayedCall(float delay, TweenCallback callback)
        {
            return new Tween();
        }
    }

    public static class DOTweenCompileShimExtensions
    {
        public static void DOKill(this Component target, bool complete = false)
        {
        }

        public static Tween DOAnchorPos(this RectTransform target, Vector2 endValue, float duration)
        {
            return new Tween();
        }

        public static Tween DOAnchorPosX(this RectTransform target, float endValue, float duration)
        {
            return new Tween();
        }

        public static Tween DOFade(this Component target, float endValue, float duration)
        {
            return new Tween();
        }

        public static Tween DOScale(this Transform target, float endValue, float duration)
        {
            return new Tween();
        }

        public static Tween DOScale(this Transform target, Vector3 endValue, float duration)
        {
            return new Tween();
        }

        public static Tween DOShakePosition(this Component target, float duration, Vector3 strength, int vibrato, float randomness)
        {
            return new Tween();
        }

        public static Tween DOShakePosition(this Component target, float duration, float strength, int vibrato, float randomness)
        {
            return new Tween();
        }
    }
}
