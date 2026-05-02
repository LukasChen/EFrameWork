# Changelog

All notable changes to EFrame are documented in this file.

EFrame uses semantic versioning for framework releases. The release version is recorded in `packages/com.eframework.core/package.json` and summarized here. Unity code, AI collaboration rules, bootstrap tools, sync scripts, and documentation are treated as one EFrame release surface. `.github/eframe-ai.manifest.json` is only an internal sync marker for business projects.

## [Unreleased]

## [0.5.0] - 2026-05-02

### Changed

- Renamed the framework brand from its legacy name to EFrame across runtime, editor tooling, package metadata, generated templates, documentation, and AI workspace contracts.
- Standardized runtime namespaces on `EFrame.Runtime` and editor namespaces on `EFrame.Editor`.
- Standardized package assemblies on `EFrame` and `EFrame.Editor`.
- Updated bootstrap scripts, UI binding templates, synced AI instructions, managed blocks, support docs, and generated HTML docs to use the EFrame naming contract.

## [0.4.6] - 2026-05-02

### Fixed

- Removed the remaining deprecated `com.unity.textmeshpro` package reference from the bundled UniTask TextMeshPro assembly definition; TextMeshPro support now keys off `com.unity.ugui`.

## [0.4.5] - 2026-05-02

### Changed

- Simplified the synced EFrame AI contract layering so managed root blocks stay as entry points, `eframe-instructions` stays focused on always-on rules, and `EFRAME_AI_API_INDEX` stays focused on business-facing API lookup.
- Refined synced `eframe-*` skills so feature bootstrap, UI, data, resource, and audit workflows have clearer boundaries and consistently prefer injected `Context` over `EFrame.Current` in framework-aware code.
- Tightened maintainer AI release and contract guidance so the release checklist and maintainer contract skill act as routing and validation docs instead of duplicating synced business rules.

## [0.4.4] - 2026-05-02

### Changed

- Clarified the documentation boundary: `packages/com.eframework.core/Documentation~/` is now human-facing documentation, while AI-facing synced support docs live under `packages/com.eframework.core/AIWorkspace~/support-docs/`.
- Moved repository docs into the package documentation tree, moved the AI release checklist into `tools/MaintainerAIWorkspace`, and kept AI contract sources separate from human docs.

### Fixed

- Updated the project bootstrap directory warning to point at `Documentation~/user/UNITY_DIRECTORY_STRUCTURE.md` after the docs reorganization.

## [0.4.3] - 2026-05-02

### Added

- Added package-shipped `AIWorkspace~` and `Tools~` folders so package-only business projects can sync EFrame AI guidance without access to the full framework repository.
- Added package-distributed business AI documentation, including the stable API index, UI guide, resource path convention, and Unity directory structure guide.

### Changed

- Moved business-facing AI sync sources, `eframe-*` skills, managed blocks, and manifest out of repository `.github` and into `packages/com.eframework.core/AIWorkspace~`.
- Moved maintainer-only AI workflow guidance to `tools/MaintainerAIWorkspace`, with setup/architecture human docs under package `Documentation~/maintainer`.
- Simplified `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md` into thin client entry indexes that point to package business contracts and maintainer docs.
- Updated Unity AI sync menu integration to run package-shipped `Tools~` scripts from the installed package root.

### Fixed

- Fixed package-only AI sync so business projects receive `.github/eframe/EFRAME_AI_API_INDEX.md` and no longer depend on root-level framework docs.

## [0.4.2] - 2026-05-02

### Added

- Added `CLAUDE.md` as the Claude Code project entry point.
- Added Unity Editor AI workspace platform selection plus `EFrame Tools/AI` menu entries for sync status, AI workspace sync, and project AI health checks.

### Changed

- Decoupled Unity cold-start from AI contract sync; users now choose Codex, Copilot, Claude Code, or all platforms before manually syncing AI guidance.

## [0.4.1] - 2026-05-01

### Added

- Added machine-readable capability metadata to synced `eframe-*` skills so AI agents and release checks can reason about ownership, outputs, delegation, and forbidden patterns.
- Added `EFRAME_AI_API_INDEX.md`, a synced AI-oriented index of stable EFrame runtime, editor bootstrap, and workspace tooling entry points.
- Added business-project AI health checking through `tools/Test-EFrameAIProject.ps1`.
- Added manifest-tracked file hashes so projects can detect local drift even when the synced AI version number matches.

### Changed

- Updated AI sync/status reporting to include manifest file integrity checks and to sync the AI API index into business projects.
- Strengthened AI release validation so synced file hashes and `eframe-*` skill capability metadata are checked before release.

### Fixed

- Fixed the README API reference link to point at the generated docs site.
- Reduced fallback AudioListener creation from a warning to an informational log.
- Guarded pause/quit handling before framework initialization completes and waited briefly for Procedure startup to report running.

## [0.3.0] - 2026-04-30

### Added

- Added EFrame-owned Procedure runtime and editor configuration (`EFrameProcedure`, `EFrameProcedureComponent`, payload enter context, and inspector).
- Added the strong managed Addressables convention: EFrame-owned resource directories auto-sync groups, addresses, labels, and generated `ResPath`, with build-time validation and synced AI guidance.
- Added the managed resource report window contract so the Addressables sync entry can inspect Addressables-to-ResPath mappings and directory convention issues.
- Split managed Addressables automation from ResPath generation so the framework owns Addressables directories while the report window chooses which directory subtrees generate `ResPath.Generated`.

## [0.2.5] - 2026-04-30

### Fixed

- Removed the deprecated `com.unity.textmeshpro` package dependency and documented TextMeshPro as part of `com.unity.ugui` for current Unity versions.

## [0.2.4] - 2026-04-30

### Added

- Added a dedicated `EFrame.Editor` asmdef so package editor tooling compiles in an explicit Editor-only assembly with declared framework and plugin dependencies.

### Changed

- Declared Universal RP as a required package dependency and documented the URP camera-stack contract for `QUI` scene-camera overlay binding.

### Fixed

- Removed the stale repo-root `eframe-ai.manifest.json` duplicate so `.github/eframe-ai.manifest.json` remains the only canonical AI sync marker.

## [0.2.3] - 2026-04-30

### Added

- Added the `EFrameSceneCamera` runtime convention so `QUI` can bind the persistent UI camera into URP camera stacks from scene-camera lifecycle events instead of per-frame polling.

### Changed

- Updated AI release maintenance rules so future release handling includes creating a git commit and matching version tag.

## [0.2.2] - 2026-04-29

### Added

- Added a framework editor menu item to sync managed Addressables groups and regenerate `ResPath.Generated.cs` without running the full project initializer.
- Added unified Audio Resources paths under `Assets/Resources/Audio`, mixer volume controls, and a single `AudioClipAsset` configuration asset that supports one or more clips.

### Fixed

- Avoided invalid generated `ResPath` members when an Addressables path repeats the same segment name as its enclosing generated class.
- Improved Audio event playback cleanup, weighted-random behavior, SFX fade pooling, delayed-event cancellation, and AudioListener fallback handling.

## [0.2.1] - 2026-04-29

### Changed

- Added a repo-root `AGENTS.md` Codex entry point and synced it with the EFrame AI workspace so Codex and Copilot can share the same framework guidance.
- Refocused `eframe-feature-bootstrap` as a top-level feature coordinator that delegates UI, data-table, resource, and maintainer details to specialized skills.
- Added cold-start generation for the empty `AudioEventConfig` Resources asset so new projects initialize audio events without missing-config warnings.
- Split `UIControllerBase<TView>` lifecycle hooks into instance-level create/destroy and per-open/per-close callbacks so cached UI handles no longer trigger release cleanup on every hide.
- Updated UI binding generation, project bootstrap templates, and editor module scaffolds to use parameterless View wrappers, `OnBindingSet()`, and `TypedViewHandle.TypedView` instead of legacy asset-path constructors or `View` facades.
- Stabilized default UI transitions so interrupted DOTween open/close animations complete their await path and stale async handle completions cannot overwrite newer UI state.
- Synchronized UI runtime contract docs, AI runtime instructions, UI feature skill, and audit checklist with the handle-first lifecycle model.

## [0.2.0] - 2026-04-29

### Changed

- Improved EFrame initialization diagnostics with explicit failure stages and safer partial-service cleanup.
- Bounded runtime asset and UI caches, added async Addressables path validation, and tightened pooled Addressables instance release.
- Updated UIController and BindingView lifecycle rules so controllers lazily validate framework context and cached UI reuses `QUIBinding`/GameObject state only.
- Added a settings-controlled screen-fit debug log switch and unified data access around `IDataService`.
- Tightened data table registration/disposal semantics, added scoped event subscriptions, and reset static task queue state on framework disposal.
- Automated data table dirty marking through table-owned setters/helpers and removed external mutable `Data` access from the persistence model.
- Added versioned data envelopes and table-owned migration hooks so old raw save files can load as `version 0` and migrate forward without immediate overwrite.
- Added temporary-file writes and `.bak` fallback recovery to `JsonFileStorage` so save replacement is safer and unreadable primary files can recover from backup.
- Added structured persistence load diagnostics via `LastLoadResult` so tables can report whether data loaded normally, migrated, fell back to backup, or reverted to defaults.
- Stabilized persistence keys by requiring each `DataTable` to declare an explicit `StorageKey` instead of defaulting save paths to runtime type names.
- Added registration-time validation for data table storage keys so empty or duplicate `StorageKey` values fail fast instead of silently sharing one save file.
- Tightened `DataTable.Mutate(...)` so table-owned mutators must explicitly report whether data changed before dirty is set.
- Added stable persistence `ReasonCode` values to storage load contexts and `LastLoadResult` so failure and recovery branches no longer depend on free-form messages.
- Added `LastSaveResult` to `DataTable` so save attempts can report whether they wrote, skipped because data was clean, or failed.
- Moved QUI sorting layer setup into the project bootstrap flow so initialization can auto-create required UI sorting layers, and added runtime QUI warnings when layers are missing.
- Reorganized editor tooling into `ProjectBootstrap`, `UI`, and `Tools` groups so UI-specific inspectors, bootstrap setup, and developer shortcuts no longer mix under one root.
- Unified legacy UI component namespaces under `EFrame.Runtime.UI` so `QScroller`, `QTab`, UI animations, and helper components no longer expose mixed historical prefixes.
- Unified `QUIBinding` generated access classes and shipped UI templates on `EFrame.Runtime.UI.Generated` so newly generated view access code matches the runtime UI namespace family.
- Split runtime UI layout adaptors and reusable controls into `Layout` and `Components` namespace groups so the UI root chain stays distinct from fitters and widget-style utilities.
- Moved `UIBuilder` and `EmptyRayCasterGraphic` out of the runtime UI root so helper/toolbox code and lightweight reusable components no longer sit beside the core QUI chain.
- Normalized `XListView` folder and namespace casing so component subfolders now follow the same stable naming style as their exported types.
- Moved binding creation and release decisions into `IUIService/QUI` so `BindingViewBase` no longer directly performs asset instantiation, cache hits, or recycle/destroy policy decisions.
- Moved `UIControllerBase` default View creation onto the `IUIService/QUI` host path so generated and templated Views can initialize through parameterless construction plus `OnBindingSet()` instead of relying on `assetPath` constructors.
- Extracted the default open/close animation into a host-owned `IUIViewTransition` strategy so `BindingViewBase` no longer contains DOTween transition details.
- Introduced `UIViewHandle<TView>` and switched `UIControllerBase` to own a handle instead of directly owning the runtime View instance.
- Moved opening/closing/released state tracking into `UIViewHandle<TView>` and reduced `BindingViewBase` toward lower-level lifecycle primitives such as attach, transition dispatch, and binding release.
- Kept cached `UIViewHandle<TView>` instances in a reusable `Closed` state instead of always releasing them, and propagated `assetPath` through host-created `SetBinding()` flows so wrapper cache keys remain valid.
- Moved `QUI` navigation stack storage onto `IUIViewHandle` so stack navigation now follows the same runtime handle semantics as controllers.
- Removed the temporary `BindingViewBase` navigation compatibility facades from `QUI` and fixed top-of-stack removal so handle-backed stack entries are removed by their wrapped View instance.
- Removed the legacy `UIControllerBase<TView>.View` facade and switched generated controller templates to explicit `TypedViewHandle` access.
- Reduced `BindingViewBase` lifecycle entrypoints to internal-only APIs so `UIViewHandle` is now the sole runtime lifecycle surface for open/close flows.
- Reorganized `eframe-runtime.instructions` into layer- and concern-based sections so the accumulated runtime/UI/persistence rules are easier to scan without changing the underlying guidance.
- Split AI guidance by sync boundary: `eframe-runtime.instructions` now focuses on business-project runtime contract, while framework-only maintenance guidance and workflows live under non-synced `maintainer-*` instructions/skills.
- Applied the same sync-boundary split to `eframe-editor.instructions`, keeping business-project editor rules in the synced contract and moving framework-only generator/release maintenance guidance to the maintainer layer.
- Clarified AI instruction-vs-skill ownership so short always-on rules stay in instructions, business-project workflows stay in synced `eframe-*` skills, and framework-only release/sync maintenance lives in `maintainer-*` skills.
- Strengthened AI release checks with frontmatter validation, manifest version-increase validation, synced instruction length warnings, and maintainer sync-boundary checks.
- Added focused business-project skills for EFrame UI features, persistent data tables, and resource/Addressables flows.
- Returned event debug type queries as snapshots so external enumeration is not affected by later subscription changes.
- Removed data storage sample/test classes from the runtime assembly surface.
- Updated AI runtime instructions and skills to recommend async asset handles, the new UI cache semantics, scoped event cleanup, DataTable dirty automation, and versioned save migration.

## [0.1.0] - 2026-04-29

### Added

- Established EFrame as a lightweight Unity game framework with a synchronized AI collaboration layer.
- Added framework-managed Copilot instructions, focused runtime/editor instructions, and EFrame workflow skills.
- Added project cold-start tooling for standard directories, bootstrap code, AI workspace sync, project overlay, and project updater installation.
- Added AI workspace sync preview with managed file, project overlay, stale item, and synced document reporting.
- Added cold-start summary output with `OK`, `SKIP`, and `WARN` status reporting.
- Added AI release check tooling to verify manifest updates and `eframe-*` / `project-*` naming boundaries.
- Added EFrame AI architecture, setup, release checklist, directory structure, and resource path convention documentation.

### Changed

- Standardized runtime service access around `EFrame.Current` and injected `Context`.
- Updated bootstrap templates to use current runtime service access patterns.
- Formalized AI workspace versioning and release maintenance rules.

### Fixed

- Fixed generated `ProcedureHome` bootstrap code to use the current UI service entry.
- Fixed runtime service interface implementation issues found during the initial framework pass.
