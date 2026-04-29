using EFrameWork.Runtime.Event;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFrameWork.Runtime.DataStorage
{
    public sealed class DataManager : IDataService
    {
        private IDataStorage m_storage;
        private Dictionary<string, IDataTable> m_tables = new();

        public DataManager(IDataStorage storage)
        {
            // 初始化数据存储
            m_storage = storage;

            EventBus.Subscribe<AppStartedEvent>(OnAppStart);
            EventBus.Subscribe<AppPausedEvent>(OnAppPause);
            EventBus.Subscribe<AppQuitEvent>(OnAppQuit);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<AppPausedEvent>(OnAppPause);
            EventBus.Unsubscribe<AppQuitEvent>(OnAppQuit);
            EventBus.Unsubscribe<AppStartedEvent>(OnAppStart);

            // 销毁所有数据表
            foreach (IDataTable table in m_tables.Values)
            {
                table.Dispose();
            }
            m_tables.Clear();
        }

        private void OnAppStart(AppStartedEvent e)
        {
        }

        private void OnAppQuit(AppQuitEvent e)
        {
            SaveAll(true);
        }

        private void OnAppPause(AppPausedEvent e)
        {
            SaveAll();
        }

        /// <summary>
        ///     注册数据表 并自动加载
        /// </summary>
        /// <typeparam name="T"> IDataTable </typeparam>
        public T RegisterTable<T>() where T : IDataTable
        {
            Type tableType = typeof(T);

            if (Activator.CreateInstance(tableType) is IDataTable table)
            {
                table.SetDataStorage(m_storage);
                string key = table.Key;
                if (!m_tables.TryAdd(key, table)) Debug.LogError($"{key} is already registered.");
                table.Load();

                Debug.Log($"{key} registered successfully.");

                return (T)table;
            }
            return default;
        }

        public T GetTable<T>()
        {
            Type tableType = typeof(T);
            string key = tableType.Name;
            if (m_tables.TryGetValue(key, out IDataTable table)) return (T)table;

            Debug.LogError($"{key} is not registered.");
            return default;
        }

        // 保存所有表
        public void SaveAll(bool forceFlush = false)
        {
            foreach (IDataTable table in m_tables.Values) table.Save(forceFlush);
        }

        // 加载所有表
        public void LoadAll()
        {
            foreach (IDataTable table in m_tables.Values) table.Load();
        }
    }
}
