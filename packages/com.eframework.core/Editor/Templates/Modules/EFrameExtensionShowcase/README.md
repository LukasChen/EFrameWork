# EFrame Extension Showcase

This optional module is installed by `EFrame Tools/项目初始化向导` into:

```text
Assets/Modules/EFrameExtensionShowcase/
```

It provides a stable entry Procedure and prefab-backed runtime UI for extension demos. Current demo entries are lightweight placeholders that detect whether the expected extension assembly is loaded, then log the selected entry. Replace each stub with a focused extension demo as the extension packages mature.

The module intentionally avoids compile-time references to optional extension namespaces. The initializer still tries to install local sibling extension packages so the runtime project is ready for real demos.

Framework maintainers edit and debug this source module directly in `test-fixtures/EFrameShowcaseUnity`, including `Res/UI/Panels/EFrameExtensionShowcase/EFrameExtensionShowcaseView.prefab`, then run `tools/Sync-EFrameShowcaseTemplate.ps1` from the repository root to generate the package template under `packages/com.eframework.core/Editor/Templates/Modules/EFrameExtensionShowcase`.
