namespace EFrameWork.Runtime
{
    public readonly struct EFrameInitializationResult
    {
        public EFrameInitializationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }
        public string Message { get; }

        public static EFrameInitializationResult Success(string message = "EFrame initialized.")
        {
            return new EFrameInitializationResult(true, message);
        }

        public static EFrameInitializationResult Failure(string message)
        {
            return new EFrameInitializationResult(false, message);
        }
    }
}
