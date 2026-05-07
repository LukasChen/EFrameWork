namespace EFramework.Editor.AILoop
{
    public sealed class EFrameUiElementInfo
    {
        public string Label { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Interaction { get; set; } = "";
        public string Path { get; set; } = "";
        public string BindingPath { get; set; } = "";
        public string BindingName { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public float BoundsMinX { get; set; }
        public float BoundsMinY { get; set; }
        public float BoundsMaxX { get; set; }
        public float BoundsMaxY { get; set; }
        public int SortingOrder { get; set; }
        public int SiblingIndex { get; set; }
    }
}
