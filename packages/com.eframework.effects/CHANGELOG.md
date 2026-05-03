# Changelog

All notable changes to the `com.eframework.effects` Unity package are documented in this file.

## [0.1.1] - 2026-05-03

### Added

- Added the optional effects package by migrating fly animation, icon bounce, camera shake, and their editor tooling out of `com.eframework.core`.
- Normalized migrated effect APIs under `EFramework.Extensions.Effects`.

### Changed

- Removed the runtime DOTween dependency from camera shake; effects now use the core `EFrameTween` facade.
