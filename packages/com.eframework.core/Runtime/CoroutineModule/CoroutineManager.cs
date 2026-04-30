using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EFrameWork.Runtime
{
    /// <summary>
    /// 全局协程管理器
    /// </summary>
    public class CoroutineManager : ICoroutineService
    {
        private MonoBehaviour m_coroutineRunner;
        private bool m_disposed;

        // WaitForSeconds 对象池，避免频繁 new 对象
        private Dictionary<float, WaitForSeconds> m_waitForSecondsCache = new Dictionary<float, WaitForSeconds>();

        // 常用的等待时间，预先缓存
        private static readonly float[] s_commonWaitTimes = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 1.2f, 1.5f, 2.0f, 2.5f, 3.0f, 5.0f };

        // 缓存大小限制，避免缓存过多不常用的时间
        private const int k_maxCacheSize = 50;

        public CoroutineManager(MonoBehaviour coroutineRunner)
        {
            m_coroutineRunner = coroutineRunner;
            InitializeCommonWaitTimes();
        }

        /// <summary>
        /// 初始化常用等待时间的缓存
        /// </summary>
        private void InitializeCommonWaitTimes()
        {
            foreach (var waitTime in s_commonWaitTimes)
            {
                m_waitForSecondsCache[waitTime] = new WaitForSeconds(waitTime);
            }
        }

        /// <summary>
        /// 启动一个协程
        /// </summary>
        /// <param name="coroutine">要启动的协程</param>
        /// <returns>返回协程对象，可用于停止协程</returns>
        public UnityEngine.Coroutine StartCoroutine(IEnumerator coroutine)
        {
            if (coroutine == null)
            {
                Debug.LogError("CoroutineManager cannot start a null coroutine.");
                return null;
            }

            if (!CanUseRunner("start coroutine"))
            {
                return null;
            }

            return m_coroutineRunner.StartCoroutine(coroutine);
        }

        /// <summary>
        /// 停止一个协程
        /// </summary>
        /// <param name="coroutine">要停止的协程对象</param>
        public void StopCoroutine(UnityEngine.Coroutine coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            if (!CanUseRunner("stop coroutine"))
            {
                return;
            }

            m_coroutineRunner.StopCoroutine(coroutine);
        }

        /// <summary>
        /// 停止一个协程
        /// </summary>
        /// <param name="coroutine">要停止的协程IEnumerator</param>
        public void StopCoroutine(IEnumerator coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            if (!CanUseRunner("stop coroutine"))
            {
                return;
            }

            m_coroutineRunner.StopCoroutine(coroutine);
        }

        /// <summary>
        /// 停止所有协程
        /// </summary>
        public void StopAllCoroutines()
        {
            if (m_disposed)
            {
                return;
            }

            if (!CanUseRunner("stop all coroutines"))
            {
                return;
            }

            m_coroutineRunner.StopAllCoroutines();
        }

        /// <summary>
        /// 延迟调用
        /// </summary>
        /// <param name="delayInSeconds">延迟时间（秒）</param>
        /// <param name="action">要执行的回调</param>
        /// <returns>协程对象</returns>
        public UnityEngine.Coroutine DelayCall(float delayInSeconds, Action action)
        {
            return StartCoroutine(DelayCoroutine(delayInSeconds, action));
        }

        /// <summary>
        /// 等待指定帧数
        /// </summary>
        /// <param name="frameCount">要等待的帧数</param>
        /// <returns>协程对象</returns>
        public UnityEngine.Coroutine WaitForFrames(int frameCount)
        {
            return StartCoroutine(WaitForFramesCoroutine(Mathf.Max(0, frameCount)));
        }

        /// <summary>
        /// 等待直到条件满足
        /// </summary>
        /// <param name="condition">条件函数</param>
        /// <returns>协程对象</returns>
        public UnityEngine.Coroutine WaitUntil(Func<bool> condition)
        {
            if (condition == null)
            {
                Debug.LogError("CoroutineManager.WaitUntil requires a non-null condition.");
                return null;
            }

            return StartCoroutine(WaitUntilCoroutine(condition));
        }

        /// <summary>
        /// 等待直到条件不满足
        /// </summary>
        /// <param name="condition">条件函数</param>
        /// <returns>协程对象</returns>
        public UnityEngine.Coroutine WaitWhile(Func<bool> condition)
        {
            if (condition == null)
            {
                Debug.LogError("CoroutineManager.WaitWhile requires a non-null condition.");
                return null;
            }

            return StartCoroutine(WaitWhileCoroutine(condition));
        }

        /// <summary>
        /// 等待指定时间（秒）
        /// </summary>
        /// <param name="seconds">等待时间（秒）</param>
        /// <returns>协程对象</returns>
        public UnityEngine.Coroutine WaitForSeconds(float seconds)
        {
            return StartCoroutine(WaitForSecondsCoroutine(seconds));
        }

        /// <summary>
        /// 下一帧执行回调
        /// </summary>
        /// <param name="action">要执行的回调</param>
        /// <returns>协程对象</returns>
        public UnityEngine.Coroutine CallNextFrame(Action action)
        {
            return StartCoroutine(CallNextFrameCoroutine(action));
        }

        /// <summary>
        /// 获取缓存的 WaitForSeconds 对象，避免频繁创建
        /// </summary>
        /// <param name="seconds">等待时间（秒）</param>
        /// <returns>WaitForSeconds 对象</returns>
        public WaitForSeconds GetWaitForSeconds(float seconds)
        {
            seconds = NormalizeWaitSeconds(seconds);

            // 先检查缓存中是否存在
            if (m_waitForSecondsCache.TryGetValue(seconds, out WaitForSeconds waitForSeconds))
            {
                return waitForSeconds;
            }

            // 如果缓存已满，不再缓存新的时间（避免内存泄漏）
            if (m_waitForSecondsCache.Count >= k_maxCacheSize)
            {
                return new WaitForSeconds(seconds);
            }

            // 创建新的并加入缓存
            waitForSeconds = new WaitForSeconds(seconds);
            m_waitForSecondsCache[seconds] = waitForSeconds;
            return waitForSeconds;
        }

        /// <summary>
        /// 延迟调用的协程实现
        /// </summary>
        private IEnumerator DelayCoroutine(float delay, Action action)
        {
            yield return GetWaitForSeconds(delay);
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 等待指定帧数的协程实现
        /// </summary>
        private IEnumerator WaitForFramesCoroutine(int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 等待直到条件满足的协程实现
        /// </summary>
        private IEnumerator WaitUntilCoroutine(Func<bool> condition)
        {
            while (!condition())
            {
                yield return null;
            }
        }

        /// <summary>
        /// 等待直到条件不满足的协程实现
        /// </summary>
        private IEnumerator WaitWhileCoroutine(Func<bool> condition)
        {
            while (condition())
            {
                yield return null;
            }
        }

        /// <summary>
        /// 等待指定时间的协程实现
        /// </summary>
        private IEnumerator WaitForSecondsCoroutine(float seconds)
        {
            yield return GetWaitForSeconds(seconds);
        }

        /// <summary>
        /// 下一帧执行的协程实现
        /// </summary>
        private IEnumerator CallNextFrameCoroutine(Action action)
        {
            // 等待到下一帧
            yield return null;
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;

            if (m_coroutineRunner != null)
            {
                m_coroutineRunner.StopAllCoroutines();
                m_coroutineRunner = null;
            }

            // 清理缓存
            m_waitForSecondsCache?.Clear();
        }

        private bool CanUseRunner(string operation)
        {
            if (m_disposed)
            {
                Debug.LogWarning($"CoroutineManager has been disposed. Cannot {operation}.");
                return false;
            }

            if (m_coroutineRunner == null)
            {
                Debug.LogError($"CoroutineManager is not initialized. Cannot {operation}.");
                return false;
            }

            return true;
        }

        private static float NormalizeWaitSeconds(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                Debug.LogWarning($"CoroutineManager received an invalid wait duration '{seconds}'. Using 0 seconds instead.");
                return 0f;
            }

            return Mathf.Max(0f, seconds);
        }
    }
}
