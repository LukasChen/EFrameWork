using System;
using UnityEngine;

namespace EFramework.Runtime.Tween
{
    public struct EFrameTweenOptions
    {
        public EFrameEase Ease;
        public AnimationCurve Curve;
        public bool IgnoreTimeScale;
        public object Target;
        public Action OnUpdate;
        public Action OnComplete;

        public static EFrameTweenOptions Default => new()
        {
            Ease = EFrameEase.Linear
        };
    }
}
