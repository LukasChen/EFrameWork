# EFrame AI API Index

This index is a compact, AI-oriented map of stable EFrameWork APIs. Use it to choose the right framework entry point before generating or refactoring code. It is intentionally smaller than the full browsable docs and favors project-facing contracts over internal implementation details.

After AI sync, business projects receive this document at `.github/eframe/EFRAME_AI_API_INDEX.md`; synced AI entry blocks point there so project AI can consult it without guessing framework APIs.

## Runtime Access

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Access initialized framework services | `EFrame.Current` | `packages/com.eframework.core/Runtime/EFrame.cs` | Only use after `EFrame.Initialize(...)` succeeds. Framework base classes receive `Context` automatically. |
| Read service bundle from injected code | `EFrameContext` | `packages/com.eframework.core/Runtime/EFrameContext.cs` | Provides `Assets`, `UI`, `Audio`, `AudioEvents`, `Data`, `Events`, `Coroutine`, `Vibration`, cameras, and FPS. |
| Receive context in behaviours/views/controllers | `IEFrameContextAware.BindContext(...)` | `packages/com.eframework.core/Runtime/EFrameBehaviour.cs` | Prefer injected `Context` inside framework-aware types instead of repeated global lookup. |

Avoid accessing `EFrame.Current.*` from constructors that can run before initialization.

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
| Runtime asset service | `IAssetService` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Access through `Context.Assets` or `EFrame.Current.Assets`. |
| Use generated managed asset ids | `ResPath.Generated` | `Assets/App/Runtime/Generated/Res/ResPath.Generated.cs` | Use generated ids instead of handwritten Addressables strings in business runtime code. |
| Procedure-owned preload scope | `IAssetPreloadScope` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Use in `OnPreloadAsync(...)`; the scope releases on procedure leave. |
| Load an asset with explicit ownership | `LoadAsync<T>(assetId)` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Dispose the returned `AssetHandle<T>` at the owning lifecycle boundary. |
| Instantiate by generated id | `Instantiate(assetId, parent)` / `InstantiateAsync(assetId, parent)` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Use `ResPath.Generated` for managed resource `assetId` values. |
| Reuse pooled prefabs | `GetFromPool(...)` / `RecycleToPool(...)` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Use for repeated runtime objects after preload. |

Avoid scattered Addressables strings, undocumented `WaitForCompletion`, and passing `AssetReference` through business runtime logic.

## UI

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| UI service | `IUIService` / `QUI` | `packages/com.eframework.core/Runtime/Services/IUIService.cs`, `packages/com.eframework.core/Runtime/UI/QUI.cs` | Create/open/release UI through framework services. |
| Controller base | `UIControllerBase<TView>` | `packages/com.eframework.core/Runtime/UI/UIControllerBase.cs` | Controllers own UI orchestration and access live views through `TypedViewHandle.TypedView`. |
| Access the live typed view | `TypedViewHandle.TypedView` | `packages/com.eframework.core/Runtime/UI/Handles/TypedViewHandle.cs` | Prefer the typed handle view access point instead of facade shortcuts. |
| View wrapper base | `BindingViewBase` | `packages/com.eframework.core/Runtime/UI/BindingViewBase.cs` | Generated/project views should stay thin and parameterless, using `SetBinding(...)` and `OnBindingSet()`. |
| Runtime view handle | `UIViewHandle<TView>` | `packages/com.eframework.core/Runtime/UI/Handles/UIViewHandle.cs` | Handles open/close/cache/release state and interruption-safe transitions. |
| Prefab binding component | `QUIBinding` | `packages/com.eframework.core/Runtime/UI/QUIBinding.cs` | Prefabs should include binding/config data consumed by view wrappers. |
| UI overlay camera participation | `EFrameSceneCamera` | `packages/com.eframework.core/Runtime/EFrameSceneCamera.cs` | Add to runtime scene cameras that should host the framework UI overlay stack. |

Avoid hand-built persistent top-level Canvas/EventSystem objects and direct `BindingViewBase` lifecycle driving.

## Data

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Data service | `IDataService` | `packages/com.eframework.core/Runtime/Services/IDataService.cs` | Register and retrieve tables through `Context.Data` / `EFrame.Current.Data`. |
| Register a table type | `RegisterTable<T>()` | `packages/com.eframework.core/Runtime/Services/IDataService.cs` | Reuse existing registered tables instead of constructing ad-hoc table instances. |
| Persistent table base | `DataTable<TData>` | `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Override `GetStorageKey()`, `GetDefaultData()`, and `Migrate(...)` when schema changes. |
| Dirty-aware mutation | `SetValue(...)` / `Mutate(...)` | `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Expose table-owned methods/properties; do not mutate raw data from outside. |
| Load/save diagnostics | `LastLoadResult` / `LastSaveResult` | `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Branch on structured status/reason codes, not log strings. |
| Trigger explicit saves | `SaveAll()` / table `Save()` | `packages/com.eframework.core/Runtime/Services/IDataService.cs`, `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Use when the project needs persistence at explicit business checkpoints. |

Avoid deriving persistence identity from class names or namespaces.

## Events

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Scoped event subscription | `Context.Events.SubscribeScoped<T>(...)` | `packages/com.eframework.core/Runtime/Event/IEventService.cs` | Store and dispose subscriptions at leave/destroy/controller cleanup boundaries. |
| Dispatch framework/project events | `Context.Events.Dispatch(evt)` | `packages/com.eframework.core/Runtime/Event/IEventService.cs` | Events are value types implementing `IEvent`. |

Avoid unpaired manual subscribe/unsubscribe when a scoped subscription is available.

## Audio And Vibration

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Audio service | `IAudioService` / `AudioManager` | `packages/com.eframework.core/Runtime/Audio/AudioManager.cs` | Use through `Context.Audio`; project setup creates mixer resources. |
| Audio events | `IAudioEventService` / `AudioEventManager` | `packages/com.eframework.core/Runtime/Audio/AudioEventManager.cs` | Routes configured audio event playback and vibration integration. |
| Vibration service | `IVibrationService` | `packages/com.eframework.core/Runtime/Vibration/QVibration.cs` | Use through `Context.Vibration`. |

Keep framework-owned audio config under `Assets/Resources/Audio`.

## Editor Bootstrap

| Need | Use | Source | Notes |
| --- | --- | --- | --- |
| Full project initialization | `EFrameProjectInitializationWindow` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameProjectInitializationWindow.cs` | Unity menu entry for cold-start/bootstrap tasks. |
| Addressables groups and ResPath generation | `EFrameAddressablesBootstrapUtility` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAddressablesBootstrapUtility.cs` | Owns managed resource group sync and `ResPath.Generated`. |
| Managed resource report UI | `EFrameAddressablesReportWindow` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAddressablesReportWindow.cs` | Use to select directories for generated ResPath entries and inspect issues. |
| Build preflight | `EFrameAddressablesBuildPreprocessor` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAddressablesBuildPreprocessor.cs` | Fails builds when managed Addressables or generated paths drift. |
| Import/move/delete auto-sync | `EFrameAppResAddressablePostprocessor` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAppResAddressablePostprocessor.cs` | Keeps managed resource changes aligned with Addressables automation. |

Avoid text-rewriting Unity `.unity`, `.prefab`, or `.asset` files when Editor APIs can make narrow serialized changes.
