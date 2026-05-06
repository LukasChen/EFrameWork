# EFrame AI Architecture

EFrame is maintained as a Unity framework plus a synchronized AI collaboration layer. The AI layer is part of the framework contract: it carries the coding rules, project structure rules, skills, and upgrade flow that let Copilot work inside EFrame projects without rediscovering the conventions every time.

## 1. Design Goal

The goal is to make every new EFrame project start with a copied Basic Unity template, then let the user sync the supported AI clients from the Unity Editor or package tools.

Cold-start and upgrade flows must keep these parts aligned:

- Unity package code under `packages/com.eframework.core/`
- Synced AI workspace source under `packages/com.eframework.core/AIWorkspace~/`
- System-required framework AI entry files such as `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md`
- Sync and bootstrap scripts under `tools/`
- Human-facing docs under `packages/com.eframework.core/Documentation~/`

When one part changes the expected project shape, the matching AI guidance must be reviewed in the same change set.

## 2. Layer Responsibilities

`packages/com.eframework.core/`

- Provides the runtime and editor implementation.
- Owns framework services such as `EFrame`, `EFrameContext`, `QUI`, asset loading, audio, data, events, and editor bootstrap utilities.
- Should not rely on project-specific AI rules.

`.github/`

- Contains only files that must live under `.github` to work in the framework repository, such as `copilot-instructions.md`.
- Is not the source directory for synced EFrame AI workspace files.

`packages/com.eframework.core/AIWorkspace~/`

- Provides the framework-managed AI collaboration source layer.
- `managed-blocks/eframe-*.md` contains the EFrame blocks injected into project-owned AI entry files.
- `instructions/eframe-instructions.md` contains the short, stable framework usage rules that are part of the synced business-project contract.
- `skills/eframe-*` contains on-demand business-project workflows that are synced into business projects.
- `eframe-ai.manifest.json` declares the synced AI layer version and file hashes for framework-managed AI files; business projects still receive this file at `.github/eframe-ai.manifest.json`.
- `support-docs/` contains AI-facing support documents that must sync to business projects but should not live under human documentation directories.

`AGENTS.md`

- Provides the framework repo-root Codex entry point for maintainers.
- Directs Codex to synced or maintainer workflow files when a task matches them.
- Is not copied wholesale into business projects. Business projects own their `AGENTS.md`; EFrame sync only injects or updates the marked EFrame managed block.

`CLAUDE.md`

- Provides the framework repo-root Claude Code entry point.
- Points Claude Code at the same shared EFrame API index, synced workspace source, and naming boundaries instead of duplicating the full contract.
- Is not copied wholesale into business projects. Business projects own their `CLAUDE.md`; EFrame sync only injects or updates the marked EFrame managed block.

`packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md`

- Provides a compact AI-oriented map of stable Runtime, Editor bootstrap, and AI tooling APIs.
- Is an AI-facing support document for business-project AI, not a release or manifest maintenance guide.
- Is synced into business projects as `.github/eframe/EFRAME_AI_API_INDEX.md` so selected AI clients can read the API map from the project workspace.
- Should favor stable project-facing entry points over internal implementation details.

`tools/`

- Imports and updates the AI layer in business projects.
- Creates cold-start project structure and copies the Basic startup template.
- Installs project-side sync scripts.
- Injects or updates EFrame managed blocks in project-owned AI entry files without overwriting local project rules.
- Checks business-project AI workspace health after sync or framework upgrades.
- Lets Unity Editor users sync the supported AI clients instead of coupling AI contract sync to cold-start.

Business project `.github/`

- Receives synced `eframe-*` files from the framework.
- Receives `.github/eframe/EFRAME_AI_API_INDEX.md` as a framework-managed support document.
- Owns root AI entry files and any local project rule files.
- Must not manually fork framework-managed `eframe-*` files.
- Receives EFrame managed blocks inside selected AI entry files; only text between the EFrame markers is framework-owned.
- Does not receive framework maintainer-only `maintainer-*` skills.
- Does not receive package human docs or maintainer AI workspace files such as `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md`.

## 3. Required Sync Rule

Any change in the following areas must include an AI layer impact check:

- Startup scene structure, `Boot`, or `EFrameComponent` initialization
- `Procedure` lifecycle templates or responsibilities
- `QUI`, `UIController`, UI prefab, or layer management rules
- `ResPath`, Addressables group rules, or resource directory conventions
- Cold-start scripts, Basic/Showcase templates, optional modules, or editor initialization windows
- Directory structure docs or release/setup docs

If the change affects how Copilot, Codex, or Claude Code should generate, refactor, or audit EFrame projects, update the relevant `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`, or `packages/com.eframework.core/AIWorkspace~` instruction/skill in the same change set. Keep always-on boundaries in instructions; put multi-step workflows, audits, and detailed checklists in skills or skill references.

## 4. UI Runtime Contract

The UI runtime contract is handle-first:

- Runtime UI lifecycle flows through `IUIService/QUI` and `UIViewHandle<TView>`.
- View wrappers stay thin, use parameterless construction, and receive prefab state through `SetBinding()` / `OnBindingSet()`.
- Controllers access live views through `CurrentView`; generated code does not use the old `UIControllerBase<TView>.View` facade.
- Controller lifecycle hooks are split by scope: `OnViewCreated()` / `OnViewDestroyed()` are instance-level, while `OnViewOpened()` / `OnViewClosed()` are per-open/per-close.
- Host-owned transitions must tolerate interruption so old open/close completions cannot overwrite the active handle state.

Any framework change that alters this UI contract must update the same contract surface in one change set:

- `packages/com.eframework.core/Documentation~/user/UI_FRAMEWORK_GUIDE.md`
- `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md`
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-ui-feature`
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-guideline-audit`
- bootstrap/editor template generators under `tools/` and `packages/com.eframework.core/Editor/`
- root and package changelogs

## 5. Unified Versioning Contract

EFrame should be maintained as one product surface, not as a Unity framework plus a second AI product. The AI collaboration layer, bootstrap tools, and sync scripts are part of the framework release contract.

- EFrame release version: stored in `packages/com.eframework.core/package.json` and recorded in the root `CHANGELOG.md`.
- AI workspace manifest version: stored in `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` only as a sync marker so business projects can detect framework-managed AI file drift.

Think in terms of the EFrame release first. Bump the package version when publishing a framework/package release. Treat the manifest as a sync marker, not a separate product version.

Manifest update rules are defined in `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md`.

The manifest version is the broad signal business projects use to detect AI layer drift. Manifest file hashes provide a narrower integrity check for local edits, missing files, or partial syncs.

Recommended release sequence:

1. Update the framework implementation or docs.
2. Update matching instructions and skills.
3. Bump `packages/com.eframework.core/package.json` if this is a framework/package release.
4. Update the root `CHANGELOG.md`; update `packages/com.eframework.core/CHANGELOG.md` if package code changed.
5. Update `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` according to `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md`.
6. Run `tools/Test-EFrameAIRelease.ps1`.
7. Verify `Initialize-EFrameAI.ps1 -StatusOnly` and `-Force`.
8. Verify cold-start or editor bootstrap paths affected by the change.
9. For a formal package release, create a preview entry before the formal tag: prefer a `preview/<package>-<version>-rc.N` branch, or record an immutable commit SHA.
10. Import that preview entry into a real business project through the Unity Package Manager Git URL, using `#preview/...` or `#<commit-sha>`, and validate the project before creating the official `vX.Y.Z` tag.

## 6. Naming Boundary

- `eframe-*` is reserved for framework-managed instructions and skills.
- `maintainer-*` is reserved for framework-repository-only skills and is not part of the sync surface.
- Business projects own their root AI entry files and may organize local AI rules however they choose.
- Framework sync scripts may overwrite drifted `eframe-*` files when `-Force` is used.
- Framework sync scripts must preserve project-owned instruction text and only update the EFrame managed block inside selected AI entry files.

This boundary keeps framework upgrades repeatable while still allowing each business project to add local rules.
