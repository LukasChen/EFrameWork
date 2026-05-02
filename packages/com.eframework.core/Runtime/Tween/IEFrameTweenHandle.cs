using System;
using Cysharp.Threading.Tasks;

namespace EFramework.Runtime.Tween
{
    public interface IEFrameTweenHandle
    {
        bool IsActive { get; }
        IEFrameTweenHandle OnComplete(Action callback);
        UniTask ToUniTask();
        void Kill(bool complete = false);
    }
}
