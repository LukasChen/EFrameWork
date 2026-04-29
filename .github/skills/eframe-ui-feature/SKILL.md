---
name: eframe-ui-feature
description: 'Build or refactor EFrame UI pages, popups, tooltips, View prefabs, UIController classes, QUIBinding usage, UI layer placement, and UI lifecycle wiring. Use when adding or fixing EFrame runtime UI features in a business project.'
argument-hint: 'Describe the UI feature, target layer, prefab/controller names, and whether it is a page, popup, tooltip, or top-layer UI.'
user-invocable: true
---

# EFrame UI Feature

## Workflow

1. Decide the UI type before creating files: page, popup, tooltip, top-layer prompt, or existing Procedure UI state.
2. Use framework naming: `XxxView.prefab`, `XxxViewController`, generated binding under `EFrameWork.Runtime.UI.Generated`.
3. Place the prefab in the matching resource convention, usually `Assets/App/Res/UI/...` or `Assets/Modules/<Name>/Res/UI/...`.
4. Mount through `QUI` layers: main pages to `QuiPanel`, popups to `QuiPopUp`, hints to `QuiTooltip` or `QuiTop`.
5. Put controller code in runtime structure. Use `OnViewCreated()` / `OnViewDestroyed()` for instance-level binding and release, and `OnViewOpened()` / `OnViewClosed()` for per-open refresh or pause behavior.
6. Use `IUIService/QUI` and `UIViewHandle` semantics for create/open/close/cache/release. Do not directly drive `BindingViewBase` lifecycle from business code.
7. Keep View wrappers thin and parameterless: cache generated binding/components in `OnBindingSet()`, keep reusable business state outside the View wrapper, and access runtime views from controllers through `TypedViewHandle.TypedView`.
8. Use centralized resource paths through `ResPath` / `ResPath.Generated` / `AssetReference`; do not scatter prefab address strings.

## Output Checks

- Confirm the UI does not require a new `Procedure` unless it changes gameplay or scene-level state.
- Confirm `QUI` SortingLayer bootstrap exists before relying on UI ordering.
- Confirm every event subscription, button listener, async handle, and controller-owned resource has a matching cleanup path at the right lifecycle scope.
- Confirm generated or hand-written Views do not rely on `assetPath` constructors or a legacy controller `View` facade.
- For larger UI feature work, read [ui-checklist](./references/ui-checklist.md).
