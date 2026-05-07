---
name: eframe-ui-feature
description: 'Build or refactor EFrame UI pages, popups, tooltips, View prefabs, UIController classes, QUIBinding usage, UI layer placement, and UI lifecycle wiring. Use when adding or fixing EFrame runtime UI features, or when the task mentions UI 接入, 页面接入, 弹窗接入, UI 重构, or UI 规范.'
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
  - runtime AddComponent for standard static UI layout components
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
5. Build static UI structure, layout, sliced-image assembly, nine-slice settings, button hierarchy, text components, standard layout components, and visual states in Unity prefabs first.
6. For mobile safe-area, notch, or rounded-screen fitting, use `SafeAreaFitter` on content containers and `FullScreenFitter` on full-screen backgrounds or masks; do not read `Screen.safeArea` or hand-write safe-area offsets in business UI code unless modifying the framework layout components themselves.
7. Serialize predictable UI components such as `SafeAreaFitter`, `FullScreenFitter`, layout helpers, interaction components, buttons, and text onto prefabs/YAML. Runtime code may call existing component refresh methods, but should not `AddComponent` standard static UI components to compensate for missing prefab configuration.
8. When the project has TextMesh Pro installed, use `TextMeshProUGUI` / `TMP_Text` for new or refactored UI text instead of legacy `UnityEngine.UI.Text`.
9. Put controller code in runtime structure. Use `OnViewCreated()` / `OnViewDestroyed()` for instance-level binding and release, and `OnViewOpened()` / `OnViewClosed()` for per-open refresh or pause behavior.
10. Keep controllers focused on event binding, data refresh, state switching, and necessary dynamic list/item instantiation; do not use large-scale `new GameObject` assembly for static UI.
11. For repeated runtime-generated elements, prefer prefab Templates already present in the View prefab or standalone Widget prefabs.
12. For normal business controllers, instantiate `UIControllerBase` subclasses and call `Show()` / `Hide()` directly; do not add repeated `BindContext(Context)` calls unless the task is testing, framework internals, or a deliberate special-context override.
13. For popups, tutorials, reward summaries, or prompts that must display sequentially, enqueue controllers through `EFrame.UI.Queue` or `EnqueueToShow(...)` / `EnqueueToShowAsync(...)` instead of hand-rolled bool locks or callback chains. The queue auto-runs and advances when each controller closes; call `EFrame.UI.Queue.Pause()` before flow transitions when pending UI should be preserved.
14. For business-facing code, route UI through `EFrame.UI` and `UIControllerBase`; do not introduce `IUIService`, `QUI`, or direct `UIViewHandle` ownership unless the task is explicitly framework internals or advanced extension work.
15. Treat View wrappers as generated access classes: keep reusable business state outside generated View classes, and access runtime views from controllers through `CurrentView`.
16. Reference UI prefabs through generated `ResPath` ids; do not scatter prefab address strings or add handwritten path wrappers.

## Output Checks

- Confirm the UI does not require a new `Procedure` unless it changes gameplay or scene-level state.
- Confirm framework UI SortingLayer bootstrap exists before relying on UI ordering.
- Confirm every event subscription, button listener, async handle, and controller-owned resource has a matching cleanup path at the right lifecycle scope.
- Confirm normal business controller creation does not include redundant `BindContext(Context)` calls; reserve explicit binding for tests, framework internals, or special-context overrides.
- Confirm generated Views do not rely on `assetPath` constructors or a controller `View` facade.
- Confirm static UI has been authored in prefabs, and runtime `new GameObject` usage is limited to necessary dynamic/repeated content.
- Confirm safe-area and full-screen fitting use prefab-authored `SafeAreaFitter` / `FullScreenFitter` instead of business `Screen.safeArea` calculations or runtime `AddComponent` fixes.
- Confirm projects with TextMesh Pro installed use TMP UI text components instead of legacy `UnityEngine.UI.Text`.
- Confirm any camera intended to host the framework UI overlay uses `EFrameSceneCamera` instead of per-frame polling or scene-specific UI camera stack code.
- For larger UI feature work, read [ui-checklist](./references/ui-checklist.md).
