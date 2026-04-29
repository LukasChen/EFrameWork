using System;

namespace EFrameWork.Runtime.DataStorage
{
    public interface IDataService : IDisposable
    {
        T RegisterTable<T>() where T : IDataTable;
        T GetTable<T>();
        void SaveAll(bool forceFlush = false);
        void LoadAll();
    }
}
