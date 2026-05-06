# EFrame

[中文](README.md) | English

EFrame is a lightweight Unity game framework with a synchronized AI collaboration layer. It keeps runtime code, editor tooling, project initialization templates, AI instructions and skills, sync scripts, and maintenance checks in one release surface so new business projects can start with both a standard Unity project structure and AI coding guidance that matches EFrame conventions.

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

To check or sync only the AI workspace:

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -StatusOnly
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

Package-only consumers can run the equivalent script from the installed package root:

```powershell
.\Packages\com.eframework.core\Tools~\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

## Repository Structure

- [packages/com.eframework.core](packages/com.eframework.core): Core runtime/editor package, including `EFrame`, `Procedure`, `QUI`, UI controllers and handles, assets, audio, data, events, and project initialization tooling.
- [packages/com.eframework.core/AIWorkspace~](packages/com.eframework.core/AIWorkspace~): Package-shipped AI collaboration source, including `eframe-*` instructions and skills, managed blocks, manifest, and AI support docs.
- [packages/com.eframework.core/Tools~](packages/com.eframework.core/Tools~): Package-shipped project initialization, AI sync, and health check scripts.
- [packages/com.eframework.core/Documentation~](packages/com.eframework.core/Documentation~): Human-facing user docs, maintainer docs, and HTML docs site.
- [packages/com.eframework.ui.virtual-list](packages/com.eframework.ui.virtual-list), [packages/com.eframework.ui-extras](packages/com.eframework.ui-extras), [packages/com.eframework.effects](packages/com.eframework.effects), [packages/com.eframework.debug-console](packages/com.eframework.debug-console): Optional extension packages.
- [test-fixtures](test-fixtures): Unity fixtures used to validate the Basic template, Showcase module, and package consumer paths.
- [tools](tools): Framework repository maintenance scripts and wrappers around package scripts. Business projects should prefer package-local `Tools~/`.
- [AGENTS.md](AGENTS.md), [CLAUDE.md](CLAUDE.md), [.github/copilot-instructions.md](.github/copilot-instructions.md): AI client entries for this framework repository. They only route to the right contracts and do not duplicate business rules.

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

## Basic And Showcase

Basic is the Core-owned minimal runnable project template. It creates or repairs:

- `Assets/Scenes/StartUp.unity`
- `ProcedureLauncher` / `ProcedureHome`
- `HomeView` / `HomeViewController`
- `Assets/App/Res/UI/Panels/Home/HomeView.prefab`
- Addressables groups and `ResPath.Generated`
- UI sorting layers, basic audio resources, and fallback tween readiness

To maintain the Basic template, edit [test-fixtures/EFrameBasicTemplate](test-fixtures/EFrameBasicTemplate), then sync it back into the package template:

```powershell
.\tools\Sync-EFrameBasicTemplate.ps1
.\tools\Sync-EFrameBasicTemplate.ps1 -CheckOnly
```

Extension Showcase is an optional business module installed to `Assets/Modules/EFrameExtensionShowcase/`. It demonstrates UI Virtual List and Debug Console. To maintain Showcase, edit [test-fixtures/EFrameShowcaseUnity](test-fixtures/EFrameShowcaseUnity), then sync it back into the package template:

```powershell
.\tools\Sync-EFrameShowcaseTemplate.ps1
.\tools\Sync-EFrameShowcaseTemplate.ps1 -CheckOnly
```

## AI Collaboration Layer

EFrame's AI collaboration layer is part of the framework release contract, not a separate documentation bundle.

- The business-project sync source lives in [packages/com.eframework.core/AIWorkspace~](packages/com.eframework.core/AIWorkspace~).
- Framework-managed files synced into business projects use the `eframe-*` prefix.
- Project entry files for Codex, GitHub Copilot, and Claude Code are owned by the business project; EFrame only updates the EFrame managed block inside those entries.
- AI-facing support docs live in `AIWorkspace~/support-docs/`; the current API index is [EFRAME_AI_API_INDEX.md](packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md).
- Framework maintainer-only rules live in [tools/MaintainerAIWorkspace](tools/MaintainerAIWorkspace) and are not synced into business projects.

Install the project-side updater:

```powershell
.\tools\Install-EFrameAIProjectUpdater.ps1 -TargetRoot "D:\YourUnityProject"
```

After sync, a business project can continue updating itself with:

```powershell
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -StatusOnly
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -Force
```

Check a synced project:

```powershell
.\tools\Test-EFrameAIProject.ps1 -TargetRoot "D:\YourUnityProject" -FrameworkRoot "."
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

## Maintenance Checks

Before release, run at least:

```powershell
.\tools\Test-EFrameAIRelease.ps1
```

Changes touching Basic, Showcase, AI sync, resource directories, startup flow, UI runtime contract, or manifest should also run the matching sync scripts and project checks according to their impact.

AI release and manifest rules are documented in [tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md](tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md).

## Documentation

- User docs: [packages/com.eframework.core/Documentation~/user](packages/com.eframework.core/Documentation~/user)
- UI framework guide: [UI_FRAMEWORK_GUIDE.md](packages/com.eframework.core/Documentation~/user/UI_FRAMEWORK_GUIDE.md)
- Directory structure convention: [UNITY_DIRECTORY_STRUCTURE.md](packages/com.eframework.core/Documentation~/user/UNITY_DIRECTORY_STRUCTURE.md)
- Samples and initialization: [SAMPLES_AND_INITIALIZATION.md](packages/com.eframework.core/Documentation~/user/SAMPLES_AND_INITIALIZATION.md)
- Resource path convention: [RESPATH_CONVENTION.md](packages/com.eframework.core/Documentation~/user/RESPATH_CONVENTION.md)
- AI architecture: [EFRAME_AI_ARCHITECTURE.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_ARCHITECTURE.md)
- AI setup: [EFRAME_AI_SETUP.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_SETUP.md)
- API HTML index: [Documentation~/api/index.html](packages/com.eframework.core/Documentation~/api/index.html)
- Docs site entry: [Documentation~/index.html](packages/com.eframework.core/Documentation~/index.html)

## Versioning

EFrame uses semantic versioning. The formal starting version is `0.1.0`; the current release version is recorded in [packages/com.eframework.core/package.json](packages/com.eframework.core/package.json), and repository-level releases are recorded in [CHANGELOG.md](CHANGELOG.md).

Unity code, AI collaboration rules, bootstrap tools, sync scripts, and docs are treated as one EFrame release surface. `AIWorkspace~/eframe-ai.manifest.json` is only an internal marker used by business projects to detect synced file drift; it is not a separate product version.
