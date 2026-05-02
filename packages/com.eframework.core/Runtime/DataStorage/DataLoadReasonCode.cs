namespace EFramework.Runtime.DataStorage
{
    public enum DataLoadReasonCode
    {
        None = 0,
        NotFound = 1,
        LoadedPrimary = 2,
        LoadedBackup = 3,
        LoadedLegacyRaw = 4,
        ReadFailed = 5,
        EmptyContent = 6,
        DeserializeFailed = 7,
        DeserializedNull = 8,
        EnvelopeDataMissing = 9,
        Migrated = 10,
        FutureVersion = 11,
        MigrationFailed = 12,
        DefaultDataCreated = 13,
        FallbackToDefault = 14
    }
}