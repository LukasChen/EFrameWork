namespace GameApp.Modules.EFrameExtensionShowcase.Demos
{
    public sealed class EFrameExtensionShowcaseDemo
    {
        public EFrameExtensionShowcaseDemo(
            string title,
            string packageName,
            string assemblyName,
            string description)
        {
            Title = title;
            PackageName = packageName;
            AssemblyName = assemblyName;
            Description = description;
        }

        public string Title { get; }
        public string PackageName { get; }
        public string AssemblyName { get; }
        public string Description { get; }
    }
}
