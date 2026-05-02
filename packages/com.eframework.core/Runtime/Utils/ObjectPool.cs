using System;
using System.Collections.Generic;

namespace EFrame.Runtime.Utils
{
    /*
     * 使用范例：
     *
     * // 常规使用
     * var vector2IntPool = new ObjectPool<Vector2Int>();
     * var vector = vector2IntPool.Get();
     * vector.x = 10;
     * vector.y = 20;
     * vector2IntPool.Release(vector); // 手动归还池中
     *------------------------------------------------------------------------
     * // 作用域使用
     * var listPool = new ObjectPool<List<int>>();
     * using (var pooledList = listPool.GetScoped())
     * {
     *     var list = pooledList.Instance;
     *     list.Add(1);
     *     list.Add(2);
     *     Debug.Log(string.Join(", ", list));
     * } // 作用域结束后自动归还池中
     */

    /// <summary>
    ///     表示一个可作用域管理的对象，支持在 using 语句中使用，作用域结束后自动归还对象池。
    /// </summary>
    public class PooledObject<T> : IDisposable where T : new()
    {
        private readonly ObjectPool<T> _pool;
        private bool m_disposed;

        public PooledObject(ObjectPool<T> pool)
        {
            _pool = pool;
            Instance = _pool.Get();
        }

        public T Instance { get; }

        public int Count
        {
            get => _pool.Count;
        }

        public void Dispose()
        {
            if (m_disposed) return;
            _pool.Release(Instance);
            m_disposed = true;
        }
    }

    /// <summary>
    ///     通用对象池，用于管理对象的获取和归还，减少频繁的内存分配和回收。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class ObjectPool<T> where T : new()
    {
        private readonly Stack<T> _pool = new Stack<T>();

        public int Count
        {
            get => _pool.Count;
        }

        public T Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : new T();
        }

        public void Release(T obj)
        {
            _pool.Push(obj);
        }

        public PooledObject<T> GetScoped()
        {
            return new PooledObject<T>(this);
        }
    }
}
