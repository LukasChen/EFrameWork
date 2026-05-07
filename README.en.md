# EFrame

[中文](README.md) | English

EFrame is a lightweight Unity game framework that provides business projects with a standard startup template, runtime infrastructure, editor initialization tooling, and a synchronized AI collaboration layer.

Framework maintenance flow: [README.maintainer.en.md](README.maintainer.en.md).

The current framework version is defined in [packages/com.eframework.core/package.json](packages/com.eframework.core/package.json).

## Quick Start

Install the Core package through Unity Package Manager:

```text
https://github.com/ethanhubin/EFrame.git?path=/packages/com.eframework.core
```

Open this entry in Unity Editor:

```text
EFrame Tools/项目初始化向导
-> Initialize / Repair Project
```

This copies or repairs the Basic startup template, syncs the EFrame AI workspace, installs the project-side AI updater, and repairs Addressables, UI sorting layers, Audio, fallback tween readiness, and Build Settings.

For command-line cold start:

```powershell
.\tools\Initialize-EFrameColdStart.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

Command-line cold start creates the directory scaffold and Basic template by default, but does not automatically sync the AI workspace. Add this flag when you also want AI sync:

```powershell
.\tools\Initialize-EFrameColdStart.ps1 -TargetRoot "D:\YourUnityProject" -Force -IncludeAIWorkspace -AIClients all
```

When using only the installed package, run the equivalent script from the package root:

```powershell
.\Packages\com.eframework.core\Tools~\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

After sync, Codex, GitHub Copilot, and Claude Code can trigger EFrame AI workflows directly from business requests. The AI follows the EFrame managed block into the synced `eframe-*` instructions, skills, and API index, then edits code or reports review findings according to framework conventions.

Example prompts:

- `Build an EFrame Settings popup.`
  Result: the AI creates the popup View, Controller, and binding code, then wires open and close through the `QUI` lifecycle.
- `Add a PlayerProfile data table.`
  Result: the AI creates the table integration, with load, change tracking, and save flow handled by EFrame conventions.
- `Make the avatar an EFrame-managed resource and load it through ResPath.`
  Result: the AI places the asset, prompts Addressables / `ResPath.Generated` sync, and switches code to the generated load entry.
- `Run an EFrame guideline audit.`
  Result: the AI checks common UI, resource, data table, and AI sync issues, then points to the needed fixes.

## Repository Structure

- [packages/com.eframework.core](packages/com.eframework.core): Core runtime/editor package, including `EFrame`, `Procedure`, `QUI`, UI controllers and handles, assets, audio, data, events, and project initialization tooling.
- [packages/com.eframework.core/Tools~](packages/com.eframework.core/Tools~): Package-shipped project initialization, AI sync, and health check scripts.
- [packages/com.eframework.core/Documentation~](packages/com.eframework.core/Documentation~): Human-facing usage docs, maintainer docs, and HTML docs site.
- [packages/com.eframework.ai-loop](packages/com.eframework.ai-loop), [packages/com.eframework.ui.virtual-list](packages/com.eframework.ui.virtual-list), [packages/com.eframework.ui-extras](packages/com.eframework.ui-extras), [packages/com.eframework.effects](packages/com.eframework.effects), [packages/com.eframework.debug-console](packages/com.eframework.debug-console): Optional extension packages.

## Core Capabilities

EFrame Core provides stable runtime infrastructure and the minimal runnable startup skeleton:

- `EFrameContext` and `EFrame.*` shortcuts
- Procedure startup flow and Basic `StartUp.unity`
- `QUI`, UI prefab binding, UIController, UIViewHandle, UI layers, and transitions
- Addressables managed resources, `ResPath.Generated`, and pre-build checks
- Audio playback, mixer volume, SFX pooling, debounce, and music crossfade
- Data tables, events, coroutines, resource loading, and common utility services
- Project initialization wizard, Addressables repair, UI binding, Scroller, and other editor tools

Common runtime entries:

```csharp
EFrame.UI
EFrame.Audio
EFrame.Data
EFrame.Assets
```

Views, controllers, and `EFrameBehaviour` components created by the framework receive `Context` automatically. Business startup code, static entry points, and other non-injected call sites can use shortcuts such as `EFrame.UI` and `EFrame.Audio`; inside framework-aware types, prefer `Context.UI` and `Context.Audio`.

## Project Conventions

EFrame treats these directories as framework-managed resource directories:

- `Assets/App/Res`
- `Assets/Scenes`
- `Assets/Modules/<ModuleName>/Res`
- `Assets/Modules/<ModuleName>/Scenes`

When resources are imported, moved, or deleted, editor automation maintains the Addressables group, address, and `eframe-managed` label. Runtime code should prefer generated `ResPath.Generated` members instead of handwritten Addressables strings or resource path wrappers.

Open this menu for manual resource checks or repair:

```text
EFrame Tools/Addressables/Sync Groups And Generate ResPath
```

## Basic And Optional Showcase

Basic is the Core-owned minimal runnable project template. It creates or repairs:

- `Assets/Scenes/StartUp.unity`
- `ProcedureLauncher` / `ProcedureHome`
- `HomeView` / `HomeViewController`
- `Assets/App/Res/UI/Panels/Home/HomeView.prefab`
- Addressables groups and `ResPath.Generated`
- UI sorting layers, basic audio resources, and fallback tween readiness

Extension Showcase is an optional business module installed to `Assets/Modules/EFrameExtensionShowcase/`. It demonstrates UI Virtual List and Debug Console.

## AI Collaboration Layer

EFrame's AI collaboration layer syncs framework conventions into business project AI client entries, giving Codex, GitHub Copilot, and Claude Code consistent EFrame coding guidance.

To check or sync only the AI workspace:

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -StatusOnly
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

Install the project-side updater:

```powershell
.\tools\Install-EFrameAIProjectUpdater.ps1 -TargetRoot "D:\YourUnityProject"
```

After sync, a business project can continue updating itself with:

```powershell
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -StatusOnly
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -Force
```

## Dependencies And Extensions

The Core package declares these Unity dependencies:

- Addressables
- UGUI
- Input System
- Universal Render Pipeline
- Unity Newtonsoft.Json

`com.eframework.core` requires URP. `QUI` uses URP overlay-camera APIs to bind the persistent UI camera into the active `EFrameSceneCamera` stack, so consuming projects should configure a URP pipeline asset.

TextMeshPro functionality is provided by `com.unity.ugui`. Projects should not add the old standalone `com.unity.textmeshpro` package or a framework-local TMP copy.

Core uses the `EFrameTween` fallback backend by default. DOTween is an optional project plugin and is not distributed with the package. Enable it through:

```text
EFrame Tools/项目初始化向导
-> Extensions
-> DOTween Adapter
-> Apply
```

Enabling the adapter adds the `EFRAME_USE_DOTWEEN` scripting define and creates or opens `Assets/Resources/DOTweenSettings.asset`. Disable the adapter before removing DOTween.

## Documentation

- Usage docs: [packages/com.eframework.core/Documentation~/user](packages/com.eframework.core/Documentation~/user)
- UI framework guide: [UI_FRAMEWORK_GUIDE.md](packages/com.eframework.core/Documentation~/user/UI_FRAMEWORK_GUIDE.md)
- Directory structure convention: [UNITY_DIRECTORY_STRUCTURE.md](packages/com.eframework.core/Documentation~/user/UNITY_DIRECTORY_STRUCTURE.md)
- Samples and initialization: [SAMPLES_AND_INITIALIZATION.md](packages/com.eframework.core/Documentation~/user/SAMPLES_AND_INITIALIZATION.md)
- Resource path convention: [RESPATH_CONVENTION.md](packages/com.eframework.core/Documentation~/user/RESPATH_CONVENTION.md)
- API HTML index: [Documentation~/api/index.html](packages/com.eframework.core/Documentation~/api/index.html)
- Docs site entry: [Documentation~/index.html](packages/com.eframework.core/Documentation~/index.html)
- Maintainer entry: [README.maintainer.en.md](README.maintainer.en.md)
