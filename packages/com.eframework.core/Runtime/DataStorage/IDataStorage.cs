namespace EFrameWork.Runtime.DataStorage
{
    public interface IDataStorage
    {
        int Version { get; }

        void Save<T>(string key, T data);

        T Load<T>(string key, T defaultValue = default);

        void Delete(string key);

        bool HasKey(string key);
    }
}
