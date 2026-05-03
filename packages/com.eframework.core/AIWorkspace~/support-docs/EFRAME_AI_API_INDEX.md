# EFrame AI API Index

This index is a compact, AI-oriented map of stable EFrame APIs. Use it to choose the right framework entry point before generating or refactoring code. It is intentionally smaller than the full browsable docs and favors project-facing contracts over internal implementation details.

After AI sync, business projects receive this document at `.github/eframe/EFRAME_AI_API_INDEX.md`; synced AI entry blocks point there so project AI can consult it without guessing framework APIs.

## Runtime Access

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Access initialized framework services | `EFrame.UI`, `EFrame.Assets`, `EFrame.Data`, `EFrame.Events`, `EFrame.Audio` | `packages/com.eframework.core/Runtime/EFrame.cs` | Only use after `EFrame.Initialize(...)` succeeds. Framework base classes receive `Context` automatically. |
| Read the full initialized service bundle | `EFrame.Current` | `packages/com.eframework.core/Runtime/EFrame.cs` | Use when a caller needs the whole `EFrameContext` instead of one common service shortcut. |
| Read service bundle from injected code | `EFrameContext` | `packages/com.eframework.core/Runtime/EFrameContext.cs` | Provides `Assets`, `UI`, `Audio`, `Data`, `Events`, `Coroutine`, cameras, and FPS. |
| Receive context in behaviours/views/controllers | `IEFrameContextAware.BindContext(...)` | `packages/com.eframework.core/Runtime/EFrameBehaviour.cs` | Prefer injected `Context` inside framework-aware types instead of repeated global lookup. |

Avoid accessing `EFrame.*` service shortcuts from constructors that can run before initialization.

## Startup And Procedure

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Boot the framework in a scene | `EFrameComponent` | `packages/com.eframework.core/Runtime/EFrameComponent.cs` | Lives on the startup `Boot` object with `EFrameProcedureComponent`. |
| Own app/game state switching | `EFrameProcedure` | `packages/com.eframework.core/Runtime/Procedure/EFrameProcedure.cs` | Put preload in `OnPreloadAsync(...)`, orchestration in enter/leave, and detailed UI/gameplay in controllers or modules. |
| Register and start procedures | `EFrameProcedureComponent` | `packages/com.eframework.core/Runtime/Procedure/EFrameProcedureComponent.cs` | Startup flow should use framework-owned procedure switching. |
| Pass one-shot transition data | `ProcedureEnterContext` | `packages/com.eframework.core/Runtime/Procedure/ProcedureEnterContext.cs` | Persistent data belongs in data tables, services, or events. |

Avoid adding new procedures for simple page/popup changes that can live inside the active state.

## Assets And Resources

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Runtime asset service | `IAssetService` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Access through `Context.Assets` or `EFrame.Assets`. |
| Use generated managed asset ids | `ResPath.Generated` | `Assets/App/Runtime/Generated/Res/ResPath.Generated.cs` | Namespace is fixed to `EFramework.Generated`; use generated ids instead of handwritten Addressables strings in business runtime code. |
| Procedure-owned preload scope | `IAssetPreloadScope` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Use in `OnPreloadAsync(...)`; the scope releases on procedure leave. |
| Load an asset with explicit ownership | `LoadAsync<T>(assetId)` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Dispose the returned `AssetHandle<T>` at the owning lifecycle boundary. |
| Instantiate by generated id | `Instantiate(assetId, parent)` / `InstantiateAsync(assetId, parent)` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Use `ResPath.Generated` for managed resource `assetId` values. |
| Reuse pooled prefabs | `GetFromPool(...)` / `RecycleToPool(...)` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Use for repeated runtime objects after preload. |

Avoid scattered Addressables strings, undocumented `WaitForCompletion`, and passing `AssetReference` through business runtime logic.

## UI

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Business UI entry | `EFrame.UI`, `UIControllerBase<TGeneratedView>` | `packages/com.eframework.core/Runtime/EFrame.cs`, `packages/com.eframework.core/Runtime/UI/UIControllerBase.cs` | Business code should write controllers and call `Show()` / `Hide()`; do not directly own service or handle objects. |
| Access the live generated view | `CurrentView` | `packages/com.eframework.core/Runtime/UI/UIControllerBase.cs` | Use inside controllers to reach the generated binding view without touching `UIViewHandle`. |
| Generated View access class | UI Binding generated class under `EFramework.Generated.UI` | `packages/com.eframework.core/Editor/UI/UIAutoBindingEditor.cs`, `packages/com.eframework.core/Runtime/UI/BindingViewBase.cs` | Generated from prefab `QUIBinding`; normal business UI should not hand-write View wrappers. |
| Prefab binding component | `QUIBinding` | `packages/com.eframework.core/Runtime/UI/QUIBinding.cs` | Prefabs include binding/config data such as default layer, cache, and animation root. |
| Advanced/internal UI host | `IUIService` / `QUI`, `UIViewHandle<TView>` | `packages/com.eframework.core/Runtime/Services/IUIService.cs`, `packages/com.eframework.core/Runtime/UI/QUI.cs`, `packages/com.eframework.core/Runtime/UI/Handles/UIViewHandle.cs` | Use directly only for framework internals, advanced UI managers, navigation stack work, or debugging lifecycle state. |
| UI overlay camera participation | `EFrameSceneCamera` | `packages/com.eframework.core/Runtime/EFrameSceneCamera.cs` | Add to runtime scene cameras that should host the framework UI overlay stack. |
| Optional UI helper/animation components | `com.eframework.ui-extras` | `packages/com.eframework.ui-extras/Runtime` | Provides `CheckableButton`, `Tabbar`, `EmptyRayCasterGraphic`, `UIHelper`, and `UIAnimation`; these are no longer core APIs. |
| Optional virtual list/grid controls | `com.eframework.ui.virtual-list` | `packages/com.eframework.ui.virtual-list/Runtime` | Provides pooled virtual list/grid controls under `EFramework.Extensions.UI.VirtualList`. |

Avoid hand-built persistent top-level Canvas/EventSystem objects, hand-written normal business View wrappers, direct `BindingViewBase` lifecycle driving, and direct business `UIViewHandle` ownership.

## Tween

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Framework-owned simple tweens | `EFrameTween` | `packages/com.eframework.core/Runtime/Tween/EFrameTween.cs` | Use for core/runtime fades, movement, colors, delays, and target-based kill operations. Basic projects use the fallback backend by default. |
| Configure a tween | `EFrameTweenOptions` | `packages/com.eframework.core/Runtime/Tween/EFrameTweenOptions.cs` | Supports target binding, update mode, ease, ignore-time-scale, and completion callbacks. |
| Track or cancel a tween | `IEFrameTweenHandle` | `packages/com.eframework.core/Runtime/Tween/IEFrameTweenHandle.cs` | Call `Kill()` or await `ToUniTask()` at the owning lifecycle boundary. |
| Optional DOTween backend | `EFRAME_USE_DOTWEEN` + initialization-window `Tween Backend` controls | `packages/com.eframework.core/Runtime/Tween/Dotween`, `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameDotweenBootstrapUtility.cs` | DOTween is not a core dependency. Enable this adapter only after DOTween is installed in the project; disable it before removing DOTween. |

Avoid referencing `DG.Tweening` directly from core-owned runtime code unless it is inside the optional DOTween adapter assembly.

## Data

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Data service | `IDataService` | `packages/com.eframework.core/Runtime/Services/IDataService.cs` | Register and retrieve tables through `Context.Data` / `EFrame.Data`. |
| Register a table type | `RegisterTable<T>()` | `packages/com.eframework.core/Runtime/Services/IDataService.cs` | Reuse existing registered tables instead of constructing ad-hoc table instances. |
| Persistent table base | `DataTable<TData>` | `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Override `GetStorageKey()`, `GetDefaultData()`, and `Migrate(...)` when schema changes. |
| Dirty-aware mutation | `SetValue(...)` / `Mutate(...)` | `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Expose table-owned methods/properties; do not mutate raw data from outside. |
| Load/save diagnostics | `LastLoadResult` / `LastSaveResult` | `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Branch on structured status/reason codes, not log strings. |
| Trigger explicit saves | `SaveAll()` / table `Save()` | `packages/com.eframework.core/Runtime/Services/IDataService.cs`, `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Use when the project needs persistence at explicit business checkpoints. |

Avoid deriving persistence identity from class names or namespaces.

## Events

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Scoped event subscription | `Context.Events.SubscribeScoped<T>(...)` / `EFrame.Events.SubscribeScoped<T>(...)` | `packages/com.eframework.core/Runtime/Event/IEventService.cs` | Store and dispose subscriptions at leave/destroy/controller cleanup boundaries. |
| Dispatch framework/project events | `Context.Events.Dispatch(evt)` / `EFrame.Events.Dispatch(evt)` | `packages/com.eframework.core/Runtime/Event/IEventService.cs` | Events are value types implementing `IEvent`. |

Avoid unpaired manual subscribe/unsubscribe when a scoped subscription is available.

## Audio

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Audio service | `IAudioService` / `AudioManager` | `packages/com.eframework.core/Runtime/Audio/AudioManager.cs` | Use through `Context.Audio` or `EFrame.Audio`; supports Music/SFX playback, mixer volume controls, `AudioClipAsset`, SFX pooling, debouncing, and music crossfade. |

Keep framework-owned audio mixer config under `Assets/Resources/Audio`.

## Editor Bootstrap

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Full project initialization | `EFrameProjectInitializationWindow` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameProjectInitializationWindow.cs` | Unity menu entry for cold-start/bootstrap tasks. |
| Install minimal runnable startup skeleton | `Initialize / Repair Project` | `packages/com.eframework.core/Editor/Templates/Basic`, `test-fixtures/EFrameBasicTemplate` | Basic is copied from the package template, stops at `ProcedureHome` showing `HomeView`, and must not depend on optional extension packages or legacy sample modules. |
| Install optional extension packages | `Install Extensions` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameProjectInitializationWindow.cs` | Expands a default-selected extension package checklist for UI Virtual List, UI Extras, Effects, and Debug Console, writes selected dependencies from local sibling packages or the same git `?path=` source as Core, and resolves packages. |
| Install optional extension demos | `Install Showcase` | `packages/com.eframework.core/Editor/Templates/Modules/EFrameExtensionShowcase` | Installs the Showcase-required extension packages, copies the full module template to `Assets/Modules/EFrameExtensionShowcase`, converts `.cs.txt` to `.cs`, preserves module resource `.meta` files, syncs Addressables, and sets `ProcedureEFrameExtensionShowcaseEntry` as the StartUp entrance. |
| Addressables groups and ResPath generation | `EFrameAddressablesBootstrapUtility` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAddressablesBootstrapUtility.cs` | Owns managed resource group sync and `ResPath.Generated`. |
| Managed resource report UI | `EFrameAddressablesReportWindow` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAddressablesReportWindow.cs` | Use to select directories for generated ResPath entries and inspect issues. |
| Build preflight | `EFrameAddressablesBuildPreprocessor` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAddressablesBuildPreprocessor.cs` | Fails builds when managed Addressables or generated paths drift. |
| Import/move/delete auto-sync | `EFrameAppResAddressablePostprocessor` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAppResAddressablePostprocessor.cs` | Keeps managed resource changes aligned with Addressables automation. |

Avoid text-rewriting Unity `.unity`, `.prefab`, or `.asset` files when Editor APIs can make narrow serialized changes.

## Templates And Optional Modules

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Minimal new-project baseline | Basic | `test-fixtures/EFrameBasicTemplate`, `packages/com.eframework.core/Editor/Templates/Basic` | Maintainers open the full Unity fixture, edit its `Assets` source, and sync it with `tools/Sync-EFrameBasicTemplate.ps1`; users run the initialization window instead of manually copying template folders. |
| Extension demo launcher | `GameApp.Modules.EFrameExtensionShowcase.Procedure.ProcedureEFrameExtensionShowcaseEntry` | `packages/com.eframework.core/Editor/Templates/Modules/EFrameExtensionShowcase` | Optional Project Module with a prefab-backed UI under `Assets/Modules/EFrameExtensionShowcase/Res/UI/Panels/EFrameExtensionShowcase`; current focused entries are UI Virtual List and Debug Console; UI Virtual List opens the module-owned `QVirtualListShowcaseWindow.prefab`; it does not belong in core runtime and should remain isolated under `Assets/Modules/EFrameExtensionShowcase`. |
| Future full game demo | Separate repository | n/a | Do not implement a complete Simple Game Demo inside `com.eframework.core`. |
