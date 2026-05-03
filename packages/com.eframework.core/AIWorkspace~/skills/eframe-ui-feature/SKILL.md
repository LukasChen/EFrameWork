---
name: eframe-ui-feature
description: 'Build or refactor EFrame UI pages, popups, tooltips, View prefabs, UIController classes, QUIBinding usage, UI layer placement, and UI lifecycle wiring. Use when adding or fixing EFrame runtime UI features in a business project.'
argument-hint: 'Describe the UI feature, target layer, prefab/controller names, and whether it is a page, popup, tooltip, or top-layer UI.'
user-invocable: true
capabilities:
  - eframe.ui.page
  - eframe.ui.popup
  - eframe.ui.controller
  - eframe.ui.binding
owns:
  - prefab UI layer configuration
  - UIController and View wrapper lifecycle
  - UI prefab naming and resource placement
delegatesTo:
  - eframe-directory-structure
  - eframe-resource-flow
  - eframe-guideline-audit
outputs:
  - EFrame.UI/UIControllerBase-first UI wiring
  - generated thin View access class usage
  - prefab/resource path alignment
forbiddenPatterns:
  - hand-built persistent runtime Canvas or EventSystem
  - large-scale runtime new GameObject assembly for static UI
  - UIControllerBase View facade usage
  - hand-written business View wrapper for normal bound UI
  - direct business UIViewHandle ownership
  - assetPath constructors in generated View wrappers
---

# EFrame UI Feature

## Workflow

1. Decide the UI type before creating files: page, popup, tooltip, top-layer prompt, or existing Procedure UI state.
2. Use framework naming: `XxxView.prefab`, `XxxViewController`, generated binding under `EFramework.Generated.UI`.
3. Place the prefab in the matching resource convention, usually `Assets/App/Res/UI/...` or `Assets/Modules/<Name>/Res/UI/...`; if page/popup/widget/common ownership is unclear, use `eframe-directory-structure`; if Addressables sync or preload/release ownership is the main problem, delegate that part to `eframe-resource-flow`.
4. Configure the prefab `DefaultLayer`: main pages to `QuiPanel`, popups to `QuiPopUp`, hints to `QuiTooltip` or `QuiTop`.
5. Build static UI structure, layout, sliced-image assembly, nine-slice settings, button hierarchy, and visual states in Unity prefabs first.
6. Put controller code in runtime structure. Use `OnViewCreated()` / `OnViewDestroyed()` for instance-level binding and release, and `OnViewOpened()` / `OnViewClosed()` for per-open refresh or pause behavior.
7. Keep controllers focused on event binding, data refresh, state switching, and necessary dynamic list/item instantiation; do not use large-scale `new GameObject` assembly for static UI.
8. For repeated runtime-generated elements, prefer prefab Templates already present in the View prefab or standalone Widget prefabs.
9. For business-facing code, route UI through `EFrame.UI` and `UIControllerBase`; do not introduce `IUIService`, `QUI`, or direct `UIViewHandle` ownership unless the task is explicitly framework internals or advanced extension work.
10. Treat View wrappers as generated access classes: keep reusable business state outside generated View classes, and access runtime views from controllers through `CurrentView`.
11. Reference UI prefabs through generated `ResPath.Generated` ids; do not scatter prefab address strings or add handwritten path wrappers.

## Output Checks

- Confirm the UI does not require a new `Procedure` unless it changes gameplay or scene-level state.
- Confirm framework UI SortingLayer bootstrap exists before relying on UI ordering.
- Confirm every event subscription, button listener, async handle, and controller-owned resource has a matching cleanup path at the right lifecycle scope.
- Confirm generated Views do not rely on `assetPath` constructors or a controller `View` facade.
- Confirm static UI has been authored in prefabs, and runtime `new GameObject` usage is limited to necessary dynamic/repeated content.
- Confirm any camera intended to host the framework UI overlay uses `EFrameSceneCamera` instead of per-frame polling or scene-specific UI camera stack code.
- For larger UI feature work, read [ui-checklist](./references/ui-checklist.md).
