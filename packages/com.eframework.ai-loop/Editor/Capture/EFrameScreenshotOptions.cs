namespace EFramework.Editor.AILoop
{
    public sealed class EFrameScreenshotOptions
    {
        public EFrameCaptureMode CaptureMode { get; set; } = EFrameCaptureMode.GameRendering;
        public string WindowName { get; set; } = "Game";
        public float ResolutionScale { get; set; } = 1f;
        public string OutputDirectory { get; set; } = "";
        public bool AnnotateElements { get; set; }
        public bool ElementsOnly { get; set; }
    }
}
