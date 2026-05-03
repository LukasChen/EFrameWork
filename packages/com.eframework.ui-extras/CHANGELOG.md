# Changelog

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
