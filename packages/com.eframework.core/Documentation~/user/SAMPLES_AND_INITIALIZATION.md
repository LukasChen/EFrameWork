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
-> Full Initialize Project
```

or, for the template portion only:

```text
EFrame Tools/项目初始化向导
-> Install Basic Sample / Import Basic Template
```

Basic creates or prepares:

- `Assets/Scenes/StartUp.unity`
- `Assets/App/Runtime/Procedure/ProcedureLauncher.cs`
- `Assets/App/Runtime/Procedure/ProcedureHome.cs`
- `Assets/App/Runtime/UI/Views/HomeView.cs`
- `Assets/App/Runtime/UI/Controllers/HomeViewController.cs`
- `Assets/App/Res/UI/Panels/Home/HomeView.prefab`
- `Assets/Modules/SampleModule/...`
- Addressables groups and generated `ResPath`
- UI sorting layers
- basic audio resources and fallback tween backend readiness

The package sample under `Samples~/Basic` is documentation for package users. The initializer uses the machine template under `Editor/Templates/Basic` plus the existing UI prefab templates under `Editor/Templates/UI`.

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

The installer copies the module template, refreshes assets, syncs managed Addressables, and tries to add local sibling extension packages with `file:` package references when this repository is used as a local framework checkout. If sibling packages are not found, the project can still keep the module and install extensions manually from git or UPM.

For framework template maintenance, open `test-fixtures/EFrameShowcaseUnity` directly in Unity and edit `Assets/Modules/EFrameExtensionShowcase/` there. Run `tools/Sync-EFrameShowcaseTemplate.ps1` before release to generate the package template; the script converts `.cs` to `.cs.txt` while preserving module resources and `.meta` files. In a consuming project, `Refresh Showcase From Template` replaces the installed `Assets/Modules/EFrameExtensionShowcase/` copy after confirmation, refreshes assets, and re-syncs managed Addressables.

Use these buttons to control startup:

- `Set Showcase Startup`: sets the StartUp scene `EFrameProcedureComponent` entrance to `GameApp.Modules.EFrameExtensionShowcase.Procedure.ProcedureEFrameExtensionShowcaseEntry`.
- `Restore Basic Startup`: restores the entrance to `GameApp.Procedure.ProcedureLauncher`.

The current showcase UI lists UI Virtual List, UI Extras, Effects, GMTools, and Debug Console entries. Each entry is a minimal runtime placeholder so future extension-specific demos can be added without changing the module shape.

## Simple Game Demo

The Simple Game Demo is planned as a separate git repository. Do not add a full game demo to `com.eframework.core`.
