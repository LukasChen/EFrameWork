using System;
using System.Collections.Generic;

namespace EFrameWork.Runtime.Event
{
    /// <summary>
    /// 事件标记接口，所有强类型事件 struct 都应实现此接口
    /// </summary>
    public interface IEvent { }

    /// <summary>
    /// 引用传递的事件委托（用于过滤类事件）
    /// </summary>
    public delegate void RefAction<T>(ref T evt) where T : struct, IEvent;

    /// <summary>
    /// 统一事件总线 - 强类型事件系统
    /// </summary>
    /// <remarks>
    /// 使用强类型 struct 事件替代 enum + 泛型参数，实现：
    /// 1. 编译期类型安全
    /// 2. 编辑器可反射事件字段
    /// 3. 任务系统无缝对接
    /// </remarks>
    public static class EventBus
    {
        #region 内部存储

        /// <summary>
        /// 事件类型 -> 委托列表（使用 object 包装 Action&lt;T&gt;）
        /// </summary>
        private static readonly Dictionary<Type, List<Delegate>> s_handlers = new();

        /// <summary>
        /// 全局事件钩子（任务系统/调试用）
        /// </summary>
        public static event Action<Type, object> OnAnyEvent;

        #endregion

        #region 订阅/取消订阅

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型（必须实现 IEvent）</typeparam>
        /// <param name="handler">事件处理器</param>
        public static void Subscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (handler == null) return;

            var type = typeof(T);
            if (!s_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>(4);
                s_handlers[type] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (handler == null) return;

            var type = typeof(T);
            if (s_handlers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    s_handlers.Remove(type);
                }
            }
        }

        /// <summary>
        /// 清除指定事件类型的所有订阅
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        public static void Clear<T>() where T : struct, IEvent
        {
            s_handlers.Remove(typeof(T));
        }

        /// <summary>
        /// 清除所有事件订阅（谨慎使用）
        /// </summary>
        public static void ClearAll()
        {
            s_handlers.Clear();
        }

        #endregion

        #region 派发

        /// <summary>
        /// 派发事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件实例</param>
        public static void Dispatch<T>(T evt) where T : struct, IEvent
        {
            var type = typeof(T);

            // 1. 调用强类型订阅者
            if (s_handlers.TryGetValue(type, out var list))
            {
                // 复制列表以防迭代时修改
                var count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    if (i < list.Count && list[i] is Action<T> action)
                    {
                        try
                        {
                            action.Invoke(evt);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogException(ex);
                        }
                    }
                }
            }

            // 2. 触发全局钩子（供任务系统/调试用）
            try
            {
                OnAnyEvent?.Invoke(type, evt);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 派发事件（引用传递，支持修改事件数据）
        /// 用于过滤类事件，订阅者可以修改事件数据（如设置 IsAllowed = false）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件实例（引用传递）</param>
        public static void DispatchRef<T>(ref T evt) where T : struct, IEvent
        {
            var type = typeof(T);

            if (s_handlers.TryGetValue(type, out var list))
            {
                var count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    if (i < list.Count && list[i] is RefAction<T> refAction)
                    {
                        try
                        {
                            refAction.Invoke(ref evt);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 订阅事件（引用传递版本）
        /// </summary>
        public static void SubscribeRef<T>(RefAction<T> handler) where T : struct, IEvent
        {
            if (handler == null) return;

            var type = typeof(T);
            if (!s_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>(4);
                s_handlers[type] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        /// <summary>
        /// 取消订阅事件（引用传递版本）
        /// </summary>
        public static void UnsubscribeRef<T>(RefAction<T> handler) where T : struct, IEvent
        {
            if (handler == null) return;

            var type = typeof(T);
            if (s_handlers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    s_handlers.Remove(type);
                }
            }
        }

        #endregion

        #region 调试/查询

        /// <summary>
        /// 获取指定事件类型的订阅者数量
        /// </summary>
        public static int GetSubscriberCount<T>() where T : struct, IEvent
        {
            return s_handlers.TryGetValue(typeof(T), out var list) ? list.Count : 0;
        }

        /// <summary>
        /// 获取所有已注册的事件类型
        /// </summary>
        public static IEnumerable<Type> GetRegisteredEventTypes()
        {
            return s_handlers.Keys;
        }

        #endregion
    }
}
