using System;
using System.Collections;
using UnityEngine;

namespace EFrameWork.Runtime
{
    public interface ICoroutineService : IDisposable
    {
        UnityEngine.Coroutine StartCoroutine(IEnumerator coroutine);
        void StopCoroutine(UnityEngine.Coroutine coroutine);
        void StopCoroutine(IEnumerator coroutine);
        void StopAllCoroutines();
        UnityEngine.Coroutine DelayCall(float delayInSeconds, Action action);
        UnityEngine.Coroutine CallNextFrame(Action action);
    }
}
