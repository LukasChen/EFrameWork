namespace EFrame.Runtime
{
    public enum EFrameInitializationStage
    {
        None,
        Component,
        Assets,
        Data,
        Audio,
        UI,
        Tween,
        Completed
    }

    public readonly struct EFrameInitializationResult
    {
        public EFrameInitializationResult(bool succeeded, string message, EFrameInitializationStage stage = EFrameInitializationStage.None)
        {
            Succeeded = succeeded;
            Message = message;
            Stage = stage;
        }

        public bool Succeeded { get; }
        public string Message { get; }
        public EFrameInitializationStage Stage { get; }

        public static EFrameInitializationResult Success(string message = "EFrame initialized.")
        {
            return new EFrameInitializationResult(true, message, EFrameInitializationStage.Completed);
        }

        public static EFrameInitializationResult Failure(string message, EFrameInitializationStage stage = EFrameInitializationStage.None)
        {
            return new EFrameInitializationResult(false, message, stage);
        }
    }
}
