namespace EFrame.Runtime.DataStorage
{
    public readonly struct DataLoadResult
    {
        public DataLoadResult(
            DataLoadStatus status,
            DataLoadReasonCode reasonCode,
            DataStorageLoadSource source,
            int sourceVersion,
            int targetVersion,
            bool usedLegacyData,
            string message)
        {
            Status = status;
            ReasonCode = reasonCode;
            Source = source;
            SourceVersion = sourceVersion;
            TargetVersion = targetVersion;
            UsedLegacyData = usedLegacyData;
            Message = message;
        }

        public DataLoadStatus Status { get; }
        public DataLoadReasonCode ReasonCode { get; }
        public DataStorageLoadSource Source { get; }
        public int SourceVersion { get; }
        public int TargetVersion { get; }
        public bool UsedLegacyData { get; }
        public string Message { get; }

        public static DataLoadResult None => new DataLoadResult(DataLoadStatus.None, DataLoadReasonCode.None, DataStorageLoadSource.None, 0, 0, false, null);
    }
}