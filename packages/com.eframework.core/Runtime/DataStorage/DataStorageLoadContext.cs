namespace EFrameWork.Runtime.DataStorage
{
    public readonly struct DataStorageLoadContext
    {
        public DataStorageLoadContext(bool found, bool success, DataStorageLoadSource source, DataLoadReasonCode reasonCode, string message)
        {
            Found = found;
            Success = success;
            Source = source;
            ReasonCode = reasonCode;
            Message = message;
        }

        public bool Found { get; }
        public bool Success { get; }
        public DataStorageLoadSource Source { get; }
        public DataLoadReasonCode ReasonCode { get; }
        public string Message { get; }

        public static DataStorageLoadContext NotFound()
        {
            return new DataStorageLoadContext(false, false, DataStorageLoadSource.None, DataLoadReasonCode.NotFound, null);
        }

        public static DataStorageLoadContext Loaded(DataStorageLoadSource source, DataLoadReasonCode reasonCode, string message = null)
        {
            return new DataStorageLoadContext(true, true, source, reasonCode, message);
        }

        public static DataStorageLoadContext Failed(DataStorageLoadSource source, DataLoadReasonCode reasonCode, string message)
        {
            return new DataStorageLoadContext(true, false, source, reasonCode, message);
        }
    }
}