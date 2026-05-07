namespace GameApp.Modules.EFrameExtensionShowcase.Demos
{
    public sealed class EFrameExtensionShowcaseDemo
    {
        public EFrameExtensionShowcaseDemo(
            EFrameExtensionShowcaseDemoKind kind,
            string title,
            string packageName,
            string assemblyName,
            string description)
        {
            Kind = kind;
            Title = title;
            PackageName = packageName;
            AssemblyName = assemblyName;
            Description = description;
        }

        public EFrameExtensionShowcaseDemoKind Kind { get; }
        public string Title { get; }
        public string PackageName { get; }
        public string AssemblyName { get; }
        public string Description { get; }
    }

    public enum EFrameExtensionShowcaseDemoKind
    {
        Feedback,
        VirtualList,
        DebugConsole
    }
}
