# Changelog

## 0.3.1 - 2026-05-07

- Merged virtual list/grid controls into UI Extras while keeping the `EFramework.Extensions.UI.VirtualList` namespace.
- Replaced legacy button/toggle helpers with `UIInteractionGuard`, `UIInteractionReporter`, and `UIInteractionFeedback`.
- Refactored `UIInteractionFeedback` into a compact setting-node Inspector: target tree dropdown, state tabs, action dropdown, action parameters, and optional tween timing for supported actions; removed the old per-state legacy feedback model.
- Fixed duplicate target tween restore, disable-time tween cleanup, and active-state restore during Unity deactivate callbacks.
- Replaced `SmoothFillController` with `UISmoothFill`.
- Removed TextPro curved text helpers and legacy `UIBuilder` / local UI animation helper components.

## 0.2.2 - 2026-05-03

- Fixed `CheckableButton` state object handling so reused state references stay active when any matching state should be visible.

## 0.2.1 - 2026-05-03

- Replaced `StateButton` with `CheckableButton`, a single-Button checked/focused state component for mouse, keyboard, and gamepad-friendly UI.
- Updated `Tabbar` to use explicit or child-collected `CheckableButton` items.

## 0.2.0 - 2026-05-03

- Renamed the button tab selector component to `Tabbar` and its runtime script to `Components/Tabbar`.
- Added `StateButton` as a reusable normal/selected switch between two UGUI Button variants.
- Expanded `Tabbar` into a TabBar-style selector built from explicit or child-collected `StateButton` items.

## 0.1.1 - 2026-05-03

- Removed the runtime DOTween dependency from UI helper and animation components; UI Extras now uses the core `EFrameTween` facade.

## 0.1.0

- Added optional UI extras package.
- Moved `Tabbar`, `EmptyRayCasterGraphic`, `UIHelper`, and `UIAnimation` out of `com.eframework.core`.
- Normalized migrated UI helper components under `EFramework.Extensions.UI.Extras`.
