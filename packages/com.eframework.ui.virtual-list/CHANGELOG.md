# Changelog

All notable changes to the `com.eframework.ui.virtual-list` Unity package are documented in this file.

## [0.1.3] - 2026-05-06

### Fixed

- Activated newly cloned pooled items even when the source template object is inactive.
- Deferred list and grid visible-item refresh when the viewport has not received its first valid layout size yet.

## [0.1.2] - 2026-05-06

### Fixed

- Fixed `QVirtualListView` resize handling so stale content offsets are clamped back into the resized viewport before visible items refresh.
- Added explicit `RefreshLayout()` methods for list and grid views so callers can request a layout refresh after external layout changes.

## [0.1.1] - 2026-05-03

### Added

- Added auto cross-axis wrapping for `QVirtualGridView`, allowing vertical grids to reflow columns by viewport width while keeping the configured scroll direction.

## [0.1.0] - 2026-05-03

### Added

- Added the optional virtual list package with `QVirtualListView`, `QVirtualGridView`, pooled item views, and adapter-driven binding contracts migrated out of `com.eframework.core`.
- Normalized migrated APIs under `EFramework.Extensions.UI.VirtualList`.
