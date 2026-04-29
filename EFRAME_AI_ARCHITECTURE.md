# EFrame AI Architecture

EFrameWork is maintained as a Unity framework plus a synchronized AI collaboration layer. The AI layer is part of the framework contract: it carries the coding rules, project structure rules, skills, and upgrade flow that let Copilot work inside EFrame projects without rediscovering the conventions every time.

## 1. Design Goal

The goal is to make every new EFrame project start with both working Unity scaffolding and matching AI guidance.

Cold-start and upgrade flows must keep these parts aligned:

- Unity package code under `packages/com.eframework.core/`
- Codex entry guidance in `AGENTS.md`
- Framework AI files under `.github/`
- Sync and bootstrap scripts under `tools/`
- Directory, resource, and release docs at the repository root

When one part changes the expected project shape, the matching AI guidance must be reviewed in the same change set.

## 2. Layer Responsibilities

`packages/com.eframework.core/`

- Provides the runtime and editor implementation.
- Owns framework services such as `EFrame`, `EFrameContext`, `QUI`, asset loading, audio, data, events, and editor bootstrap utilities.
- Should not rely on project-specific AI overlay rules.

`.github/`

- Provides the framework-managed AI collaboration layer.
- `copilot-instructions.md` contains always-on EFrame rules.
- `instructions/eframe-*.instructions.md` contains short, stable runtime and editor rules that are part of the synced business-project contract.
- `skills/eframe-*` contains on-demand business-project workflows that are also synced into business projects.
- `instructions/maintainer-*.instructions.md` and `skills/maintainer-*` contain framework-repository-only guidance and workflows; these files are not part of the synced project contract because the sync scripts only manage `eframe-*` items.
- `eframe-ai.manifest.json` declares the synced AI layer version.

`AGENTS.md`

- Provides the repo-root Codex entry point.
- Mirrors the stable EFrame AI contract at a high level and directs Codex to the relevant `.github/skills/*/SKILL.md` workflow files when a task matches a synced or maintainer workflow.
- Is synced to business projects together with the AI docs so Codex and Copilot can share the same framework guidance from different entry points.

`tools/`

- Imports and updates the AI layer in business projects.
- Creates cold-start project structure and bootstrap code.
- Installs project-side sync scripts.
- Generates project overlay templates without overwriting local project rules.

Business project `.github/`

- Receives synced `eframe-*` files from the framework.
- Owns `project-*` overlay files for local project rules.
- Must not manually fork framework-managed `eframe-*` files.
- Does not receive framework maintainer-only `maintainer-*` instructions or skills.

## 3. Required Sync Rule

Any change in the following areas must include an AI layer impact check:

- Startup scene structure, `Boot`, or `EFrameComponent` initialization
- `Procedure` lifecycle templates or responsibilities
- `QUI`, `UIController`, UI prefab, or layer management rules
- `ResPath`, Addressables group rules, or resource directory conventions
- Cold-start scripts, bootstrap code templates, sample modules, or editor initialization windows
- Directory structure docs or release/setup docs

If the change affects how Copilot or Codex should generate, refactor, or audit EFrame projects, update the relevant `AGENTS.md`, `.github` instruction, or skill in the same change set. Keep always-on boundaries in instructions; put multi-step workflows, audits, and detailed checklists in skills or skill references.

## 4. UI Runtime Contract Sync

The current UI runtime contract is handle-first:

- Runtime UI lifecycle flows through `IUIService/QUI` and `UIViewHandle<TView>`.
- View wrappers stay thin, use parameterless construction, and receive prefab state through `SetBinding()` / `OnBindingSet()`.
- Controllers access live views through `TypedViewHandle.TypedView`; the legacy `UIControllerBase<TView>.View` facade must not reappear in generated code.
- Controller lifecycle hooks are split by scope: `OnViewCreated()` / `OnViewDestroyed()` are instance-level, while `OnViewOpened()` / `OnViewClosed()` are per-open/per-close.
- Host-owned transitions must tolerate interruption so stale async open/close completions cannot overwrite newer handle state.

Any framework change that alters this UI contract must update the same contract surface in one change set:

- `UI_FRAMEWORK_GUIDE.md`
- `.github/instructions/eframe-runtime.instructions.md`
- `.github/skills/eframe-ui-feature`
- `.github/skills/eframe-guideline-audit`
- bootstrap/editor template generators under `tools/` and `packages/com.eframework.core/Editor/`
- root and package changelogs

## 5. Unified Versioning Contract

EFrameWork should be maintained as one product surface, not as a Unity framework plus a second AI product. The AI collaboration layer, bootstrap tools, and sync scripts are part of the framework release contract.

- EFrameWork release version: stored in `packages/com.eframework.core/package.json` and recorded in the root `CHANGELOG.md`.
- AI workspace manifest version: stored in `.github/eframe-ai.manifest.json` only as a sync marker so business projects can detect stale framework-managed AI files.

For the formal starting release, both values start at `0.1.0`. After that, think in terms of the EFrameWork release first. Bump the package version when publishing a framework/package release. Bump the manifest when the synced AI rules, AI docs, sync scripts, or cold-start tooling change, but do not describe it as a separate product version.

Whenever `AGENTS.md`, `.github/copilot-instructions.md`, `.github/instructions/`, `.github/skills/`, AI sync scripts, cold-start scripts, or AI setup docs change, bump `.github/eframe-ai.manifest.json`.

The manifest version is only the signal business projects use to detect whether their synced AI layer is stale.

Recommended release sequence:

1. Update the framework implementation or docs.
2. Update matching instructions and skills.
3. Bump `packages/com.eframework.core/package.json` if this is a framework/package release.
4. Update the root `CHANGELOG.md`; update `packages/com.eframework.core/CHANGELOG.md` if package code changed.
5. Bump `.github/eframe-ai.manifest.json` if AI rules, AI docs, sync scripts, or cold-start tooling changed.
6. Run `tools/Test-EFrameAIRelease.ps1`.
7. Verify `Initialize-EFrameAI.ps1 -StatusOnly` and `-Force`.
8. Verify cold-start or editor bootstrap paths affected by the change.

## 6. Naming Boundary

- `eframe-*` is reserved for framework-managed instructions and skills.
- `project-*` is reserved for business project overlays.
- `maintainer-*` is reserved for framework-repository-only instructions and skills and is not part of the sync surface.
- Framework sync scripts may overwrite stale `eframe-*` files when `-Force` is used.
- Framework sync scripts must preserve project-owned `project-*` files and other local customizations.

This boundary keeps framework upgrades repeatable while still allowing each business project to add local rules.
