# Changelog

All notable changes to the `com.eframework.debug-console` Unity package are documented in this file.

## [Unreleased]

### Added

- Added the optional debug console package by migrating Unity Ingame Debug Console `1.8.2` out of `com.eframework.core`.
- Added `EFrameDebugConsole.Show()` for runtime code that should instantiate and show the console panel on demand.

### Changed

- Replaced vendored Ingame Debug Console TextMeshPro UI fields, input fields, and prefab components with standard UGUI `Text` and `InputField` components.
