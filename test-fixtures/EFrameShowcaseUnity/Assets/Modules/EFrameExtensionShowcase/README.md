# EFrame Extension Showcase

This optional module is installed by `EFrame Tools/项目初始化向导` into:

```text
Assets/Modules/EFrameExtensionShowcase/
```

It provides a stable entry Procedure and prefab-backed runtime UI for focused extension demos.

Current entries:

- UI Virtual List: opens the module-owned runtime sample window backed by `com.eframework.ui.virtual-list`.
- Debug Console: shows the runtime console panel provided by `com.eframework.debug-console`.

`Install Showcase` installs `com.eframework.ui.virtual-list` and `com.eframework.debug-console` before copying this module, so the module can keep direct, readable sample code while Core itself stays free of extension runtime dependencies.

Framework maintainers edit and debug this source module directly in `test-fixtures/EFrameShowcaseUnity`, including `Res/UI/Panels/EFrameExtensionShowcase/EFrameExtensionShowcaseView.prefab`, then run `tools/Sync-EFrameShowcaseTemplate.ps1` from the repository root to generate the package template under `packages/com.eframework.core/Editor/Templates/Modules/EFrameExtensionShowcase`.
