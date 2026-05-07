namespace EFramework.Editor.AILoop
{
    public sealed class EFrameRecordInputRequest
    {
        public string OutputPath { get; set; } = "";
        public string Keys { get; set; } = "";
    }

    public sealed class EFrameRecordInputResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public string ReportPath { get; set; } = "";
        public int TotalFrames { get; set; }
        public float DurationSeconds { get; set; }
        public int EventCount { get; set; }
    }
}
