# EFrame Extension Showcase Module

Extension Showcase is an optional project module installed to:

```text
Assets/Modules/EFrameExtensionShowcase/
```

Install it from Unity with `EFrame Tools/项目初始化向导` and choose `Install Showcase`. The initializer copies the module from `Editor/Templates/Modules/EFrameExtensionShowcase`, tries to add sibling local extension packages in framework development checkouts, and can set the StartUp scene to enter `ProcedureEFrameExtensionShowcaseEntry`.

When maintaining the template, edit and debug the source module in `test-fixtures/EFrameShowcaseUnity/Assets/Modules/EFrameExtensionShowcase`, then run `tools/Sync-EFrameShowcaseTemplate.ps1` from the repository root to generate the package template.

The current module provides a stable runtime UI, demo registry, and placeholder entries for:

- UI Virtual List
- UI Extras
- Effects
- GMTools
- Debug Console

Each entry is intentionally lightweight so future extension-specific demos can be added without changing the Basic startup skeleton.
