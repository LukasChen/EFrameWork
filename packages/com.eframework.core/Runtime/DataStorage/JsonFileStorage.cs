using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
namespace EFrameWork.Runtime.DataStorage
{
    public class JsonFileStorage : IDataStorage
    {
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
            byte[] bytes = Encoding.UTF8.GetBytes(json);

#if !UNITY_EDITOR
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] ^= m_xorKey;
                    }
#endif
            File.WriteAllBytes(GetPath(key), bytes);
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            string path = GetPath(key);
            if (!File.Exists(path)) return defaultValue;
            byte[] bytes = File.ReadAllBytes(path);

#if !UNITY_EDITOR
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] ^= m_xorKey;
                    }
#endif
            string json = Encoding.UTF8.GetString(bytes);
            try
            {
                if (string.IsNullOrEmpty(json))
                    return defaultValue;

                T result = JsonConvert.DeserializeObject<T>(json, UnityJsonSettings.Settings);
                return result != null ? result : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public void Delete(string key)
        {
            string path = GetPath(key);
            if (File.Exists(path)) File.Delete(path);
        }

        public bool HasKey(string key)
        {
            return File.Exists(GetPath(key));
        }

        private string GetPath(string key)
        {
            return Path.Combine(Application.persistentDataPath, key + ".json");
        }
    }
}
