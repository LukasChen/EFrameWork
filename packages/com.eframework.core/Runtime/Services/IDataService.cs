using System;

namespace EFrame.Runtime.DataStorage
{
    public interface IDataService : IDisposable
    {
        T RegisterTable<T>() where T : IDataTable, new();
        T GetTable<T>();
        void SaveAll(bool forceFlush = false);
        void LoadAll();
    }
}
