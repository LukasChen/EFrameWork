using EFrameWork.Runtime.Event;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFrameWork.Runtime.DataStorage
{
    public sealed class DataManager : IDataService
    {
        private readonly IDataStorage m_storage;
        private readonly Dictionary<Type, IDataTable> m_tables = new();
        private readonly Dictionary<string, string> m_storageKeys = new();
        private bool m_disposed;

        public DataManager(IDataStorage storage)
        {
            m_storage = storage ?? throw new ArgumentNullException(nameof(storage));

            EventBus.Subscribe<AppStartedEvent>(OnAppStart);
            EventBus.Subscribe<AppPausedEvent>(OnAppPause);
            EventBus.Subscribe<AppQuitEvent>(OnAppQuit);
        }

        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            EventBus.Unsubscribe<AppPausedEvent>(OnAppPause);
            EventBus.Unsubscribe<AppQuitEvent>(OnAppQuit);
            EventBus.Unsubscribe<AppStartedEvent>(OnAppStart);

            SaveAll(true);

            foreach (IDataTable table in m_tables.Values)
            {
                table.Dispose();
            }
            m_tables.Clear();
            m_storageKeys.Clear();
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
        public T RegisterTable<T>() where T : IDataTable, new()
        {
            ThrowIfDisposed();

            Type tableType = typeof(T);
            var table = new T();
            table.SetDataStorage(m_storage);
            string key = table.Key;
            string storageKey = table.StorageKey;
            string tableIdentity = tableType.FullName ?? key;

            if (m_tables.TryGetValue(tableType, out var existingTable))
            {
                table.Dispose();
                return (T)existingTable;
            }

            if (string.IsNullOrWhiteSpace(storageKey))
            {
                table.Dispose();
                throw new InvalidOperationException($"DataTable '{key}' must declare a non-empty StorageKey.");
            }

            if (m_storageKeys.TryGetValue(storageKey, out var existingTableKey))
            {
                table.Dispose();
                throw new InvalidOperationException($"DataTable '{key}' cannot reuse StorageKey '{storageKey}' because it is already used by '{existingTableKey}'.");
            }

            m_tables.Add(tableType, table);
            m_storageKeys.Add(storageKey, tableIdentity);
            table.Load();

            Debug.Log($"{key} registered successfully.");
            return table;
        }

        public T GetTable<T>()
        {
            ThrowIfDisposed();

            Type tableType = typeof(T);
            if (m_tables.TryGetValue(tableType, out IDataTable table)) return (T)table;

            Debug.LogError($"{tableType.FullName} is not registered.");
            return default;
        }

        // 保存所有表
        public void SaveAll(bool forceFlush = false)
        {
            if (m_disposed && !forceFlush) return;
            foreach (IDataTable table in m_tables.Values) table.Save(forceFlush);
        }

        // 加载所有表
        public void LoadAll()
        {
            ThrowIfDisposed();
            foreach (IDataTable table in m_tables.Values) table.Load();
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(DataManager));
            }
        }
    }
}
