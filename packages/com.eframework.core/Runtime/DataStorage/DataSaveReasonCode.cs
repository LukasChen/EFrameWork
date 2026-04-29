namespace EFrameWork.Runtime.DataStorage
{
    public enum DataSaveReasonCode
    {
        None = 0,
        SavedDirty = 1,
        SavedForceFlush = 2,
        SkippedNotDirty = 3,
        MissingStorage = 4,
        StorageException = 5
    }
}