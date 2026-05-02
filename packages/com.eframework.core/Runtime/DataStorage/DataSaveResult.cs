namespace EFrame.Runtime.DataStorage
{
    public readonly struct DataSaveResult
    {
        public DataSaveResult(
            DataSaveStatus status,
            DataSaveReasonCode reasonCode,
            bool forceFlush,
            int savedVersion,
            string message)
        {
            Status = status;
            ReasonCode = reasonCode;
            ForceFlush = forceFlush;
            SavedVersion = savedVersion;
            Message = message;
        }

        public DataSaveStatus Status { get; }
        public DataSaveReasonCode ReasonCode { get; }
        public bool ForceFlush { get; }
        public int SavedVersion { get; }
        public string Message { get; }

        public static DataSaveResult None => new DataSaveResult(DataSaveStatus.None, DataSaveReasonCode.None, false, 0, null);
    }
}