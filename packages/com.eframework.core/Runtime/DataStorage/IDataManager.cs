namespace EFrameWork.Runtime.DataStorage
{
    public interface IDataManager
    {
        void RegisterTable<T>(DataTable<T> table);

        void Save<T>(string key, T data);

        T Load<T>(string key, T defaultValue = default);

        void Delete(string key);

        bool HasKey(string key);
    }
}
