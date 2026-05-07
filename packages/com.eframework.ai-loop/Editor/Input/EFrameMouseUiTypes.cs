namespace EFramework.Editor.AILoop
{
    public enum EFrameMouseUiAction
    {
        Click = 0,
        LongPress = 1,
        Drag = 2
    }

    public enum EFrameMouseButton
    {
        Left = 0,
        Right = 1,
        Middle = 2
    }

    public sealed class EFrameMouseUiRequest
    {
        public EFrameMouseUiAction Action { get; set; } = EFrameMouseUiAction.Click;
        public float X { get; set; }
        public float Y { get; set; }
        public float FromX { get; set; }
        public float FromY { get; set; }
        public float Duration { get; set; } = 0.5f;
        public float DragSpeed { get; set; } = 1200f;
        public EFrameMouseButton Button { get; set; } = EFrameMouseButton.Left;
    }

    public sealed class EFrameMouseUiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string HitGameObjectName { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
    }
}
