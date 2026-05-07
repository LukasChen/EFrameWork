---
name: eframe-directory-structure
description: 'Classify and validate EFrame Unity project file placement. Use before creating, moving, or reviewing business runtime code, editor tooling, UI prefabs, generated files, scenes, resources, FX, materials, module-owned files, or when the task mentions 目录归位, 文件放哪, 模块归属, 目录审查, or 资源放哪.'
argument-hint: 'Describe the file, feature, resource, module, or directory change to place or audit.'
user-invocable: true
capabilities:
  - eframe.directory.classify
  - eframe.directory.audit
  - eframe.module.boundary
  - eframe.resource.ownership
owns:
  - Unity project directory placement decisions
  - App versus Module ownership boundaries
  - generated-code and managed-resource directory routing
delegatesTo:
  - eframe-ui-feature
  - eframe-resource-flow
  - eframe-guideline-audit
outputs:
  - target directory recommendation
  - ownership and sharing rationale
  - follow-up specialized skill handoff
forbiddenPatterns:
  - scattering new scripts under Assets root
  - mixing Editor code into Runtime directories
  - putting generated code into handwritten business directories
  - moving module-private resources into App shared directories prematurely
---

# EFrame Directory Structure

## Workflow

1. Classify the artifact first: runtime code, editor tooling, generated code, UI prefab, shared resource, scene file, scene dependency, FX, material/shader, data/config, or module-owned content.
2. Decide ownership before path shape: use `Assets/App/...` for main-app or cross-module shared content, and `Assets/Modules/<Name>/...` for independent gameplay, subsystem, or module-private content.
3. Keep runtime and editor code separate: main-app runtime under `Assets/App/Runtime/...`, module runtime under `Assets/Modules/<Name>/Runtime/...`, main-app editor tooling under `Assets/App/Editor/...`, and module editor tooling under `Assets/Modules/<Name>/Editor/...`.
4. Put generated code only in generated directories: App generated code under `Assets/App/Runtime/Generated/...`; module generated code under `Assets/Modules/<Name>/Runtime/Generated/...`.
5. Put UI prefabs by UI type: pages in `Res/UI/Panels/<PanelName>/`, popups in `Res/UI/Popups/<PopupName>/`, reusable item prefabs in `Res/UI/Widgets/<WidgetName>/`, and cross-UI shared assets in `Res/UI/Common/...`.
6. Keep scene files and scene dependencies distinct: `.unity` files go under `Assets/Scenes/...` or `Assets/Modules/<Name>/Scenes/...`; scene-owned prefabs, textures, lighting, and config go under `Res/SceneAssets/<SceneName>/...`.
7. Keep startup resources small and explicit: only startup-required, always-needed lightweight assets belong in `Assets/App/Res/Bootstrap/...`.
8. Place resources by owner and reuse scope: module-private assets stay under `Assets/Modules/<Name>/Res/...`; cross-module shared assets can move to `Assets/App/Res/...`; UI-specific materials stay with UI, scene-specific materials stay with `SceneAssets`, and broadly shared materials/shaders go to `Assets/App/Res/Materials` or `Assets/App/Res/Shaders`.
9. For Addressables-managed resources, stay inside managed roots: `Assets/App/Res/...`, `Assets/Scenes/...`, `Assets/Modules/<Name>/Res/...`, or `Assets/Modules/<Name>/Scenes/...`; use `eframe-resource-flow` for ResPath, group sync, preload, and release decisions.
10. If the task includes UI lifecycle, controller, binding, or layer setup, use `eframe-ui-feature`; if it is an audit after implementation, use `eframe-guideline-audit`.

## Output Checks

- State the chosen target path and why App or Module owns it.
- Note whether the path is framework-managed for Addressables or generated-code sync.
- Flag any project-specific directory exception as project-owned instruction content, not an edit to synced `eframe-*` files.
- If no new path is needed, keep edits in the existing file's current ownership boundary.
