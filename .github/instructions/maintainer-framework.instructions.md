---
name: "EFrame Framework Maintainer Rules"
description: "Use when editing the EFrameWork framework repository itself: package runtime/editor code, AI sync scripts, framework templates, AI instructions/skills, or release/setup docs. This file is maintainer-only and is not synced into business projects."
applyTo: "{**/packages/com.eframework.core/**,**/tools/**/*.ps1,**/.github/**/*.md,**/EFRAME_AI_*.md,**/README.md}"
---

# EFrame Framework Maintainer Rules

This file is for framework maintainers working inside the EFrameWork repository.

Unlike `eframe-*.instructions.md`, this file is repository-local guidance and should not be treated as a business-project runtime contract.

## Sync Boundary

- `eframe-*` files are framework-managed public AI contract files and are synced into business projects.
- `project-*` files belong to business projects and must not exist in the framework repository.
- `maintainer-*` files are framework-repository-only guidance and must not be added to the AI sync whitelist.
- The same prefix boundary applies to skills: `eframe-*` skills are synced project-facing workflows, while `maintainer-*` skills are framework-repository workflows.
- When deciding where a rule belongs, first ask whether it should still make sense after cold-start into a business project. If not, it belongs here, not in `eframe-*`.

## What Belongs Here

- framework-internal migration notes
- temporary cleanup constraints while refactoring package code
- sync-boundary rules about which files are public contract vs repo-local
- release maintenance reminders about manifest bumps, setup docs, bootstrap scripts, or template regeneration
- internal structure rules that are meaningful for maintaining `packages/com.eframework.core` but are not business-project-facing usage rules
- editor-side migration rules such as temporary directory regrouping, legacy menu cleanup, or generator refactors that should not be pushed into business-project contract files

## What Must Stay Out Of Synced Contract Files

- one-off rename cleanup reminders from framework refactors
- temporary folder reshuffles that business projects should not care about
- framework-internal packaging or repository housekeeping notes
- migration-phase compatibility cleanup steps that do not describe long-term project usage

## Maintenance Workflow

- Maintain public usage contract in `eframe-*.instructions.md` and `eframe-*` skills.
- Maintain framework-only evolution notes in `maintainer-*` instructions.
- Put multi-step business-project workflows in `eframe-*` skills; put framework AI release, sync, migration, and instruction-maintenance workflows in `maintainer-*` skills.
- If a framework change affects both the public project contract and the framework maintenance workflow, update both layers in the same change set.
- If a rule stops being a temporary maintainer concern and becomes a stable project contract, move it out of `maintainer-*` and into the relevant `eframe-*` file.

## Maintaining AI Instructions

- Treat instruction files as executable AI configuration, not passive documentation; optimize them for accurate triggering, clear scope, and stable behavior after sync.
- Keep frontmatter valid YAML. Descriptive prose belongs below the closing `---`; metadata belongs in explicit fields such as `name`, `description`, and `applyTo`.
- Give synced `eframe-*.instructions.md` files precise `applyTo` patterns whenever path-based triggering can reduce ambiguity.
- Write synced `eframe-*` rules as long-lived project contracts. Avoid migration-phase wording such as "continue to", "for now", "cleanup", or "temporary" unless the rule truly belongs in business projects after cold-start.
- Put AI release reminders, manifest bumps, setup-doc checks, sync whitelist rules, and framework-internal cleanup notes in `maintainer-*`, not in synced runtime/editor contracts.
- When refining runtime or editor instructions, preserve the contract boundary: `eframe-runtime.instructions.md` describes runtime usage semantics, `eframe-editor.instructions.md` describes business-project editor tooling semantics, and this file describes framework-repo maintenance.
- If synced instructions mention `project-*` overlays, phrase the rule as precedence and boundary guidance; do not encourage business projects to edit framework-managed `eframe-*` files directly.
- After changing any framework-managed instruction, update `.github/eframe-ai.manifest.json` and run the AI release check before considering the maintenance pass complete.

## Release And Tooling Checks

- Sync scripts in `tools/Initialize-EFrameAI.ps1` and related tooling should continue to sync only `eframe-*` instructions and skills.
- If you change naming boundaries, sync selection patterns, or manifest expectations, update `EFRAME_AI_ARCHITECTURE.md`, `EFRAME_AI_SETUP.md`, and release checks in the same change set.
- Treat `.github/eframe-ai.manifest.json` as the public AI sync marker only for framework-managed synced files, not for maintainer-only notes.
- When changing `EFrame`, `EFrameComponent`, `Procedure`, `QUI`, `UIController`, `ResPath`, Addressables, resource directory conventions, or runtime cold-start behavior, check whether `.github` instructions, skills, cold-start templates, setup docs, and `.github/eframe-ai.manifest.json` need to change in the same set.
- When changing initialization windows, Addressables/ResPath generation, prefab template import, or cold-start code generation inside the framework repo, also update the synced AI contract, related skills, setup docs, and release checklist in the same change set.
