namespace EFrameWork.Runtime.DataStorage
{
    public interface IDataTable
    {
        string Key { get; }

        void SetDataStorage(IDataStorage dataStorage);

        void Save(bool forceFlush = false);

        void Reset();

        void SetDirty();

        void Load();

        void Dispose();
    }
}
