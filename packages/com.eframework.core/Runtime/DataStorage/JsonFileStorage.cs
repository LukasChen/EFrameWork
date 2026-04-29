using System;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace EFrameWork.Runtime.DataStorage
{
    public class JsonFileStorage : IDataStorage
    {
        private const string FileExtension = ".json";
        private const string TempFileSuffix = ".tmp";
        private const string BackupFileSuffix = ".bak";

        private readonly byte m_xorKey;

        public JsonFileStorage(byte xorKey = 0xAA)
        {
            m_xorKey = xorKey;
        }

        public int Version
        {
            get => 1;
        }

        public void Save<T>(string key, T data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented, UnityJsonSettings.Settings);
            byte[] bytes = EncodeBytes(json);

            string path = GetPath(key);
            string tempPath = GetTempPath(key);
            string backupPath = GetBackupPath(key);
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(tempPath, bytes);

            try
            {
                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(tempPath, path, backupPath, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceFileBestEffort(tempPath, path, backupPath);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            return TryLoad<T>(key, out var data) ? data : defaultValue;
        }

        public bool TryLoad<T>(string key, out T data)
        {
            return TryLoad(key, out data, out _);
        }

        public bool TryLoad<T>(string key, out T data, out DataStorageLoadContext loadContext)
        {
            data = default;
            loadContext = DataStorageLoadContext.NotFound();

            if (!TryReadJson(key, out var json, out loadContext))
            {
                return false;
            }

            try
            {
                var result = JsonConvert.DeserializeObject<T>(json, UnityJsonSettings.Settings);
                if (!typeof(T).IsValueType && result == null)
                {
                    loadContext = DataStorageLoadContext.Failed(loadContext.Source, DataLoadReasonCode.DeserializedNull, $"Deserialized '{key}' as null {typeof(T).Name}.");
                    return false;
                }

                data = result;
                return true;
            }
            catch (JsonException exception)
            {
                string message = $"Failed to deserialize '{key}' as {typeof(T).Name}: {exception.Message}";
                Debug.LogWarning($"[JsonFileStorage] {message}");
                loadContext = DataStorageLoadContext.Failed(loadContext.Source, DataLoadReasonCode.DeserializeFailed, message);
                return false;
            }
        }

        public void Delete(string key)
        {
            string path = GetPath(key);
            if (File.Exists(path)) File.Delete(path);

            string backupPath = GetBackupPath(key);
            if (File.Exists(backupPath)) File.Delete(backupPath);

            string tempPath = GetTempPath(key);
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }

        public bool HasKey(string key)
        {
            return File.Exists(GetPath(key)) || File.Exists(GetBackupPath(key));
        }

        private string GetPath(string key)
        {
            return Path.Combine(Application.persistentDataPath, key + FileExtension);
        }

        private string GetTempPath(string key)
        {
            return GetPath(key) + TempFileSuffix;
        }

        private string GetBackupPath(string key)
        {
            return GetPath(key) + BackupFileSuffix;
        }

        private bool TryReadJson(string key, out string json, out DataStorageLoadContext loadContext)
        {
            json = null;
            loadContext = DataStorageLoadContext.NotFound();
            DataStorageLoadContext primaryFailureContext = DataStorageLoadContext.NotFound();

            string path = GetPath(key);
            if (TryReadJsonFile(path, DataStorageLoadSource.Primary, out json, out loadContext))
            {
                return true;
            }

            if (loadContext.Found)
            {
                primaryFailureContext = loadContext;
            }

            string backupPath = GetBackupPath(key);
            if (TryReadJsonFile(backupPath, DataStorageLoadSource.Backup, out json, out loadContext))
            {
                string message = $"Primary save '{key}' was unavailable or unreadable. Loaded backup file instead.";
                Debug.LogWarning($"[JsonFileStorage] {message}");
                loadContext = DataStorageLoadContext.Loaded(DataStorageLoadSource.Backup, DataLoadReasonCode.LoadedBackup, message);
                return true;
            }

            if (!loadContext.Found && primaryFailureContext.Found)
            {
                loadContext = primaryFailureContext;
            }

            return false;
        }

        private byte[] EncodeBytes(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);

#if !UNITY_EDITOR
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= m_xorKey;
            }
#endif

            return bytes;
        }

        private bool TryReadJsonFile(string path, DataStorageLoadSource source, out string json, out DataStorageLoadContext loadContext)
        {
            json = null;
            loadContext = DataStorageLoadContext.NotFound();

            if (!File.Exists(path))
            {
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (IOException exception)
            {
                string message = $"Failed to read '{path}': {exception.Message}";
                Debug.LogWarning($"[JsonFileStorage] {message}");
                loadContext = DataStorageLoadContext.Failed(source, DataLoadReasonCode.ReadFailed, message);
                return false;
            }

            json = Encoding.UTF8.GetString(DecodeBytes(bytes));
            if (string.IsNullOrEmpty(json))
            {
                loadContext = DataStorageLoadContext.Failed(source, DataLoadReasonCode.EmptyContent, $"Read '{path}' but content was empty.");
                return false;
            }

            loadContext = DataStorageLoadContext.Loaded(source, source == DataStorageLoadSource.Backup ? DataLoadReasonCode.LoadedBackup : DataLoadReasonCode.LoadedPrimary);
            return true;
        }

        private byte[] DecodeBytes(byte[] bytes)
        {
#if !UNITY_EDITOR
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= m_xorKey;
            }
#endif

            return bytes;
        }

        private static void ReplaceFileBestEffort(string tempPath, string path, string backupPath)
        {
            if (File.Exists(path))
            {
                File.Copy(path, backupPath, true);
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
    }
}
