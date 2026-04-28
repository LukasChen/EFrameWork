using UnityEngine;

namespace EFrameWork.Runtime.DataStorage
{
    public abstract class DataTable<T> : IDataTable
    {
        private IDataStorage m_dataStorage;
        private bool m_dirty;
        private bool m_initialized;

        protected DataTable()
        {
            Key = GetType().Name;
        }

        public string Key { get; }

        public T Data { get; protected set; }

        public void Reset()
        {
            Data = GetDefaultData();
            m_dirty = true;
        }

        public void SetDirty()
        {
            m_dirty = true;
        }

        public void Save(bool forceFlush = false)
        {
            if (forceFlush || m_dirty)
            {
                m_dataStorage.Save(Key, Data);
                m_dirty = false;
            }
        }

        public void Load()
        {
            if (m_dataStorage.HasKey(Key))
            {
                Data = m_dataStorage.Load<T>(Key);
                // 如果加载失败（文件损坏或内容为空），使用默认值
                if (Data == null)
                {
                    Debug.LogWarning($"DataTable: {Key} load failed (corrupted or empty), using default data.");
                    Data = GetDefaultData();
                    Save(true);
                }
                m_dirty = false;
            }
            else
            {
                Data = GetDefaultData();
                Save(true);
                Debug.Log($" DataTable: {Key} not found, using default data.");
            }

            // 首次加载后调用初始化
            if (!m_initialized)
            {
                m_initialized = true;
                OnInit();
            }
        }

        public void Dispose()
        {
            if (m_initialized)
            {
                OnDispose();
                m_initialized = false;
            }
        }

        public void SetDataStorage(IDataStorage dataStorage)
        {
            m_dataStorage = dataStorage;
        }

        protected virtual T GetDefaultData()
        {
            return default;
        }

        /// <summary>
        /// 初始化回调，在首次加载数据后调用
        /// </summary>
        /// <remarks>
        /// 子类可重写此方法进行事件订阅等初始化操作
        /// </remarks>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// 销毁回调，在 Dispose 时调用
        /// </summary>
        /// <remarks>
        /// 子类可重写此方法进行事件取消订阅等清理操作
        /// </remarks>
        protected virtual void OnDispose()
        {
        }
    }
}
