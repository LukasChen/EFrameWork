using System;
using UnityEngine;

namespace EFramework.Runtime.Tween
{
    public interface IEFrameTweenProvider
    {
        string Name { get; }
        IEFrameTweenHandle Float(float from, float to, float duration, Action<float> setter, EFrameTweenOptions options);
        IEFrameTweenHandle Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> setter, EFrameTweenOptions options);
        IEFrameTweenHandle Vector3(Vector3 from, Vector3 to, float duration, Action<Vector3> setter, EFrameTweenOptions options);
        IEFrameTweenHandle Color(Color from, Color to, float duration, Action<Color> setter, EFrameTweenOptions options);
        IEFrameTweenHandle Delay(float duration, Action callback, EFrameTweenOptions options);
        void Kill(object target, bool complete = false);
    }
}
