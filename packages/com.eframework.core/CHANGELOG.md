# Changelog

All notable changes to the `com.eframework.core` Unity package are documented in this file.

## [0.1.0] - 2026-04-29

### Added

- Initial formal EFrameWork Core Unity package release.
- Added runtime framework entry through `EFrame.Current` and `EFrameContext` services.
- Added runtime modules for UI, assets, audio, data storage, events, coroutine helpers, effects, vibration, and utility services.
- Added editor tooling for project initialization, Addressables setup, ResPath generation, sample UI prefab import, audio setup, UI binding, and effect debugging.
- Added package dependencies for Addressables, UGUI, Input System, TextMeshPro, and Unity Newtonsoft.Json.

### Changed

- Removed legacy direct service statics such as `EFrame.UI`, `EFrame.Audio`, and `EFrame.DataManager` from the runtime access model.
- Kept synchronous asset helpers as transitional APIs while documenting async handle-based service access as the preferred path.