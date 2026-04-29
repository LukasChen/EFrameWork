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

## Avoid

- Hand-created persistent top-level Canvas or EventSystem in scene content
- Deep `transform.Find` in controller logic when `QUIBinding` can expose nodes
- Controller constructors that read game data or framework services before the UI is shown
- Caching controller state as if it were a reusable view instance
