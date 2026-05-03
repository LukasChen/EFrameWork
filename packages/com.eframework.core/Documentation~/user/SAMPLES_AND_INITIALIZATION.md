# EFrame Samples And Initialization

EFrame uses three layers:

- Core: `com.eframework.core`, stable runtime/editor infrastructure and the minimal startup skeleton.
- Extension: optional packages such as `com.eframework.ui.virtual-list`, `com.eframework.ui-extras`, `com.eframework.effects`, `com.eframework.gm-tools`, and `com.eframework.debug-console`.
- Samples: project skeletons or modules installed into a consuming Unity project when they are useful.

## Basic

Basic is the core-owned minimal runnable project skeleton. It is the maintained version of the current initialization template and does not reference optional extension packages.

Install it from Unity:

```text
EFrame Tools/项目初始化向导
-> Initialize / Repair Project
```

The initializer copies the package Basic template and then repairs Addressables, UI sorting layers, audio, fallback tween readiness, and build settings.

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
-> Install Showcase
```

The installer copies the module template, refreshes assets, syncs managed Addressables, sets the StartUp entrance to `GameApp.Modules.EFrameExtensionShowcase.Procedure.ProcedureEFrameExtensionShowcaseEntry`, and writes local sibling extension packages into `Packages/manifest.json` as `file:` package references when this repository is used as a local framework checkout. If sibling packages are not found, the project can still keep the module and install extensions manually from git or UPM.

For framework template maintenance, open `test-fixtures/EFrameShowcaseUnity` directly in Unity and edit `Assets/Modules/EFrameExtensionShowcase/` there. Run `tools/Sync-EFrameShowcaseTemplate.ps1` before release to generate the package template; the script converts `.cs` to `.cs.txt` while preserving module resources and `.meta` files.

The current showcase UI is backed by `Assets/Modules/EFrameExtensionShowcase/Res/UI/Panels/EFrameExtensionShowcase/EFrameExtensionShowcaseView.prefab` and lists UI Virtual List, UI Extras, Effects, GMTools, and Debug Console entries. Each entry is a minimal runtime placeholder so future extension-specific demos can be added without changing the module shape.

## Simple Game Demo

The Simple Game Demo is planned as a separate git repository. Do not add a full game demo to `com.eframework.core`.
