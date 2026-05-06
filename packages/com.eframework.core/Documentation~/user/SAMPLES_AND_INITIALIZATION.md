# EFrame Samples And Initialization

EFrame uses three layers:

- Core: `com.eframework.core`, stable runtime/editor infrastructure and the minimal startup skeleton.
- Extension: optional packages such as `com.eframework.ui.virtual-list`, `com.eframework.ui-extras`, `com.eframework.effects`, and `com.eframework.debug-console`.
- Samples: project skeletons or modules installed into a consuming Unity project when they are useful.

## Basic

Basic is the core-owned minimal runnable project skeleton. It is the maintained version of the current initialization template and does not reference optional extension packages.

Install it from Unity:

```text
EFrame Tools/项目初始化向导
-> Initialize / Repair Project
```

The initializer copies the package Basic template, syncs the EFrame AI workspace for all supported clients, installs the project AI updater when available, and then repairs Addressables, UI sorting layers, audio, fallback tween readiness, and build settings.

Basic creates or prepares:

- `Assets/Scenes/StartUp.unity`
- `Assets/App/Runtime/Procedure/ProcedureLauncher.cs`
- `Assets/App/Runtime/Procedure/ProcedureHome.cs`
- `Assets/App/Runtime/UI/Views/HomeView.cs`
- `Assets/App/Runtime/UI/Controllers/HomeViewController.cs`
- `Assets/App/Res/UI/Panels/Home/HomeView.prefab`
- Addressables groups and generated `ResPath`
- UI sorting layers
- basic audio resources and fallback tween backend readiness

The editable Basic source is a full Unity fixture under `test-fixtures/EFrameBasicTemplate`; maintainers sync its `Assets` folder into `Editor/Templates/Basic` with `tools/Sync-EFrameBasicTemplate.ps1`.

## Extension Showcase Module

Extension Showcase is an optional Project Module installed to:

```text
Assets/Modules/EFrameExtensionShowcase/
```

Install it from Unity:

```text
EFrame Tools/项目初始化向导
-> Extensions -> Apply
-> Showcase -> Install Showcase
```

`Extensions` opens expanded and automatically scans the current project package state into the checkboxes for UI Virtual List, UI Extras, Effects, and Debug Console. `Select All` selects every extension package so a new project can install all extensions in one pass. `Apply` writes checked extension packages into `Packages/manifest.json`; in a local framework checkout it uses sibling `file:` package references, and when Core is installed from a git URL with `?path=/packages/com.eframework.core`, it derives matching git dependencies for each selected extension package. Unchecking an already installed package does not remove it. The same list includes `DOTween Adapter`, which applies or removes the `EFRAME_USE_DOTWEEN` scripting define instead of adding a package dependency.

`Install Showcase` first installs `com.eframework.ui.virtual-list` and `com.eframework.debug-console`. If those packages were just added to the manifest or are still resolving, wait for Unity Package Manager import and script compilation to finish, then click `Install Showcase` again. Once the required packages are available, the action copies the module template, repairs the installed virtual-list prefab script reference against the copied `VirtualListShowcaseWindow.cs.meta`, refreshes assets, syncs managed Addressables, and sets the StartUp entrance to `GameApp.Modules.EFrameExtensionShowcase.Procedure.ProcedureEFrameExtensionShowcaseEntry`.

For framework template maintenance, open `test-fixtures/EFrameShowcaseUnity` directly in Unity and edit `Assets/Modules/EFrameExtensionShowcase/` there. Run `tools/Sync-EFrameShowcaseTemplate.ps1` before release to generate the package template; the script converts `.cs` to `.cs.txt` while preserving module resources and `.meta` files.

The current showcase UI is backed by `Assets/Modules/EFrameExtensionShowcase/Res/UI/Panels/EFrameExtensionShowcase/EFrameExtensionShowcaseView.prefab` and lists only UI Virtual List and Debug Console entries. UI Virtual List opens the module-owned `QVirtualListShowcaseWindow.prefab` sample window, covering variable-size lists, grids, scrolling, reload, refresh, pooling, and visible-range reporting. Debug Console opens the runtime console panel.

## Simple Game Demo

The Simple Game Demo is planned as a separate git repository. Do not add a full game demo to `com.eframework.core`.
