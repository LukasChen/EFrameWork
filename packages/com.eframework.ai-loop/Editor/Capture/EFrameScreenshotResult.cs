using System.Collections.Generic;

namespace EFramework.Editor.AILoop
{
    public sealed class EFrameScreenshotResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Path { get; set; } = "";
        public long FileSizeBytes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int YOffset { get; set; }
        public string CoordinateSystem { get; set; } = "GameViewTopLeft";
        public List<EFrameUiElementInfo> Elements { get; set; } = new();
    }
}
