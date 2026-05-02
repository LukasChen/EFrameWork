using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFramework.Runtime
{
    /// <summary>
    /// 静态任务队列
    /// 各系统自主入队，Procedure 负责触发执行
    /// </summary>
    public static class TaskQueue
    {
        #region 内部数据结构

        private class TaskEntry : IComparable<TaskEntry>
        {
            public IQueueableTask Task;
            public int Priority;
            public long EnqueueOrder; // 入队顺序，用于相同优先级时保持 FIFO

            public int CompareTo(TaskEntry other)
            {
                int priorityCompare = Priority.CompareTo(other.Priority);
                if (priorityCompare != 0) return priorityCompare;
                return EnqueueOrder.CompareTo(other.EnqueueOrder);
            }
        }

        #endregion

        #region 字段

        private static readonly List<TaskEntry> s_queue = new();
        private static readonly List<TaskEntry> s_childQueue = new(); // 子任务队列（阻塞模式）
        private static long s_enqueueCounter = 0;
        private static bool s_isRunning = false;
        private static IQueueableTask s_currentTask = null;

        #endregion

        #region 调试事件

        /// <summary>
        /// 任务入队时触发
        /// </summary>
        public static event Action<IQueueableTask> OnTaskEnqueued;

        /// <summary>
        /// 任务开始执行时触发
        /// </summary>
        public static event Action<IQueueableTask> OnTaskStarted;

        /// <summary>
        /// 任务执行完成时触发
        /// </summary>
        public static event Action<IQueueableTask> OnTaskCompleted;

        /// <summary>
        /// 队列开始执行时触发
        /// </summary>
        public static event Action OnQueueStarted;

        /// <summary>
        /// 队列全部执行完成时触发
        /// </summary>
        public static event Action OnQueueFinished;

        #endregion

        #region 属性

        /// <summary>
        /// 队列是否正在执行
        /// </summary>
        public static bool IsRunning => s_isRunning;

        /// <summary>
        /// 当前正在执行的任务
        /// </summary>
        public static IQueueableTask CurrentTask => s_currentTask;

        /// <summary>
        /// 队列中待执行的任务数量
        /// </summary>
        public static int Count => s_queue.Count + s_childQueue.Count;

        #endregion

        #region 入队方法

        /// <summary>
        /// 将任务加入队列
        /// </summary>
        /// <param name="task">任务实例</param>
        /// <param name="priority">优先级（可选，默认使用任务自身优先级）</param>
        public static void Enqueue(IQueueableTask task, int? priority = null)
        {
            if (task == null)
            {
                Debug.LogWarning("[TaskQueue] Attempted to enqueue null task");
                return;
            }

            var entry = new TaskEntry
            {
                Task = task,
                Priority = priority ?? task.Priority,
                EnqueueOrder = s_enqueueCounter++
            };

            s_queue.Add(entry);
            s_queue.Sort();

            Debug.Log($"[TaskQueue] Enqueued: {task.TaskName} (Priority: {entry.Priority}, QueueCount: {s_queue.Count})");
            OnTaskEnqueued?.Invoke(task);
        }

        /// <summary>
        /// 将子任务加入队列（在当前任务执行期间）
        /// </summary>
        /// <param name="task">子任务实例</param>
        /// <param name="blocking">是否阻塞主队列（true: 先执行完子任务再继续主队列）</param>
        /// <param name="priority">优先级</param>
        public static void EnqueueChild(IQueueableTask task, bool blocking = true, int? priority = null)
        {
            if (task == null)
            {
                Debug.LogWarning("[TaskQueue] Attempted to enqueue null child task");
                return;
            }

            if (blocking)
            {
                // 阻塞模式：加入子队列，会在当前任务后、下一个主任务前执行
                var entry = new TaskEntry
                {
                    Task = task,
                    Priority = priority ?? task.Priority,
                    EnqueueOrder = s_enqueueCounter++
                };
                s_childQueue.Add(entry);
                s_childQueue.Sort();

                Debug.Log($"[TaskQueue] Enqueued child (blocking): {task.TaskName}");
            }
            else
            {
                // 非阻塞模式：直接加入主队列，按优先级正常排序
                Enqueue(task, priority);
                Debug.Log($"[TaskQueue] Enqueued child (non-blocking): {task.TaskName}");
            }

            OnTaskEnqueued?.Invoke(task);
        }

        #endregion

        #region 执行方法

        /// <summary>
        /// 开始执行队列中的任务
        /// 会持续执行直到队列为空（包括执行期间新入队的任务）
        /// </summary>
        public static async UniTask RunAsync()
        {
            if (s_isRunning)
            {
                Debug.LogWarning("[TaskQueue] Queue is already running");
                return;
            }

            if (s_queue.Count == 0 && s_childQueue.Count == 0)
            {
                Debug.Log("[TaskQueue] Queue is empty, nothing to run");
                return;
            }

            s_isRunning = true;
            Debug.Log($"[TaskQueue] Queue started, {s_queue.Count} tasks pending");
            OnQueueStarted?.Invoke();

            try
            {
                while (s_queue.Count > 0 || s_childQueue.Count > 0)
                {
                    // 优先执行子队列（阻塞的子任务）
                    while (s_childQueue.Count > 0)
                    {
                        var childEntry = s_childQueue[0];
                        s_childQueue.RemoveAt(0);
                        await ExecuteTaskAsync(childEntry.Task);
                    }

                    // 执行主队列
                    if (s_queue.Count > 0)
                    {
                        var entry = s_queue[0];
                        s_queue.RemoveAt(0);
                        await ExecuteTaskAsync(entry.Task);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TaskQueue] Error during execution: {ex}");
            }
            finally
            {
                s_currentTask = null;
                s_isRunning = false;
                Debug.Log("[TaskQueue] Queue finished");
                OnQueueFinished?.Invoke();
            }
        }

        /// <summary>
        /// 执行单个任务
        /// </summary>
        private static async UniTask ExecuteTaskAsync(IQueueableTask task)
        {
            s_currentTask = task;
            Debug.Log($"[TaskQueue] Executing: {task.TaskName}");
            OnTaskStarted?.Invoke(task);

            try
            {
                await task.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TaskQueue] Task '{task.TaskName}' failed: {ex}");
            }

            Debug.Log($"[TaskQueue] Completed: {task.TaskName}");
            OnTaskCompleted?.Invoke(task);
            s_currentTask = null;
        }

        #endregion

        #region 控制方法

        /// <summary>
        /// 清空队列
        /// </summary>
        public static void Clear()
        {
            int count = s_queue.Count + s_childQueue.Count;
            s_queue.Clear();
            s_childQueue.Clear();
            s_enqueueCounter = 0;

            if (count > 0)
            {
                Debug.Log($"[TaskQueue] Cleared {count} pending tasks");
            }
        }

        public static void Reset()
        {
            Clear();
            s_currentTask = null;
            s_isRunning = false;
            OnTaskEnqueued = null;
            OnTaskStarted = null;
            OnTaskCompleted = null;
            OnQueueStarted = null;
            OnQueueFinished = null;
        }

        /// <summary>
        /// 获取队列状态信息（调试用）
        /// </summary>
        public static string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== TaskQueue Debug Info ===");
            sb.AppendLine($"IsRunning: {s_isRunning}");
            sb.AppendLine($"CurrentTask: {s_currentTask?.TaskName ?? "None"}");
            sb.AppendLine($"MainQueue ({s_queue.Count}):");
            foreach (var entry in s_queue)
            {
                sb.AppendLine($"  - [{entry.Priority}] {entry.Task.TaskName}");
            }
            sb.AppendLine($"ChildQueue ({s_childQueue.Count}):");
            foreach (var entry in s_childQueue)
            {
                sb.AppendLine($"  - [{entry.Priority}] {entry.Task.TaskName}");
            }
            return sb.ToString();
        }

        #endregion
    }
}
