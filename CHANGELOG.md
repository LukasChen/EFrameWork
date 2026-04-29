# Changelog

All notable changes to EFrameWork are documented in this file.

EFrameWork uses semantic versioning for framework releases. The release version is recorded in `packages/com.eframework.core/package.json` and summarized here. Unity code, AI collaboration rules, bootstrap tools, sync scripts, and documentation are treated as one EFrameWork release surface. `.github/eframe-ai.manifest.json` is only an internal sync marker for business projects.

## [0.1.0] - 2026-04-29

### Added

- Established EFrameWork as a lightweight Unity game framework with a synchronized AI collaboration layer.
- Added framework-managed Copilot instructions, focused runtime/editor instructions, and EFrame workflow skills.
- Added project cold-start tooling for standard directories, bootstrap code, AI workspace sync, project overlay, and project updater installation.
- Added AI workspace sync preview with managed file, project overlay, stale item, and synced document reporting.
- Added cold-start summary output with `OK`, `SKIP`, and `WARN` status reporting.
- Added AI release check tooling to verify manifest updates and `eframe-*` / `project-*` naming boundaries.
- Added EFrame AI architecture, setup, release checklist, directory structure, and resource path convention documentation.

### Changed

- Standardized runtime service access around `EFrame.Current` and injected `Context` instead of legacy direct service statics.
- Updated bootstrap templates to use current runtime service access patterns.
- Formalized AI workspace versioning and release maintenance rules.

### Fixed

- Fixed generated `ProcedureHome` bootstrap code so it no longer references the removed `EFrame.UI` shortcut.
- Fixed runtime service interface implementation issues found during the initial framework pass.