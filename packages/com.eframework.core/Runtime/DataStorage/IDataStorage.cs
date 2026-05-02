namespace EFramework.Runtime.DataStorage
{
    public interface IDataStorage
    {
        int Version { get; }

        void Save<T>(string key, T data);

        T Load<T>(string key, T defaultValue = default);

        bool TryLoad<T>(string key, out T data);

        bool TryLoad<T>(string key, out T data, out DataStorageLoadContext loadContext);

        void Delete(string key);

        bool HasKey(string key);
    }
}
