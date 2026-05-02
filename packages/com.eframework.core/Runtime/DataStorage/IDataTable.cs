namespace EFrame.Runtime.DataStorage
{
    public interface IDataTable
    {
        string Key { get; }
        string StorageKey { get; }
        DataSaveResult LastSaveResult { get; }

        void SetDataStorage(IDataStorage dataStorage);

        void Save(bool forceFlush = false);

        void Reset();

        void Load();

        void Dispose();
    }
}
