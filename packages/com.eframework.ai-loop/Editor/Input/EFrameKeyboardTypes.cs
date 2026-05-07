namespace EFramework.Editor.AILoop
{
    public enum EFrameKeyboardAction
    {
        Press = 0,
        KeyDown = 1,
        KeyUp = 2
    }

    public sealed class EFrameKeyboardRequest
    {
        public EFrameKeyboardAction Action { get; set; } = EFrameKeyboardAction.Press;
        public string Key { get; set; } = "";
        public float Duration { get; set; }
    }

    public sealed class EFrameKeyboardResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Key { get; set; } = "";
    }
}
