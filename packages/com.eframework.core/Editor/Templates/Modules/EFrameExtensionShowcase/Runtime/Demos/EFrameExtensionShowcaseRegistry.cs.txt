using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameApp.Modules.EFrameExtensionShowcase.Demos
{
    public static class EFrameExtensionShowcaseRegistry
    {
        private static readonly EFrameExtensionShowcaseDemo[] s_demos =
        {
            Create("UI Virtual List", "com.eframework.ui.virtual-list", "EFrame.UI.VirtualList", "Virtual list and grid controls for large scrollable data sets."),
            Create("UI Extras", "com.eframework.ui-extras", "EFrame.UI.Extras", "Reusable UI helpers, tabs, non-rendering raycast targets, and UI animations."),
            Create("Effects", "com.eframework.effects", "EFrame.Effects", "Presentation effects such as fly animation, icon bounce, and camera shake."),
            Create("GMTools", "com.eframework.gm-tools", "EFrame.GMTools", "Runtime GM command and button helpers for development builds."),
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
                description,
                () => Debug.Log($"[EFrameExtensionShowcase] Selected {title}. Replace this placeholder with a focused {packageName} demo."));
        }
    }
}
