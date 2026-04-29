# EFrameWork

EFrameWork is a lightweight Unity game framework with a synchronized AI collaboration layer. It provides runtime/editor code, project bootstrap tools, AI instructions, skills, and update workflows so new Unity projects can start with both framework conventions and AI coding guidance in one step.

In this repository, the AI layer is a first-class framework feature, not an optional documentation bundle. When EFrame runtime, editor tooling, bootstrap templates, directory rules, resource conventions, or startup flow change, the matching `AGENTS.md`, `.github` instructions, skills, manifest, sync scripts, and setup docs must be reviewed and updated together.

## Framework Layers

- `packages/com.eframework.core/`: Unity runtime and editor package, including `EFrame`, `Procedure`, `QUI`, UI controllers, assets, audio, data, and bootstrap editor tooling.
- `AGENTS.md`: Codex-compatible repo entry point that mirrors the EFrame AI contract and points Codex to the synced workflow skills.
- `.github/`: framework-managed AI collaboration layer, including Copilot instructions, synced `eframe-*` instructions/skills, maintainer-only `maintainer-*` guidance, and the AI manifest.
- `tools/`: project cold-start and AI sync toolchain for importing, updating, and extending the framework AI layer in business projects.

See [EFRAME_AI_ARCHITECTURE.md](EFRAME_AI_ARCHITECTURE.md) for the AI layer contract and maintenance rules.

For the current handle-first UI runtime structure and usage examples, see [UI_FRAMEWORK_GUIDE.md](UI_FRAMEWORK_GUIDE.md).

## Versioning

The formal starting version for EFrameWork is `0.1.0`.

- EFrameWork release version: `packages/com.eframework.core/package.json` is the canonical product version for Unity Package Manager consumers and for the framework as a whole.
- EFrameWork release history: [CHANGELOG.md](CHANGELOG.md) records repository-level releases, including package code, AI collaboration rules, tools, and documentation changes.
- Package release history: [packages/com.eframework.core/CHANGELOG.md](packages/com.eframework.core/CHANGELOG.md) records package-specific changes.
- AI workspace manifest: `.github/eframe-ai.manifest.json` is an internal sync marker used by business projects to detect stale synced AI rules. It is not a separate product version.

Use semantic versioning for EFrameWork releases. Before `1.0.0`, minor versions may still include breaking changes, but they must be clearly documented in the changelog. Treat Unity code, AI collaboration rules, bootstrap tools, and sync scripts as one release surface.

## AI Workspace Support

Unity projects that adopt EFrameWork should receive both the Unity framework structure and the synced AI workspace layer.

- Codex entry rules live in `AGENTS.md`; Copilot and synced workflow rules live under `.github/`
- Synced business-project rules and workflows use the `eframe-*` prefix; framework-repository maintenance rules and workflows use `maintainer-*` and are not synced to projects.
- High-value synced skills cover feature bootstrap, guideline audit, UI features, data tables, and resource flows.
- Sync and installer scripts live under `tools/`
- Setup and upgrade flow is documented in [EFRAME_AI_SETUP.md](EFRAME_AI_SETUP.md)
- AI release and manifest rules are documented in [EFRAME_AI_RELEASE_CHECKLIST.md](EFRAME_AI_RELEASE_CHECKLIST.md)

To sync the framework AI layer into a project root:

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Force
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

Inside Unity Editor, you can also open `EFrame Tools/项目初始化向导` to create `StartUp.unity`, the `Boot` object structure, and trigger AI/bootstrap initialization from a single window.

## DOTween Dependency

`com.eframework.core` uses DOTween directly in several runtime components. Consumer projects should install DOTween into the project `Assets` before using tween-enabled EFrameWork features.

- Recommended: install DOTween as a normal project plugin under `Assets`
- Then use `EFrame Tools/项目初始化向导` to create or open `Assets/Resources/DOTweenSettings.asset`
- Do not rely on configuring DOTween through a package-local copy

## TextMeshPro Dependency

`com.eframework.core` also uses TextMeshPro directly and now expects the official Unity package dependency `com.unity.textmeshpro` instead of a framework-bundled copy.

## Runtime Access

Runtime services are accessed through `EFrame.Current`, which returns the active `EFrameContext`.

```csharp
EFrame.Current.UI
EFrame.Current.Audio
EFrame.Current.Data
EFrame.Current.Assets
```

Framework base classes receive `Context` automatically. Prefer `Context.UI`/`Context.Audio` inside views and controllers, and use `EFrame.Current` only at outer Unity entry points.
