# EFrame AI API Index

This index is a compact map of stable EFrame APIs for business-project AI. Use it to choose framework entry points before generating or refactoring code. It is not a workflow checklist.

After AI sync, business projects receive this document at `.github/eframe/EFRAME_AI_API_INDEX.md`.

## Runtime Access

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Access initialized framework services | `EFrame.UI`, `EFrame.Assets`, `EFrame.Data`, `EFrame.Events`, `EFrame.Audio` | `packages/com.eframework.core/Runtime/EFrame.cs` | Use only after `EFrame.Initialize(...)`; inside framework-aware types prefer injected `Context`. |
| Read the full initialized service bundle | `EFrame.Current` | `packages/com.eframework.core/Runtime/EFrame.cs` | Use when a caller needs the whole `EFrameContext`. |
| Read service bundle from injected code | `EFrameContext` | `packages/com.eframework.core/Runtime/EFrameContext.cs` | Owns common services, cameras, coroutine host, and FPS. |
| Receive context in framework-aware objects | `IEFrameContextAware.BindContext(...)` | `packages/com.eframework.core/Runtime/EFrameBehaviour.cs` | Avoid repeated global service lookup after context injection. |

Avoid calling `EFrame.*` shortcuts from constructors or code that can run before initialization.

## Startup And Procedure

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Boot the framework in a scene | `EFrameComponent` | `packages/com.eframework.core/Runtime/EFrameComponent.cs` | Startup scene host, normally paired with `EFrameProcedureComponent`. |
| Own app/game state switching | `EFrameProcedure` | `packages/com.eframework.core/Runtime/Procedure/EFrameProcedure.cs` | State transitions and preload orchestration belong here; feature UI/details usually belong elsewhere. |
| Register and start procedures | `EFrameProcedureComponent` | `packages/com.eframework.core/Runtime/Procedure/EFrameProcedureComponent.cs` | Use framework-owned procedure switching. |
| Pass one-shot transition data | `ProcedureEnterContext` | `packages/com.eframework.core/Runtime/Procedure/ProcedureEnterContext.cs` | Persistent state belongs in data tables, services, or events. |

Avoid adding a new procedure for simple page, popup, or prompt changes.

## Assets And Resources

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Runtime asset service | `IAssetService` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Access through `Context.Assets` or `EFrame.Assets`. |
| Use generated managed asset ids | `ResPath.Generated` | `Assets/App/Runtime/Generated/Res/ResPath.Generated.cs` | Use generated ids instead of handwritten Addressables strings in business runtime code. |
| Procedure-owned preload scope | `IAssetPreloadScope` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Releases when the owning procedure leaves. |
| Load an asset with explicit ownership | `LoadAsync<T>(assetId)` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Dispose the returned `AssetHandle<T>` at the owner lifecycle boundary. |
| Instantiate by generated id | `Instantiate(assetId, parent)` / `InstantiateAsync(assetId, parent)` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Use `ResPath.Generated` for managed resource ids. |
| Reuse pooled prefabs | `GetFromPool(...)` / `RecycleToPool(...)` | `packages/com.eframework.core/Runtime/Services/IAssetService.cs` | Use for repeated runtime objects after preload. |

Avoid scattered Addressables strings, undocumented `WaitForCompletion`, and passing `AssetReference` through business runtime logic.

## UI

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Business UI entry | `EFrame.UI`, `UIControllerBase<TGeneratedView>` | `packages/com.eframework.core/Runtime/EFrame.cs`, `packages/com.eframework.core/Runtime/UI/UIControllerBase.cs` | Business code should write controllers and call `Show()` / `Hide()`. |
| Access the live generated view | `CurrentView` | `packages/com.eframework.core/Runtime/UI/UIControllerBase.cs` | Use inside controllers instead of touching `UIViewHandle`. |
| Generated View access class | UI Binding generated class under `EFramework.Generated.UI` | `packages/com.eframework.core/Editor/UI/UIAutoBindingEditor.cs`, `packages/com.eframework.core/Runtime/UI/BindingViewBase.cs` | Generated from prefab `QUIBinding`; normal business UI should not hand-write View wrappers. |
| Prefab binding component | `QUIBinding` | `packages/com.eframework.core/Runtime/UI/QUIBinding.cs` | Holds binding/config data such as default layer, cache, and animation root. |
| Advanced/internal UI host | `IUIService` / `QUI`, `UIViewHandle<TView>` | `packages/com.eframework.core/Runtime/Services/IUIService.cs`, `packages/com.eframework.core/Runtime/UI/QUI.cs`, `packages/com.eframework.core/Runtime/UI/Handles/UIViewHandle.cs` | Direct use is for framework internals, advanced managers, or lifecycle debugging. |
| UI overlay camera participation | `EFrameSceneCamera` | `packages/com.eframework.core/Runtime/EFrameSceneCamera.cs` | Add to scene cameras that should host the framework UI overlay stack. |
| Safe area content fitting | `SafeAreaFitter` | `packages/com.eframework.core/Runtime/UI/Layout/SafeAreaFitter.cs` | Put on prefab content containers that should stay inside safe areas. |
| Full-screen background fitting | `FullScreenFitter` | `packages/com.eframework.core/Runtime/UI/Layout/FullScreenFitter.cs` | Put on prefab backgrounds or masks that should extend beyond the fitted UI area. |
| Optional UI extras | `com.eframework.ui-extras` | `packages/com.eframework.ui-extras/Runtime` | Includes reusable controls, graphics helpers, interaction Guard/Reporter/Feedback, and pooled virtual list/grid. Virtual list/grid types keep `EFramework.Extensions.UI.VirtualList`. |
| Optional PlayMode AI validation | `com.eframework.ai-loop` | `packages/com.eframework.ai-loop/Editor` | Optional editor-only package for screenshots, UI input simulation, keyboard simulation, and input record/replay. |

Avoid hand-built persistent top-level Canvas/EventSystem objects, hand-written normal View wrappers, and direct business `UIViewHandle` ownership.

## Tween

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Framework-owned simple tweens | `EFrameTween` | `packages/com.eframework.core/Runtime/Tween/EFrameTween.cs` | Core runtime tween entry. |
| Configure a tween | `EFrameTweenOptions` | `packages/com.eframework.core/Runtime/Tween/EFrameTweenOptions.cs` | Supports target binding, update mode, ease, ignore-time-scale, and callbacks. |
| Track or cancel a tween | `IEFrameTweenHandle` | `packages/com.eframework.core/Runtime/Tween/IEFrameTweenHandle.cs` | Kill or await at the owner lifecycle boundary. |
| Optional DOTween backend | `EFRAME_USE_DOTWEEN` + initialization-window `Extensions` Apply controls | `packages/com.eframework.core/Runtime/Tween/Dotween`, `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameDotweenBootstrapUtility.cs` | Enable only after DOTween is installed; DOTween is not a core dependency. |

Avoid referencing `DG.Tweening` from core-owned runtime code outside the optional adapter assembly.

## Data

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Data service | `IDataService` | `packages/com.eframework.core/Runtime/Services/IDataService.cs` | Register and retrieve tables through `Context.Data` / `EFrame.Data`. |
| Register a table type | `RegisterTable<T>()` | `packages/com.eframework.core/Runtime/Services/IDataService.cs` | Reuse registered tables instead of creating ad-hoc table instances. |
| Persistent table base | `DataTable<TData>` | `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Owns storage key, default data, migration, dirty state, and save/load result state. |
| Dirty-aware mutation | `SetValue(...)` / `Mutate(...)` | `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Expose table-owned methods/properties; do not mutate raw data from outside. |
| Load/save diagnostics | `LastLoadResult` / `LastSaveResult` | `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Branch on structured status/reason codes, not log strings. |
| Trigger explicit saves | `SaveAll()` / table `Save()` | `packages/com.eframework.core/Runtime/Services/IDataService.cs`, `packages/com.eframework.core/Runtime/DataStorage/DataTable.cs` | Use at explicit business checkpoints. |

Avoid deriving persistence identity from mutable class names or namespaces.

## Events

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Scoped event subscription | `Context.Events.SubscribeScoped<T>(...)` / `EFrame.Events.SubscribeScoped<T>(...)` | `packages/com.eframework.core/Runtime/Event/IEventService.cs` | Dispose subscriptions at leave/destroy/controller cleanup boundaries. |
| Dispatch framework/project events | `Context.Events.Dispatch(evt)` / `EFrame.Events.Dispatch(evt)` | `packages/com.eframework.core/Runtime/Event/IEventService.cs` | Events are value types implementing `IEvent`. |

Avoid unpaired manual subscribe/unsubscribe when scoped subscription is available.

## Audio

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Audio service | `IAudioService` / `AudioManager` | `packages/com.eframework.core/Runtime/Audio/AudioManager.cs` | Use through `Context.Audio` or `EFrame.Audio`. |

Keep framework-owned audio mixer config under `Assets/Resources/Audio`.

## Editor Bootstrap

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Full project initialization | `EFrameProjectInitializationWindow` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameProjectInitializationWindow.cs` | Main Unity Editor entry for EFrame project setup and repair. |
| Install minimal startup skeleton | `Initialize / Repair Project` | `packages/com.eframework.core/Editor/Templates/Basic` | Installs the Basic template and syncs EFrame AI contracts; use the initialization window rather than manual template copying. |
| Install optional extension packages | `Extensions` + `Apply` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameProjectInitializationWindow.cs` | Adds checked optional EFrame packages, resolves dependencies, and applies the DOTween Adapter checkbox. |
| Install optional extension demos | `Install Showcase` | `packages/com.eframework.core/Editor/Templates/Modules/EFrameExtensionShowcase` | Adds the optional Showcase module and required extension packages. |
| Addressables groups and ResPath generation | `EFrameAddressablesBootstrapUtility` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAddressablesBootstrapUtility.cs` | Owns managed group sync and `ResPath.Generated`. |
| Managed resource report UI | `EFrameAddressablesReportWindow` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAddressablesReportWindow.cs` | Select generated ResPath directories and inspect managed resource issues. |
| Build preflight | `EFrameAddressablesBuildPreprocessor` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAddressablesBuildPreprocessor.cs` | Fails player builds when managed Addressables or generated paths drift. |
| Import/move/delete auto-sync | `EFrameAppResAddressablePostprocessor` | `packages/com.eframework.core/Editor/ProjectBootstrap/EFrameAppResAddressablePostprocessor.cs` | Keeps managed resource changes aligned after asset changes. |

Prefer direct, narrow, reviewable YAML edits for Unity `.unity`, `.prefab`, and `.asset` files. Use Unity Editor APIs or other fallback paths only when direct YAML editing cannot complete, cannot preserve references, or validation shows abnormal serialized state.

## Optional Templates And Demos

| Need | Use | Source | Note |
| --- | --- | --- | --- |
| Minimal new-project baseline | Basic template | `packages/com.eframework.core/Editor/Templates/Basic` | Business projects install this through the initialization window. |
| Extension demo launcher | `ProcedureEFrameExtensionShowcaseEntry` | `packages/com.eframework.core/Editor/Templates/Modules/EFrameExtensionShowcase` | Optional module demo; keep it isolated under `Assets/Modules/EFrameExtensionShowcase`. |
