# EFrameWork Codex Guide

This repository is the canonical EFrameWork framework source. EFrameWork is a lightweight Unity game framework plus a synchronized AI collaboration layer and project cold-start/update toolchain.

Codex should treat this file as the repo-level entry point. GitHub Copilot reads `.github/copilot-instructions.md`; Codex reads `AGENTS.md`. Keep both aligned when the framework AI contract changes.

## Core Rules

- Follow the EFrame framework rules in `.github/copilot-instructions.md` for Unity startup, `Procedure`, `QUI`, `UIController`, resource paths, Addressables, directory structure, and AI sync boundaries.
- Keep runtime code under clear Unity project structure such as `Assets/App/Runtime`, `Assets/App/Res`, and `Assets/Modules/*`; do not introduce temporary directories for production code.
- Use `Procedure` for state switching and lifecycle orchestration only. Put page, popup, and gameplay behavior in the appropriate UI/controller/module layer.
- Manage UI through `QUI`, `UIController`, and `UIViewHandle`; do not bypass the framework by hand-building persistent top-level Canvas or EventSystem objects in scenes.
- Centralize resource paths through `ResPath`, `ResPath.Generated`, `AssetReference`, or an equivalent module path center. Avoid scattered raw Addressables strings.
- Keep Unity scene, prefab, and `.asset` edits narrow and avoid unrelated serialization churn.

## AI Layer Maintenance

- The AI collaboration layer is a first-class framework surface. When framework code, templates, startup flow, directory rules, resource rules, or sync tools change, review the matching `.github` instructions, skills, manifest, scripts, and docs in the same change.
- Bump `.github/eframe-ai.manifest.json` whenever `AGENTS.md`, `.github/copilot-instructions.md`, `.github/instructions/`, `.github/skills/`, AI sync scripts, cold-start scripts, or AI setup/release docs change.
- `eframe-*` files are framework-managed and synced to business projects.
- `project-*` files are business-project overlays and must not be created in the framework repo.
- `maintainer-*` files are framework-repository-only and must not be synced to business projects.

## Codex Skill Discovery

Codex does not automatically load GitHub Copilot workspace skills. When a task matches one of these workflows, read the corresponding `SKILL.md` before making changes:

- Feature scaffolding and cross-cutting gameplay work: `.github/skills/eframe-feature-bootstrap/SKILL.md`
- EFrame guideline review and regression checks: `.github/skills/eframe-guideline-audit/SKILL.md`
- UI pages, popups, prefabs, controllers, bindings: `.github/skills/eframe-ui-feature/SKILL.md`
- Persistent data tables, dirty state, migration, save/load results: `.github/skills/eframe-data-table/SKILL.md`
- Resources, Addressables, `ResPath`, async handles: `.github/skills/eframe-resource-flow/SKILL.md`
- Framework AI/release/sync/template maintenance: `.github/skills/maintainer-ai-contract/SKILL.md`

For architecture, setup, and release boundaries, see `EFRAME_AI_ARCHITECTURE.md`, `EFRAME_AI_SETUP.md`, and `EFRAME_AI_RELEASE_CHECKLIST.md`.
