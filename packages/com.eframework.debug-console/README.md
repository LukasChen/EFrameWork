# EFrame Debug Console

Optional EFrame extension package that vendors Unity Ingame Debug Console.

## Contents

- Runtime log viewer and command console from `IngameDebugConsole.Runtime`.
- `EFramework.Extensions.DebugConsole.EFrameDebugConsole.Show()` for runtime code that should instantiate and show the console panel on demand.
- Editor support from `IngameDebugConsole.Editor`.
- Original third-party license in `LICENSE.txt`.

## Dependencies

- `com.eframework.core`
- `com.unity.inputsystem`
- `com.unity.ugui` (provides TextMeshPro runtime assemblies in current Unity versions)

TextMeshPro is an external Unity package dependency for this extension. The package does not vendor TextMeshPro plugin files or TMP Essential Resources; showcase fixtures may keep those assets locally for testing, but generated EFrame templates should not redistribute them.

The vendored upstream asset is Unity Ingame Debug Console `1.8.2`.
