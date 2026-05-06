# Changelog

All notable changes to the `com.eframework.core` Unity package are documented in this file.

## [0.7.4] - 2026-05-06

### Fixed

- Fixed the project initialization window so Extensions automatically syncs installed package state, provides an explicit Select All action, and waits for required Showcase extension packages to be available before copying the Showcase module.
- Fixed Extension Showcase installation so the installed `QVirtualListShowcaseWindow.prefab` resolves its `VirtualListShowcaseWindow` component against the script GUID that actually landed in the business project.

## [0.7.3] - 2026-05-06

### Changed

- Updated `QUI` root layer sizing so portrait UI expands height only on taller screens, landscape UI expands width only on wider screens, and opposite-aspect screens keep the design root centered instead of stretching the business UI coordinate space.
- Updated the project initialization window so `Initialize / Repair Project` syncs EFrame AI contracts, Extensions opens as an install-state checklist with a unified Apply action, DOTween Adapter is applied from Extensions, and Showcase has its own section.

## [0.7.2] - 2026-05-06

### Changed

- Added synced AI guidance to prefer TextMesh Pro UI text components over legacy `UnityEngine.UI.Text` when TextMesh Pro is installed.
- Added synced AI guidance to use prefab-authored `SafeAreaFitter` / `FullScreenFitter` for business UI safe-area fitting.
- Updated the Extension Showcase Virtual List sample to use prefab-authored safe-area/full-screen fitters, larger controls and text, static toolbar/list templates, and explicit layout refresh after mode switches.

## [0.7.1] - 2026-05-06

### Fixed

- Updated the Extension Showcase Virtual List sample to open as a standard `QuiPopUp` UIController popup with adaptive panel sizing and viewport-driven grid wrapping.

## [0.7.0] - 2026-05-05

### Changed

- Added platform-native EFrame skill sync outputs for Codex `.agents/skills`, GitHub Copilot `.github/skills`, and Claude Code `.claude/skills`, while keeping always-on instructions and managed blocks free of skill routing lists.

### Fixed

- Fixed AI manifest file hashes for text files.

## [0.6.22] - 2026-05-05

### Fixed

- Fixed synced AI instruction activation for Unity `.prefab`, `.unity`, and `.asset` edits and aligned feature bootstrap UI bypass wording with the `EFrame.UI/UIControllerBase` business entry contract.

## [0.6.21] - 2026-05-05

### Changed

- Clarified synced AI skill ownership so coordinator and audit skills delegate detailed UI, resource, directory, data, and compile checks to specialized skills and references.

## [0.6.20] - 2026-05-05

### Changed

- Slimmed the synced AI API index into API lookup tables with short behavior notes, moving workflow detail out of the support document surface.

## [0.6.19] - 2026-05-05

### Changed

- Slimmed the synced always-on AI instruction into stable EFrame boundaries and task handoffs, moving detailed UI, resource, directory, data, audit, and compile guidance to specialized EFrame skills.

## [0.6.18] - 2026-05-05

### Changed

- Slimmed synced AI managed blocks into platform routing baselines that hand off Procedure, UI, data, resources, directory, and Unity serialized editing details to the synced instruction and specialized EFrame skills.

## [0.6.17] - 2026-05-05

### Changed

- Aligned synced AI Unity serialized asset guidance to prefer direct, narrow YAML edits, using Unity Editor APIs or other fallback paths only when direct YAML editing cannot complete or validation shows abnormal serialized state.

## [0.6.16] - 2026-05-05

### Changed

- Updated synced AI editor guidance for Unity prefab, scene object, and asset edits to allow narrow, transparent YAML edits while preserving Unity serialized identity and using Editor APIs for high-risk structural changes.

## [0.6.15] - 2026-05-03

### Fixed

- Fixed UI Extras `CheckableButton` state object handling so reused state references stay active when any matching state should be visible.
- Synchronized Basic and Extension Showcase template metadata with their fixture sources for release validation.

## [0.6.14] - 2026-05-03

### Changed

- Updated AI and user-facing UI Extras references for the new `CheckableButton` and renamed `Tabbar` components.

## [0.6.13] - 2026-05-03

### Changed

- Updated AI and user-facing UI Extras references for the new `StateButton` and renamed `Tabbar` components.

## [0.6.12] - 2026-05-03

### Added

- Added synced AI UI contract rules requiring static UI structure and visual states to be authored in prefabs, with runtime controllers limited to binding, refresh, state switching, and necessary dynamic item instantiation.

## [0.6.11] - 2026-05-03

### Added

- Added a focused module-owned UI Virtual List sample window for the Extension Showcase, covering variable-size list rows, grid mode, scroll alignment, reload, item refresh, pooling, and visible-range reporting.

### Changed

- Limited the Extension Showcase module UI to UI Virtual List and Debug Console entries.
- Made `Install Showcase` install its required extension packages before copying the module template, keeping Core free of extension runtime dependencies while allowing direct sample code inside the optional module.

### Removed

- Removed `com.eframework.gm-tools` from the initialization-window extension checklist, Showcase fixture dependencies, and extension compile validation.

### Fixed

- Fixed `Install Showcase` extension package auto-install for projects that consume EFrame Core from a git URL instead of a local framework checkout.
- Changed the initialization window Sample area into an Extensions area with a default-selected extension package checklist before `Install Showcase`.

## [0.6.10] - 2026-05-03

### Changed

- Changed the initialization-window Sample area to a single `Install Showcase` action that sets Showcase as StartUp automatically.
- Updated Extension Showcase installation to write local sibling extension packages into `Packages/manifest.json` as `file:` dependencies and resolve Package Manager packages.

## [0.6.9] - 2026-05-03

### Added

- Added initialization-window controls to refresh the installed Extension Showcase module from the package template and switch startup between Showcase and Basic.
- Added a synced AI collaboration contract requiring question and analysis requests to be handled as discussion first, with approval before project-changing actions.
- Added a maintainer sync path for Extension Showcase where `test-fixtures/EFrameShowcaseUnity` is the editable Unity source and `tools/Sync-EFrameShowcaseTemplate.ps1` generates the package template.
- Added a maintainer sync path for the Basic template where `test-fixtures/EFrameBasicTemplate/Assets` is the editable source and `tools/Sync-EFrameBasicTemplate.ps1` generates the package template.

### Changed

- Changed Basic initialization to copy the package template, including `StartUp.unity` and `HomeView.prefab`, instead of dynamically generating the startup code and UI prefab.
- Removed the legacy sample module from the Basic initialization template and fixtures; Basic now stops at `ProcedureHome` showing the Home initialization screen.
- Changed the Extension Showcase runtime UI to load the module-owned `EFrameExtensionShowcaseView.prefab` instead of building Canvas/Text/Button objects in code.
- Updated Extension Showcase template installation to copy module `.meta` files, including `.cs.txt.meta` to `.cs.meta` conversion, so non-code resources can keep stable references.

## [0.6.8] - 2026-05-03

### Changed

- Added the EFrame logo to package README files.
- Simplified the Extension Showcase initialization entry to a single install action that sets Showcase as the default startup.
- Made AI workspace checks run asynchronously from the project initialization window to avoid blocking the Unity Editor UI.

## [0.6.7] - 2026-05-03

### Added

- Added an EFrame logo asset to the project initialization window.

### Changed

- Simplified the project initialization window by consolidating setup actions, DOTween adapter controls, and AI workspace checks.

## [0.6.6] - 2026-05-02

### Added

- Added a package-documented Basic sample/template as the minimal runnable initialization skeleton for `StartUp.unity`, `ProcedureLauncher`, `ProcedureHome`, `HomeView`, basic UI prefabs, audio setup, Addressables, and generated `ResPath`.
- Added an optional Extension Showcase Project Module template with `ProcedureEFrameExtensionShowcaseEntry`, runtime showcase UI, extension demo registry entries, and initialization-window install/startup controls.
- Added `EFrameTween` as the core tween facade with a built-in fallback backend and an optional DOTween adapter gated by `EFRAME_USE_DOTWEEN`.

### Changed

- Updated the project initialization window with explicit Basic install, Extension Showcase install, Extension Showcase startup, and Basic startup restore actions.
- Made DOTween optional for core runtime code; projects can enable or disable the adapter from the initialization window when DOTween is installed.

### Removed

- Removed legacy `QList` and `XListView` list implementations.
- Moved `QVirtualListView`, `QVirtualGridView`, and their adapter/pooling support out of core into the optional `com.eframework.ui.virtual-list` package.
- Moved Unity Ingame Debug Console out of core into the optional `com.eframework.debug-console` package.
- Moved reusable presentation effects out of core into the optional `com.eframework.effects` package, including fly animation, icon bounce, camera shake, and their editor tooling.
- Moved GM tools out of core into the optional `com.eframework.gm-tools` package.
- Moved UI helper/animation components and lightweight extra controls out of core into the optional `com.eframework.ui-extras` package, including `UIHelper`, `UIAnimation`, `Tabbar`, and `EmptyRayCasterGraphic`.
- Normalized EFrame-owned extension APIs under `EFramework.Extensions.*` namespaces and added the root `tools/Test-EFrameExtensionUnityCompile.ps1` validation path for extension packages.
- Documented URP as a core-standard dependency and DOTween as an optional project-installed tween backend.
- Removed NiceVibrations, `QVibration`, `IVibrationService`, and the `EFrame.Vibration` / `Context.Vibration` core entries.
- Removed AudioEvents runtime/editor tooling from core, including `AudioEventManager`, audio event config assets, editor windows, and the `EFrame.AudioEvents` / `Context.AudioEvents` entries.

### Fixed

- Aligned optional DOTween adapter delay timing and target-kill completion behavior with the fallback tween backend.

## [0.6.5] - 2026-05-02

### Added

- Added configurable UI view transitions, including separate open and close transition settings, built-in `None` transitions, and Inspector selection for custom `IUIViewTransition` implementations.
- Added automatic UI screen-fit refresh handling for resolution and safe-area changes.

### Changed

- Updated `QUI` screen adaptation so foreground UI and world-space background layers can be refreshed consistently, including an automatic fit mode and scene-camera background rebinding.
- Improved generated UI binding names by deriving concise unique names from node path context and component semantics.
- Hardened `UIControllerBase` lifecycle initialization, async show/hide ordering, popup completion, and button listener registration defaults.
- Improved full-screen and safe-area fitters so cached or inactive UI can reapply layout safely when re-enabled.

### Fixed

- Fixed UI view creation cleanup when generated view construction fails.
- Fixed Addressables bootstrap and managed resource postprocessing edge cases for project initialization flows.

## [0.6.4] - 2026-05-02

### Added

- Added the synced `eframe-directory-structure` skill and lightweight instruction triggers so business AI can classify EFrame Unity file, resource, scene, generated-code, and module directory placement before creating or moving assets.
- Added `Test-EFrameUnityCompile.ps1` to replay Unity Bee Roslyn response files and catch Unity Editor compile errors that `dotnet build` can miss.

## [0.6.3] - 2026-05-02

### Changed

- Removed the project Root Namespace prompt from initialization and fixed generated code namespaces for `ResPath.Generated` and UI binding output.
- Moved generated UI binding output under EFrame runtime generated directories for App and module-owned UI.
- Aligned business-facing UI guidance around generated View access classes, `UIControllerBase<TGeneratedView>`, and the new `CurrentView` controller shortcut instead of exposing `UIViewHandle` as a normal usage concept.

## [0.6.2] - 2026-05-02

### Changed

- Renamed framework runtime and editor namespaces from `EFrame.*` to `EFramework.*` so external consumers can import `EFramework.Runtime` and access the static `EFrame` entry without namespace/type collisions.

## [0.6.1] - 2026-05-02

### Added

- Added a repository-level Unity consumer fixture for validating package import, the real `Initialize-EFrameColdStart.ps1` bootstrap flow, asmdef references, and external `EFrame` API usage from a business-project shape.

### Changed

- Kept the static `EFrame` runtime entry type in `EFramework.Runtime` and added consumer-fixture coverage for the namespace/type collision that requires an explicit alias or a future entry API rename.

## [0.6.0] - 2026-05-02

### Added

- Added lightweight `EFrame.UI`, `EFrame.Assets`, `EFrame.Data`, `EFrame.Events`, `EFrame.Audio`, and related runtime shortcuts while keeping `EFrame.Current` as the full context entry.

### Changed

- Updated package AI guidance and user docs to prefer the lighter `EFrame.*` runtime service shortcuts outside framework-aware injected `Context` code.

### Fixed

- Fixed `FlyAnimationSystem` audio guards to use the `EFrame` runtime shortcuts without invalid nested namespace qualification.

## [0.5.0] - 2026-05-02

### Changed

- Renamed the package-facing framework brand to EFrame across package metadata, runtime/editor code, bootstrap templates, docs, and AI workspace support files.
- Standardized runtime namespaces on `EFramework.Runtime` and editor namespaces on `EFramework.Editor`.
- Standardized package assemblies on `EFrame` and `EFrame.Editor`.
- Updated built-in UI templates and generated binding defaults to use `EFramework.Runtime.UI.Generated`.

## [0.4.6] - 2026-05-02

### Fixed

- Removed the remaining deprecated `com.unity.textmeshpro` package reference from the bundled UniTask TextMeshPro assembly definition; TextMeshPro support now keys off `com.unity.ugui`.

## [0.4.5] - 2026-05-02

### Changed

- Simplified the synced package AI contract layering so managed root blocks stay as entry points, `eframe-instructions` stays focused on always-on rules, and `EFRAME_AI_API_INDEX` stays focused on business-facing API lookup.
- Refined synced `eframe-*` skills so feature bootstrap, UI, data, resource, and audit workflows have clearer ownership boundaries and align runtime service examples around injected `Context` first.

## [0.4.4] - 2026-05-02

### Changed

- Moved AI-facing API support content under `AIWorkspace~/support-docs/` and removed the package documentation surface.
- Kept human-facing usage and maintenance documents in package `Documentation~` instead of treating them as AI sync contract sources; maintainer AI release rules live outside the package in `tools/MaintainerAIWorkspace`.

### Fixed

- Updated the Addressables bootstrap directory-structure warning to reference `Documentation~/user/UNITY_DIRECTORY_STRUCTURE.md` after the docs move.

## [0.4.3] - 2026-05-02

### Added

- Added `AIWorkspace~` and `Tools~` to the package so consuming projects receive AI sync sources and sync scripts with the Unity package.
- Added package-distributed EFrame AI API index, UI guide, resource path convention, and Unity directory structure guide.

### Changed

- Updated the project initialization window and AI menu actions to run package-local `Tools~` scripts instead of requiring a local full framework repository clone.
- Moved synced `eframe-*` instructions, skills, managed blocks, and manifest into the package AI workspace.

### Fixed

- Fixed package-only AI workspace sync so consuming projects receive `.github/eframe/EFRAME_AI_API_INDEX.md` from the installed package.

## [0.4.2] - 2026-05-02

### Added

- Added Unity Editor AI workspace platform selection plus `EFrame Tools/AI` menu entries for sync status, AI workspace sync, and project AI health checks.

### Changed

- Decoupled Unity cold-start from AI contract sync; users now choose Codex, Copilot, Claude Code, or all platforms before manually syncing AI guidance.

## [0.4.1] - 2026-05-01

### Fixed

- Reduced fallback AudioListener creation from a warning to an informational log.
- Guarded pause/quit handling before framework initialization completes and waited briefly for Procedure startup to report running.

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

- Added a dedicated `EFramework.Editor` asmdef so package editor tooling compiles in an explicit Editor-only assembly with declared framework and plugin dependencies.

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
- Unified legacy UI component namespaces under `EFramework.Runtime.UI` so runtime UI widgets and helper components share one framework namespace family.
- Unified `QUIBinding` generated access classes and built-in UI templates on `EFramework.Runtime.UI.Generated` so generated bindings follow the same runtime UI namespace family.
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
- Split AI guidance by sync boundary so synced `eframe-*` files stay business-project-facing while framework-internal release and sync workflows stay outside the package AI contract.
- Applied the same sync-boundary cleanup to the editor instruction layer so only stable business-project editor rules remain in synced `eframe-editor.instructions`.
- Clarified AI instruction-vs-skill ownership so business-project workflows remain in synced `eframe-*` skills while framework-only release and sync workflows stay outside the package AI contract.
- Strengthened AI release checks with frontmatter validation, manifest version-increase validation, synced instruction length warnings, and sync-boundary checks.
- Added focused business-project skills for EFrame UI features, persistent data tables, and resource/Addressables flows.
- Returned event debug type queries as snapshots so external enumeration is not affected by later subscription changes.
- Removed data storage sample/test classes from the runtime assembly surface.

## [0.1.0] - 2026-04-29

### Added

- Initial formal EFrame Core Unity package release.
- Added runtime framework entry through `EFrame.Current` and `EFrameContext` services.
- Added runtime modules for UI, assets, audio, data storage, events, coroutine helpers, effects, vibration, and utility services.
- Added editor tooling for project initialization, Addressables setup, ResPath generation, sample UI prefab import, audio setup, UI binding, and effect debugging.
- Added package dependencies for Addressables, UGUI, Input System, TextMeshPro, and Unity Newtonsoft.Json.

### Changed

- Standardized runtime service access through `EFrame.Current` and injected `Context`.
- Documented async handle-based resource service access as the runtime path.
