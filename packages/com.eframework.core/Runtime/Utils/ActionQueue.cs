using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFramework.Runtime.Utils
{
    public sealed class ActionQueue
    {
        private static Queue<Func<UniTask>> m_actionQueueAsync = new();
        public static bool IsRunning { get; private set; }

        public static void AddActionAsync(Func<UniTask> actionTask)
        {
            m_actionQueueAsync.Enqueue(actionTask);
        }

        public static async UniTask Run()
        {
            if (IsRunning)
            {
                Debug.LogWarning("Run failed: ActionQueue is already running.");
                return;
            }

            IsRunning = true;
            while (m_actionQueueAsync.Count > 0)
            {
                var actionTask = m_actionQueueAsync.Dequeue();
                try
                {
                    await actionTask.Invoke();
                    //  UnityEngine.Debug.Log(" action !");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Action failed: {ex.Message}");
                    break;
                }
            }

            IsRunning = false;
            // UnityEngine.Debug.Log("All actions completed");
        }

        public static void Clear()
        {
            m_actionQueueAsync.Clear();
        }
    }
}
