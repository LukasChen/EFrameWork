# Changelog

All notable changes to the `com.eframework.debug-console` Unity package are documented in this file.

## [Unreleased]

### Added

- Added the optional debug console package by migrating Unity Ingame Debug Console `1.8.2` out of `com.eframework.core`.
- Added `EFrameDebugConsole.Show()` for runtime code that should instantiate and show the console panel on demand.

### Changed

- Declared TextMeshPro as an external dependency supplied by `com.unity.ugui`; this package does not redistribute TextMeshPro plugin files.
