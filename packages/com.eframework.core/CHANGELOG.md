# Changelog

All notable changes to the `com.eframework.core` Unity package are documented in this file.

## [Unreleased]

## [0.4.0] - 2026-04-30

### Added

- Added Procedure-owned asset preload scopes via `EFrameProcedure.OnPreloadAsync(...)` so resources can load before `OnEnter` and release automatically on Procedure leave.
- Added `IAssetPreloadScope` and asset preload cache APIs to `IAssetService`, with cached instantiate and path-pool hot paths backed by fallback diagnostics.
- Added runtime audio clip asset preloading, release, and one-time warnings when SFX plays before preload.

### Changed

- Shifted the runtime resource contract to asset ids and `Context.Assets`, keeping `AssetReference` as an editor-authoring concern rather than a runtime business API.
- Updated `QUI` and Fly animation spawning to instantiate through the asset service so UI, effects, and fly prefabs participate in Procedure preload caches.
- Refactored `FlyAnimationConfig` to store runtime asset ids for fly prefabs, effects, and audio, while its editor keeps drag-and-resolve authoring.
- Updated cold-start and module scaffolds so generated Procedures preload their main UI before entering.
- Reduced public `AssetManager` static synchronous and `AssetReference` APIs to internal fallback helpers.
- Updated synced AI/runtime/resource guidance and bumped the AI manifest for the new Procedure preload and asset-id resource contract.

### Fixed

- Stabilized data table lookup by keying registered tables by type instead of short type name.
- Hardened coroutine helpers against null, negative, invalid, and disposed-state inputs.

## [0.3.0] - 2026-04-30

### Added

- Added EFrame-owned Procedure runtime and editor configuration (`EFrameProcedure`, `EFrameProcedureComponent`, payload enter context, and inspector).
- Added EFrame-managed Addressables automation so resources under `Assets/App/Res`, `Assets/Scenes`, and `Assets/Modules` auto-sync groups, addresses, labels, and `ResPath.Generated`, with build-time validation before player builds.
- Added a managed resource report window behind `EFrame Tools/Addressables/Sync Groups And Generate ResPath` to inspect Addressables entries, generated ResPath members, sync status, and directory convention issues.
- Split managed Addressables synchronization from ResPath generation with a project-level ResPath directory selection stored under `Assets/Settings/EFrameAddressablesResPathSettings.json`.

## [0.2.5] - 2026-04-30

### Fixed

- Removed the deprecated `com.unity.textmeshpro` package dependency and documented TextMeshPro as part of `com.unity.ugui` for current Unity versions.

## [0.2.4] - 2026-04-30

### Added

- Added a dedicated `EFrameWork.Editor` asmdef so package editor tooling compiles in an explicit Editor-only assembly with declared framework and plugin dependencies.

### Changed

- Declared Universal RP as a package dependency because `QUI` scene-camera overlay binding uses URP camera stack APIs.

## [0.2.3] - 2026-04-30

### Added

- Added `EFrameSceneCamera` and QUI scene camera binding so marked runtime cameras automatically host the persistent UI camera as a URP Overlay stack camera without per-frame polling.

## [0.2.2] - 2026-04-29

### Added

- Added an editor menu item at `EFrame Tools/Addressables/Sync Groups And Generate ResPath` to initialize Addressables if needed, sync managed `Assets/App/Res`, `Assets/Scenes`, and `Assets/Modules` entries, and regenerate `ResPath.Generated.cs` without running the full project initializer.
- Added Audio service volume controls for master, music, and SFX output, music pause/resume, SFX stop-all, and mixer-exposed `MasterVolume`, `MusicVolume`, and `SfxVolume` template parameters.
- Added centralized Audio Resources paths under `Assets/Resources/Audio` and unified `AudioClipAsset` so one asset can represent either a single clip or a random set of clips.

### Fixed

- Avoided invalid generated `ResPath` members when an Addressables path repeats the same segment name as its enclosing generated class, such as `Pets/Pet_1/Pet_1`.
- Improved Audio event delayed-play cancellation, SFX fade cleanup through the shared pool, weighted-random zero-weight fallback, and AudioListener fallback creation.

## [0.2.1] - 2026-04-29

### Changed

- Split `UIControllerBase<TView>` lifecycle hooks into instance-level create/destroy and per-open/per-close callbacks so cached UI handles keep reusable instances without running release cleanup on every hide.
- Updated UI binding generation and bootstrap/editor module scaffolds to generate parameterless View wrappers initialized through `OnBindingSet()` and controllers that access views through `TypedViewHandle.TypedView`.
- Stabilized default `IUIViewTransition` playback so DOTween open/close transitions await actual completion or interruption, and `UIViewHandle<TView>` ignores stale async completions after newer operations start.

## [0.2.0] - 2026-04-29

### Changed

- Added initialization failure stages and safer cleanup for partially created services.
- Added async Addressables path validation, bounded object pools, and Addressables-aware pooled instance release.
- Refined UI cache behavior so cached views reuse `QUIBinding`/GameObject instances while View wrappers remain single-use.
- Added lazy UIController context validation before showing UI.
- Added `EFrameSettings.EnableScreenFitDebugLog` and unified data access around `IDataService`.
- Tightened data table registration/disposal semantics, added scoped event subscriptions, cleared global event hooks on reset, and reset static task queue state on framework disposal.
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
- Reorganized editor tooling into `ProjectBootstrap`, `UI`, and `Tools` groups so initialization utilities, UI inspectors, and developer shortcuts have clearer ownership.
- Unified legacy UI component namespaces under `EFrameWork.Runtime.UI` so runtime UI widgets and helper components share one framework namespace family.
- Unified `QUIBinding` generated access classes and built-in UI templates on `EFrameWork.Runtime.UI.Generated` so generated bindings follow the same runtime UI namespace family.
- Split runtime UI layout adaptors and reusable controls into `Layout` and `Components` namespace groups so core UI services and widget-style utilities are easier to distinguish.
- Moved `UIBuilder` and `EmptyRayCasterGraphic` out of the runtime UI root so helper/toolbox code and lightweight reusable components align with `UIHelper` and `Components` ownership.
- Normalized `XListView` folder and namespace casing so component subfolders now match the naming style of the exported runtime types.
- Moved binding creation and release decisions into `IUIService/QUI` so `BindingViewBase` is reduced toward a view wrapper instead of owning instantiation and cache policy.
- Moved `UIControllerBase` default View creation onto the `IUIService/QUI` host path so new Views can initialize through parameterless construction and `OnBindingSet()` rather than `assetPath` constructors.
- Extracted the default open/close animation into a host-owned `IUIViewTransition` strategy so `BindingViewBase` no longer owns DOTween transition details.
- Introduced `UIViewHandle<TView>` so `UIControllerBase` no longer directly owns the runtime View lifecycle state.
- Moved opening/closing/released state tracking into `UIViewHandle<TView>` and thinned `BindingViewBase` toward lifecycle primitives instead of full open/close orchestration.
- Kept cached `UIViewHandle<TView>` instances reusable in a `Closed` state and propagated `assetPath` into host-created View wrappers so cache/release keys remain correct.
- Moved `QUI` navigation stack storage onto `IUIViewHandle` while retaining `TopView` as a compatibility facade.
- Removed the temporary `BindingViewBase` navigation compatibility facades from `QUI` and fixed handle-backed stack removal to compare against `IUIViewHandle.View`.
- Removed the legacy `UIControllerBase<TView>.View` facade and updated generated controller templates to use `TypedViewHandle` explicitly.
- Reduced `BindingViewBase` lifecycle entrypoints to internal-only APIs so runtime open/close flows now route through `UIViewHandle` only.
- Reorganized the framework runtime instruction file into layer- and concern-based sections so UI, startup, resource, and persistence rules are easier to maintain.
- Split AI guidance by sync boundary so synced `eframe-*` files stay business-project-facing while framework-only maintenance notes and workflows live under non-synced `maintainer-*` instructions/skills.
- Applied the same sync-boundary cleanup to the editor instruction layer so only stable business-project editor rules remain in synced `eframe-editor.instructions`.
- Clarified AI instruction-vs-skill ownership so business-project workflows remain in synced `eframe-*` skills while framework-only release and sync maintenance lives in `maintainer-*` skills.
- Strengthened AI release checks with frontmatter validation, manifest version-increase validation, synced instruction length warnings, and maintainer sync-boundary checks.
- Added focused business-project skills for EFrame UI features, persistent data tables, and resource/Addressables flows.
- Returned event debug type queries as snapshots so external enumeration is not affected by later subscription changes.
- Removed data storage sample/test classes from the runtime assembly surface.

## [0.1.0] - 2026-04-29

### Added

- Initial formal EFrameWork Core Unity package release.
- Added runtime framework entry through `EFrame.Current` and `EFrameContext` services.
- Added runtime modules for UI, assets, audio, data storage, events, coroutine helpers, effects, vibration, and utility services.
- Added editor tooling for project initialization, Addressables setup, ResPath generation, sample UI prefab import, audio setup, UI binding, and effect debugging.
- Added package dependencies for Addressables, UGUI, Input System, TextMeshPro, and Unity Newtonsoft.Json.

### Changed

- Standardized runtime service access through `EFrame.Current` and injected `Context`.
- Documented async handle-based resource service access as the runtime path.
