namespace EFrameWork.Runtime.DataStorage
{
    public enum DataLoadStatus
    {
        None = 0,
        DefaultDataCreated = 1,
        Loaded = 2,
        Migrated = 3,
        FutureVersionLoaded = 4,
        FallbackToDefault = 5,
        MigrationFailed = 6
    }
}