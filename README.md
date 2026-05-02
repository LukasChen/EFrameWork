# EFrameWork

EFrameWork is a lightweight Unity game framework with a synchronized AI collaboration layer. It provides runtime/editor code, project bootstrap tools, AI instructions, skills, and update workflows so new Unity projects can start with both framework conventions and AI coding guidance in one step.

In this repository, the AI layer is a first-class framework feature, not an optional documentation bundle. Business-facing AI rules, docs, and sync tools ship inside `packages/com.eframework.core/`; maintainer-only docs and release checks stay outside the package. When EFrame runtime, editor tooling, bootstrap templates, directory rules, resource conventions, or startup flow change, the matching package AI workspace, package docs, client entry files, sync scripts, and maintainer docs must be reviewed together.

## Framework Layers

- `packages/com.eframework.core/`: Unity runtime and editor package, including `EFrame`, `Procedure`, `QUI`, UI controllers, assets, audio, data, and bootstrap editor tooling.
- `AGENTS.md`: Codex-compatible repo entry point that mirrors the EFrame AI contract and points Codex to the synced workflow skills.
- `.github/`: system-required GitHub entry files, currently the framework repository Copilot instructions.
- `packages/com.eframework.core/AIWorkspace~/`: package-shipped AI collaboration source layer, including synced `eframe-*` instructions/skills, managed block sources, support docs declared by the manifest, and the AI manifest.
- `packages/com.eframework.core/Tools~/`: package-shipped project cold-start and AI sync toolchain for importing, updating, and extending the framework AI layer in business projects.
- `tools/`: maintainer-side wrappers and release checks used while developing this repository; business projects should rely on package-shipped `Tools~/` when package-only installed.

See [docs/maintainer/EFRAME_AI_ARCHITECTURE.md](docs/maintainer/EFRAME_AI_ARCHITECTURE.md) for the AI layer contract and maintenance rules.

For the current handle-first UI runtime structure and usage examples, see [packages/com.eframework.core/Documentation~/UI_FRAMEWORK_GUIDE.md](packages/com.eframework.core/Documentation~/UI_FRAMEWORK_GUIDE.md).

For a compact AI-oriented map of stable framework APIs and tool entry points, see [packages/com.eframework.core/Documentation~/EFRAME_AI_API_INDEX.md](packages/com.eframework.core/Documentation~/EFRAME_AI_API_INDEX.md).

For a framework-level API catalog covering runtime and editor public surfaces, see [docs/api/index.html](docs/api/index.html).

For the new browsable HTML docs site skeleton, open [docs/index.html](docs/index.html).

## Versioning

The formal starting version for EFrameWork is `0.1.0`.

- EFrameWork release version: `packages/com.eframework.core/package.json` is the canonical product version for Unity Package Manager consumers and for the framework as a whole.
- EFrameWork release history: [CHANGELOG.md](CHANGELOG.md) records repository-level releases, including package code, AI collaboration rules, tools, and documentation changes.
- Package release history: [packages/com.eframework.core/CHANGELOG.md](packages/com.eframework.core/CHANGELOG.md) records package-specific changes.
- AI workspace manifest: `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` is an internal sync marker used by business projects to detect stale synced AI rules. Business projects receive it at `.github/eframe-ai.manifest.json`; it is not a separate product version.

Use semantic versioning for EFrameWork releases. Before `1.0.0`, minor versions may still include breaking changes, but they must be clearly documented in the changelog. Treat Unity code, AI collaboration rules, bootstrap tools, and sync scripts as one release surface.

## AI Workspace Support

Unity projects that adopt EFrameWork should receive both the Unity framework structure and the synced AI workspace layer.

- Framework repo AI entry files are `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md`; they are thin indexes that point to package business contracts and maintainer docs.
- Business-project AI sync source lives in `packages/com.eframework.core/AIWorkspace~/`; package-only consumers receive the same syncable `eframe-*` rules and manifest.
- Synced business-project rules and workflows use the `eframe-*` prefix; synced support docs are explicitly declared in the manifest; framework-repository maintenance rules and workflows live under `tools/MaintainerAIWorkspace/` and are not synced to projects.
- High-value synced skills cover feature bootstrap, guideline audit, UI features, data tables, and resource flows.
- Package-shipped sync and installer scripts live under `packages/com.eframework.core/Tools~/`; root `tools/` keeps maintainer wrappers for this repository.
- Setup and upgrade flow is documented in [docs/maintainer/EFRAME_AI_SETUP.md](docs/maintainer/EFRAME_AI_SETUP.md)
- AI release and manifest rules are documented in [docs/maintainer/EFRAME_AI_RELEASE_CHECKLIST.md](docs/maintainer/EFRAME_AI_RELEASE_CHECKLIST.md)

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

To generate the minimal startup Procedure/bootstrap placeholder code only:

```powershell
.\tools\Initialize-EFrameBootstrapCode.ps1 -TargetRoot "D:\YourUnityProject" -RootNamespace "YourGame"
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

Inside Unity Editor, you can also open `EFrame Tools/项目初始化向导` to create `StartUp.unity`, the `Boot` object structure, run Unity bootstrap initialization, choose an AI platform, check AI sync status, sync the AI workspace, and run the AI health check from a single window. The same AI actions are available from `EFrame Tools/AI`.

## Managed Resource Convention

EFrameWork treats resources under `Assets/App/Res`, `Assets/Scenes`, and `Assets/Modules` as framework-managed Addressables content. Put assets in the mapped directory and let the editor automation own the Addressables group, address, and `eframe-managed` label.

- Importing, moving, or deleting managed resources automatically syncs Addressables groups.
- `ResPath.Generated` is generated from resources under the directory trees selected in the managed resource report window.
- Player builds run an EFrame Addressables preflight and fail if managed entries, addresses, labels, or generated paths are stale.
- Use `EFrame Tools/Addressables/Sync Groups And Generate ResPath` to open the managed resource report window, choose ResPath source directories, inspect Addressables-to-ResPath mappings, review directory convention issues, or run a manual repair/check.
- Runtime code should use `ResPath.Generated` instead of raw Addressables strings or handwritten resource path wrappers. `AssetReference` remains appropriate for inspector-authored editor fields, but runtime business calls should receive generated asset ids.

## DOTween Dependency

`com.eframework.core` uses DOTween directly in several runtime components. Consumer projects should install DOTween into the project `Assets` before using tween-enabled EFrameWork features.

- Recommended: install DOTween as a normal project plugin under `Assets`
- Then use `EFrame Tools/项目初始化向导` to create or open `Assets/Resources/DOTweenSettings.asset`
- Do not rely on configuring DOTween through a package-local copy

## URP Dependency

`com.eframework.core` requires the Universal Render Pipeline package. `QUI` binds the persistent UI camera into the active `EFrameSceneCamera` stack through URP overlay-camera APIs, so consuming projects should be URP projects.

## TextMeshPro Dependency

`com.eframework.core` also uses TextMeshPro directly. In current Unity versions, TextMeshPro functionality comes from `com.unity.ugui`, so consumer projects should depend on UGUI instead of the deprecated standalone `com.unity.textmeshpro` package.

## Runtime Access

Runtime services are accessed through `EFrame.Current`, which returns the active `EFrameContext`.

```csharp
EFrame.Current.UI
EFrame.Current.Audio
EFrame.Current.Data
EFrame.Current.Assets
```

Framework base classes receive `Context` automatically. Prefer `Context.UI`/`Context.Audio` inside views and controllers, and use `EFrame.Current` only at outer Unity entry points.
