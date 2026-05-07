namespace EFramework.Editor.AILoop
{
    public sealed class EFrameReplayInputRequest
    {
        public string InputPath { get; set; } = "";
        public bool Loop { get; set; }
    }

    public sealed class EFrameReplayInputResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string InputPath { get; set; } = "";
        public int CurrentFrame { get; set; }
        public int TotalFrames { get; set; }
        public float Progress { get; set; }
        public bool IsReplaying { get; set; }
    }
}
