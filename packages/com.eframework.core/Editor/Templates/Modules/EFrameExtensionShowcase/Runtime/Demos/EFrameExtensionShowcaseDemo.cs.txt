using System;

namespace GameApp.Modules.EFrameExtensionShowcase.Demos
{
    public sealed class EFrameExtensionShowcaseDemo
    {
        public EFrameExtensionShowcaseDemo(
            string title,
            string packageName,
            string assemblyName,
            string description,
            Action run)
        {
            Title = title;
            PackageName = packageName;
            AssemblyName = assemblyName;
            Description = description;
            Run = run;
        }

        public string Title { get; }
        public string PackageName { get; }
        public string AssemblyName { get; }
        public string Description { get; }
        public Action Run { get; }
    }
}
