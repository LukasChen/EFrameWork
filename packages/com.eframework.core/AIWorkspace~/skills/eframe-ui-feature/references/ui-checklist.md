# EFrame UI Checklist

## Placement

- Page: `QuiPanel`
- Popup: `QuiPopUp`
- Tooltip or short hint: `QuiTooltip`
- Highest-priority overlay: `QuiTop`

## Files

- Runtime controller: `Assets/App/Runtime/UI/Controllers` or `Assets/Modules/<Name>/Runtime/UI`
- Runtime view wrapper: `Assets/App/Runtime/UI/Views` or module runtime UI path
- Prefab: `Assets/App/Res/UI/...` or `Assets/Modules/<Name>/Res/UI/...`
- Generated binding namespace: `EFrameWork.Runtime.UI.Generated`

## Lifecycle

- View wrappers have parameterless constructors and initialize prefab references from `OnBindingSet()`
- Controllers access the active View through `TypedViewHandle.TypedView`
- `OnViewCreated()` / `OnViewDestroyed()` are used for instance-level binding and release
- `OnViewOpened()` / `OnViewClosed()` are used for per-open refresh and pause behavior
- Cached UI can close into `UIViewHandleState.Closed` without running release cleanup

## Avoid

- Hand-created persistent top-level Canvas or EventSystem in scene content
- Deep `transform.Find` in controller logic when `QUIBinding` can expose nodes
- Controller constructors that read game data or framework services before the UI is shown
- Caching controller state as if it were a reusable view instance
- View constructors that load prefab assets directly from an `assetPath`
- Reintroducing a controller `View` facade instead of using `TypedViewHandle`
