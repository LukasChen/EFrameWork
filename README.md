# EFrame

EFrame is a lightweight Unity game framework with a synchronized AI collaboration layer. It provides runtime/editor code, project bootstrap tools, AI instructions, skills, and update workflows so new Unity projects can start with both framework conventions and AI coding guidance in one step.

In this repository, the AI layer is a first-class framework feature, not an optional documentation bundle. AI contracts, sync tools, and human-facing docs ship inside `packages/com.eframework.core/`; AI-facing contracts live under `AIWorkspace~`, while human docs live under `Documentation~`. When EFrame runtime, editor tooling, bootstrap templates, directory rules, resource conventions, or startup flow change, the matching package AI workspace, client entry files, sync scripts, and human docs must be reviewed together.

## Framework Layers

- `packages/com.eframework.core/`: Unity runtime and editor package, including `EFrame`, `Procedure`, `QUI`, UI controllers, assets, audio, data, and bootstrap editor tooling.
- `AGENTS.md`: Codex-compatible repo entry point that mirrors the EFrame AI contract and points Codex to the synced workflow skills.
- `.github/`: system-required GitHub entry files, currently the framework repository Copilot instructions.
- `packages/com.eframework.core/AIWorkspace~/`: package-shipped AI collaboration source layer, including synced `eframe-*` instructions/skills, managed block sources, support docs declared by the manifest, and the AI manifest.
- `packages/com.eframework.core/Tools~/`: package-shipped project cold-start and AI sync toolchain for importing, updating, and extending the framework AI layer in business projects.
- `tools/`: maintainer-side wrappers and release checks used while developing this repository; business projects should rely on package-shipped `Tools~/` when package-only installed.

See [packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_ARCHITECTURE.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_ARCHITECTURE.md) for the AI layer contract and maintenance rules.

For the current handle-first UI runtime structure and usage examples, see [packages/com.eframework.core/Documentation~/user/UI_FRAMEWORK_GUIDE.md](packages/com.eframework.core/Documentation~/user/UI_FRAMEWORK_GUIDE.md).

For human-facing docs, see [packages/com.eframework.core/Documentation~](packages/com.eframework.core/Documentation~). AI-facing sync support docs live under `packages/com.eframework.core/AIWorkspace~/support-docs/`.

For a framework-level API catalog covering runtime and editor public surfaces, see [packages/com.eframework.core/Documentation~/api/index.html](packages/com.eframework.core/Documentation~/api/index.html).

For the new browsable HTML docs site skeleton, open [packages/com.eframework.core/Documentation~/index.html](packages/com.eframework.core/Documentation~/index.html).

## Versioning

The formal starting version for EFrame is `0.1.0`.

- EFrame release version: `packages/com.eframework.core/package.json` is the canonical product version for Unity Package Manager consumers and for the framework as a whole.
- EFrame release history: [CHANGELOG.md](CHANGELOG.md) records repository-level releases, including package code, AI collaboration rules, tools, and documentation changes.
- Package release history: [packages/com.eframework.core/CHANGELOG.md](packages/com.eframework.core/CHANGELOG.md) records package-specific changes.
- AI workspace manifest: `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` is an internal sync marker used by business projects to detect stale synced AI rules. Business projects receive it at `.github/eframe-ai.manifest.json`; it is not a separate product version.

Use semantic versioning for EFrame releases. Before `1.0.0`, minor versions may still include breaking changes, but they must be clearly documented in the changelog. Treat Unity code, AI collaboration rules, bootstrap tools, and sync scripts as one release surface.

## AI Workspace Support

Unity projects that adopt EFrame should receive both the Unity framework structure and the synced AI workspace layer.

- Framework repo AI entry files are `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md`; they are thin indexes that point to package AI contracts and human docs.
- Business-project AI sync source lives in `packages/com.eframework.core/AIWorkspace~/`; package-only consumers receive the same syncable `eframe-*` rules and manifest.
- Synced business-project rules and workflows use the `eframe-*` prefix; synced support docs are explicitly declared in the manifest; framework-repository maintenance rules and workflows live under `tools/MaintainerAIWorkspace/` and are not synced to projects.
- High-value synced skills cover feature bootstrap, guideline audit, UI features, data tables, and resource flows.
- Package-shipped sync and installer scripts live under `packages/com.eframework.core/Tools~/`; root `tools/` keeps maintainer wrappers for this repository.
- Setup and upgrade flow is documented in [packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_SETUP.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_SETUP.md)
- AI release and manifest rules are documented in [tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md](tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md)

To sync the framework AI layer into a project root:

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

For package-only consumers, run the equivalent package script from the installed package root:

```powershell
.\Packages\com.eframework.core\Tools~\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

To cold-start a new project in one command:

```powershell
.\tools\Initialize-EFrameColdStart.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

To copy only the Basic startup template without AI sync:

```powershell
.\tools\Initialize-EFrameBootstrapCode.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

To install a project-side updater script:

```powershell
.\tools\Install-EFrameAIProjectUpdater.ps1 -TargetRoot "D:\YourUnityProject"
```

Before releasing AI-layer or bootstrap tooling changes, run:

```powershell
.\tools\Test-EFrameAIRelease.ps1
```

To inspect a business project after AI sync or framework upgrades, run:

```powershell
.\tools\Test-EFrameAIProject.ps1 -TargetRoot "D:\YourUnityProject" -FrameworkRoot "."
```

For package-only consumers, pass the installed package root as `-FrameworkRoot`.

Inside Unity Editor, you can also open `EFrame Tools/项目初始化向导` to copy or repair the Basic template, register `StartUp.unity`, run Unity bootstrap initialization, check AI sync status, sync the AI workspace, and run the AI health check from a single window. The same AI actions are available from `EFrame Tools/AI`.

## Test Fixtures

- `test-fixtures/EFrameConsumerUnity`: minimal consumer fixture used by package import and API compile checks.
- `test-fixtures/EFrameBasicTemplate`: runnable Unity fixture for editing and debugging the Basic startup template. Its `Assets` folder syncs into `packages/com.eframework.core/Editor/Templates/Basic`.
- `test-fixtures/EFrameShowcaseUnity`: runnable Unity fixture for editing and debugging `Assets/Modules/EFrameExtensionShowcase` with normal `.cs` files and module resources.

After changing the Basic or Showcase fixture, sync the package template before release:

```powershell
.\tools\Sync-EFrameBasicTemplate.ps1
.\tools\Sync-EFrameBasicTemplate.ps1 -CheckOnly
.\tools\Sync-EFrameShowcaseTemplate.ps1
.\tools\Sync-EFrameShowcaseTemplate.ps1 -CheckOnly
```

## Managed Resource Convention

EFrame treats resources under `Assets/App/Res`, `Assets/Scenes`, and `Assets/Modules` as framework-managed Addressables content. Put assets in the mapped directory and let the editor automation own the Addressables group, address, and `eframe-managed` label.

- Importing, moving, or deleting managed resources automatically syncs Addressables groups.
- `ResPath.Generated` is generated from resources under the directory trees selected in the managed resource report window.
- Player builds run an EFrame Addressables preflight and fail if managed entries, addresses, labels, or generated paths are stale.
- Use `EFrame Tools/Addressables/Sync Groups And Generate ResPath` to open the managed resource report window, choose ResPath source directories, inspect Addressables-to-ResPath mappings, review directory convention issues, or run a manual repair/check.
- Runtime code should use `ResPath.Generated` instead of raw Addressables strings or handwritten resource path wrappers. `AssetReference` remains appropriate for inspector-authored editor fields, but runtime business calls should receive generated asset ids.

## Tween Backend

`com.eframework.core` uses `EFrameTween` for framework-owned transitions, scroll movement, and audio fades. Core includes a fallback backend, so new Basic projects do not need DOTween.

DOTween can still be used as an optional runtime backend:

- Install DOTween as a normal project plugin under `Assets`
- Use `EFrame Tools/项目初始化向导` -> `Tween Backend` to detect DOTween and enable the adapter
- Enabling the adapter adds `EFRAME_USE_DOTWEEN` and creates or opens `Assets/Resources/DOTweenSettings.asset`
- Disable the adapter before removing DOTween from a project

## URP Dependency

`com.eframework.core` requires the Universal Render Pipeline package. `QUI` binds the persistent UI camera into the active `EFrameSceneCamera` stack through URP overlay-camera APIs, so consuming projects should be URP projects.

## TextMeshPro Dependency

`com.eframework.core` also uses TextMeshPro directly. In current Unity versions, TextMeshPro functionality comes from `com.unity.ugui`, so consumer projects should depend on UGUI instead of the deprecated standalone `com.unity.textmeshpro` package.

## Runtime Access

Runtime services are accessed through lightweight `EFrame.*` shortcuts. `EFrame.Current` still returns the active `EFrameContext` when the full service bundle is needed.

```csharp
EFrame.UI
EFrame.Audio
EFrame.Data
EFrame.Assets
```

Framework base classes receive `Context` automatically. Prefer `Context.UI`/`Context.Audio` inside framework-aware views and controllers, and use `EFrame.UI`/`EFrame.Audio` at startup, static entry points, or other non-injected call sites.
