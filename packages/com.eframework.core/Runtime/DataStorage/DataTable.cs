using UnityEngine;
using System;
using System.Collections.Generic;

namespace EFramework.Runtime.DataStorage
{
    public abstract class DataTable<T> : IDataTable
    {
        private IDataStorage m_dataStorage;
        private bool m_initialized;
        protected T m_data;

        protected DataTable()
        {
            Key = GetType().Name;
        }

        public string Key { get; }

        public bool IsDirty { get; private set; }
        public bool IsLoaded { get; private set; }
        public int LoadedVersion { get; private set; }
        public DataLoadResult LastLoadResult { get; private set; }
        public DataSaveResult LastSaveResult { get; private set; }
        public string StorageKey => GetStorageKey();

        protected T Data => m_data;
        protected virtual int CurrentVersion => 1;

        public void Reset()
        {
            m_data = GetDefaultData();
            MarkDirty();
        }

        protected void MarkDirty()
        {
            IsDirty = true;
        }

        protected bool SetValue<TValue>(ref TValue field, TValue value)
        {
            if (EqualityComparer<TValue>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            MarkDirty();
            return true;
        }

        protected bool Mutate(Func<T, bool> mutator)
        {
            if (mutator == null)
            {
                return false;
            }

            if (!mutator(m_data))
            {
                return false;
            }

            MarkDirty();
            return true;
        }

        public void Save(bool forceFlush = false)
        {
            if (m_dataStorage == null)
            {
                const string message = "Save skipped because no data storage is configured.";
                Debug.LogError($"DataTable: {Key} has no data storage.");
                LastSaveResult = new DataSaveResult(
                    DataSaveStatus.Failed,
                    DataSaveReasonCode.MissingStorage,
                    forceFlush,
                    LoadedVersion,
                    message);
                return;
            }

            if (!forceFlush && !IsDirty)
            {
                LastSaveResult = new DataSaveResult(
                    DataSaveStatus.Skipped,
                    DataSaveReasonCode.SkippedNotDirty,
                    false,
                    LoadedVersion,
                    $"Skipped saving '{Key}' because the table is not dirty.");
                return;
            }

            var envelope = new DataEnvelope<T>
            {
                Version = CurrentVersion,
                SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Data = m_data
            };

            try
            {
                m_dataStorage.Save(StorageKey, envelope);
                LoadedVersion = CurrentVersion;
                IsDirty = false;
                LastSaveResult = new DataSaveResult(
                    DataSaveStatus.Saved,
                    forceFlush ? DataSaveReasonCode.SavedForceFlush : DataSaveReasonCode.SavedDirty,
                    forceFlush,
                    LoadedVersion,
                    forceFlush
                        ? $"Saved '{Key}' with force flush."
                        : $"Saved '{Key}' because the table was dirty.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"DataTable: {Key} save failed. {exception}");
                LastSaveResult = new DataSaveResult(
                    DataSaveStatus.Failed,
                    DataSaveReasonCode.StorageException,
                    forceFlush,
                    LoadedVersion,
                    exception.Message);
            }
        }

        public void Load()
        {
            if (m_dataStorage == null)
            {
                Debug.LogError($"DataTable: {Key} has no data storage.");
                return;
            }

            bool hasStoredData = m_dataStorage.HasKey(StorageKey);
            if (hasStoredData)
            {
                if (TryLoadVersionedData(out var loadedVersion, out var loadedData, out var loadSource, out var usedLegacyData, out var failureReasonCode, out var failureMessage))
                {
                    m_data = loadedData;
                    LoadedVersion = loadedVersion;

                    if (LoadedVersion < CurrentVersion)
                    {
                        try
                        {
                            m_data = Migrate(m_data, LoadedVersion);
                            LoadedVersion = CurrentVersion;
                            MarkDirty();
                            LastLoadResult = new DataLoadResult(
                                DataLoadStatus.Migrated,
                                DataLoadReasonCode.Migrated,
                                loadSource,
                                loadedVersion,
                                CurrentVersion,
                                usedLegacyData,
                                $"Migrated '{Key}' from version {loadedVersion} to {CurrentVersion}.");
                            Debug.Log($"DataTable: {Key} migrated to version {CurrentVersion}.");
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError($"DataTable: {Key} migration failed from version {loadedVersion} to {CurrentVersion}. {exception}");
                            m_data = GetDefaultData();
                            LoadedVersion = CurrentVersion;
                            IsDirty = false;
                            LastLoadResult = new DataLoadResult(
                                DataLoadStatus.MigrationFailed,
                                DataLoadReasonCode.MigrationFailed,
                                loadSource,
                                loadedVersion,
                                CurrentVersion,
                                usedLegacyData,
                                exception.Message);
                        }
                    }
                    else
                    {
                        if (LoadedVersion > CurrentVersion)
                        {
                            Debug.LogWarning($"DataTable: {Key} was saved with future version {LoadedVersion}. Current runtime version is {CurrentVersion}.");
                            LastLoadResult = new DataLoadResult(
                                DataLoadStatus.FutureVersionLoaded,
                                DataLoadReasonCode.FutureVersion,
                                loadSource,
                                LoadedVersion,
                                CurrentVersion,
                                usedLegacyData,
                                $"Loaded future version {LoadedVersion} for '{Key}' while runtime expects {CurrentVersion}.");
                        }
                        else
                        {
                            LastLoadResult = new DataLoadResult(
                                DataLoadStatus.Loaded,
                                usedLegacyData
                                    ? DataLoadReasonCode.LoadedLegacyRaw
                                    : loadSource == DataStorageLoadSource.Backup
                                        ? DataLoadReasonCode.LoadedBackup
                                        : DataLoadReasonCode.LoadedPrimary,
                                loadSource,
                                LoadedVersion,
                                CurrentVersion,
                                usedLegacyData,
                                loadSource == DataStorageLoadSource.Backup
                                    ? $"Loaded '{Key}' from backup storage."
                                    : usedLegacyData
                                        ? $"Loaded legacy raw data for '{Key}'."
                                        : $"Loaded '{Key}' successfully.");
                        }

                        IsDirty = false;
                    }
                }
                else
                {
                    Debug.LogWarning($"DataTable: {Key} load failed, using default data without overwriting the existing file.");
                    m_data = GetDefaultData();
                    LoadedVersion = CurrentVersion;
                    IsDirty = false;
                    LastLoadResult = new DataLoadResult(
                        DataLoadStatus.FallbackToDefault,
                        failureReasonCode == DataLoadReasonCode.None ? DataLoadReasonCode.FallbackToDefault : failureReasonCode,
                        DataStorageLoadSource.None,
                        0,
                        CurrentVersion,
                        false,
                        string.IsNullOrEmpty(failureMessage)
                            ? $"Stored data for '{Key}' could not be read and default data was used."
                            : failureMessage);
                }
            }
            else
            {
                m_data = GetDefaultData();
                LoadedVersion = CurrentVersion;
                MarkDirty();
                LastLoadResult = new DataLoadResult(
                    DataLoadStatus.DefaultDataCreated,
                    DataLoadReasonCode.DefaultDataCreated,
                    DataStorageLoadSource.None,
                    0,
                    CurrentVersion,
                    false,
                    $"No stored data found for '{Key}'. Default data was created.");
                Debug.Log($" DataTable: {Key} not found, using default data.");
            }

            IsLoaded = true;

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
            IsLoaded = false;
            LoadedVersion = 0;
            LastLoadResult = DataLoadResult.None;
            LastSaveResult = DataSaveResult.None;
        }

        public void SetDataStorage(IDataStorage dataStorage)
        {
            m_dataStorage = dataStorage ?? throw new System.ArgumentNullException(nameof(dataStorage));
        }

        protected virtual T GetDefaultData()
        {
            return default;
        }

        protected virtual T Migrate(T data, int fromVersion)
        {
            return data;
        }

        protected abstract string GetStorageKey();

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

        private bool TryLoadVersionedData(out int loadedVersion, out T data, out DataStorageLoadSource loadSource, out bool usedLegacyData, out DataLoadReasonCode failureReasonCode, out string failureMessage)
        {
            loadedVersion = 0;
            data = default;
            loadSource = DataStorageLoadSource.None;
            usedLegacyData = false;
            failureReasonCode = DataLoadReasonCode.None;
            failureMessage = null;

            if (m_dataStorage.TryLoad<DataEnvelope<T>>(StorageKey, out var envelope, out var envelopeLoadContext) && envelope != null && HasEnvelopeData(envelope))
            {
                loadedVersion = Mathf.Max(0, envelope.Version);
                data = envelope.Data;
                loadSource = envelopeLoadContext.Source;
                return true;
            }

            if (envelopeLoadContext.Success && envelope != null && !HasEnvelopeData(envelope))
            {
                failureReasonCode = DataLoadReasonCode.EnvelopeDataMissing;
                failureMessage = $"Stored envelope data for '{Key}' was missing or null.";
            }
            else
            {
                failureReasonCode = envelopeLoadContext.ReasonCode;
                failureMessage = envelopeLoadContext.Message;
            }

            if (m_dataStorage.TryLoad<T>(StorageKey, out var legacyData, out var legacyLoadContext))
            {
                loadedVersion = 0;
                data = legacyData;
                loadSource = legacyLoadContext.Source;
                usedLegacyData = true;
                Debug.Log($"DataTable: {Key} loaded legacy raw data and will treat it as version 0.");
                return true;
            }

            if (legacyLoadContext.ReasonCode != DataLoadReasonCode.NotFound)
            {
                failureReasonCode = legacyLoadContext.ReasonCode;
            }

            if (!string.IsNullOrEmpty(legacyLoadContext.Message))
            {
                failureMessage = legacyLoadContext.Message;
            }

            return false;
        }

        private static bool HasEnvelopeData(DataEnvelope<T> envelope)
        {
            if (envelope == null)
            {
                return false;
            }

            if (!typeof(T).IsValueType && ReferenceEquals(envelope.Data, null))
            {
                return false;
            }

            return true;
        }
    }
}
