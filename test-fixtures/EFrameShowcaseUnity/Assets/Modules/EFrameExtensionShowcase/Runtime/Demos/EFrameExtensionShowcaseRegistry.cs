using System;
using System.Collections.Generic;
using System.Linq;

namespace GameApp.Modules.EFrameExtensionShowcase.Demos
{
    public static class EFrameExtensionShowcaseRegistry
    {
        private static readonly EFrameExtensionShowcaseDemo[] s_demos =
        {
            Create("UI Virtual List", "com.eframework.ui.virtual-list", "EFrame.UI.VirtualList", "Virtual list and grid controls for large scrollable data sets."),
            Create("Debug Console", "com.eframework.debug-console", "IngameDebugConsole.Runtime", "Runtime debug console integration.")
        };

        public static IReadOnlyList<EFrameExtensionShowcaseDemo> Demos => s_demos;

        public static bool IsInstalled(EFrameExtensionShowcaseDemo demo)
        {
            if (demo == null || string.IsNullOrWhiteSpace(demo.AssemblyName))
            {
                return false;
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .Any(assembly => string.Equals(assembly.GetName().Name, demo.AssemblyName, StringComparison.Ordinal));
        }

        private static EFrameExtensionShowcaseDemo Create(string title, string packageName, string assemblyName, string description)
        {
            return new EFrameExtensionShowcaseDemo(
                title,
                packageName,
                assemblyName,
                description);
        }
    }
}
