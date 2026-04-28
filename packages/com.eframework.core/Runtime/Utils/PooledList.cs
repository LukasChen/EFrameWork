using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace EFrameWork.Runtime.Utils
{
    /// <summary>
    ///     PooledList\
    ///     <T\>
    ///         是一个可复用的临时 List 池，减少频繁分配和回收带来的 GC 压力。
    ///         用法示例：
    ///         <code>
    /// using EFrameCore.Utils;
    /// 
    /// void Example()
    /// {
    ///     // 从池中获取一个临时列表，作用域结束后自动归还池中
    ///     using (var list = PooledList&lt;int&gt;.Get())
    ///     {
    ///         list.Add(1);
    ///         list.Add(2);
    ///         foreach (var item in list)
    ///         {
    ///             UnityEngine.Debug.Log(item);
    ///         }
    ///     } // 这里自动 Dispose
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class PooledList<T> : List<T>, IDisposable
    {
        // 静态池，存放未被使用的 PooledList 实例
        private static readonly Stack<PooledList<T>> s_Pool = new();

        // 标记当前实例是否处于激活状态
        private bool m_Active;

        // 构造函数私有化，防止外部直接 new
        private PooledList()
        {
        }

        /// <summary>
        ///     用完后调用 Dispose，将对象归还池中并清空内容。
        ///     推荐用 using 自动调用。
        /// </summary>
        public void Dispose()
        {
            Assert.IsTrue(m_Active, "PooledList 已经被释放或未激活！");
            m_Active = false;
            Clear();
            s_Pool.Push(this);
#if DEBUG
            // Debug 模式下禁止析构
            GC.SuppressFinalize(this);
#endif
        }

        /// <summary>
        ///     从池中获取一个 PooledList 实例。用完后需调用 Dispose 归还池中。
        ///     推荐配合 using 语句自动释放。
        /// </summary>
        /// <returns>可用的 PooledList 实例</returns>
        public static PooledList<T> Get()
        {
            if (s_Pool.Count == 0)
                // 池为空则新建
                return new PooledList<T> { m_Active = true };

            // 取出池中对象并激活
            var list = s_Pool.Pop();
            list.m_Active = true;
#if DEBUG
            // Debug 模式下重新注册析构检查
            GC.ReRegisterForFinalize(list);
#endif
            return list;
        }

        // Debug 模式下，若未手动 Dispose 则析构时报错，提醒开发者
#if DEBUG
        ~PooledList()
        {
            Debug.LogError($"{nameof(PooledList<T>)} 必须手动 Dispose 释放。");
        }
#endif
    }
}
