# EFrame AI Contract Refactor Plan

This plan is maintainer-only. It does not sync to business projects and must not be added to the AI workspace manifest.

## Goal

Reduce drift and ambiguity in the EFrame AI collaboration layer without changing business-project behavior first. The immediate target is to build a stable rule information architecture: every rule is structured, every rule has one canonical definition, rule views select rules by purpose, and platform outputs render those views only after the core rule model is stable.

## Current Baseline

- EFrame `0.6.16` accepts the synced Unity serialized asset editing guidance and passes `tools/Test-EFrameAIRelease.ps1`.
- Business-project sync still depends on `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` and file-level hashes.
- The synced surface contains managed blocks, one always-on instruction, one API index, six `eframe-*` skills, and skill references.
- The largest maintainability risks are repeated rule text, UI internal/business perspective mixing, lack of rule-level ownership metadata, and platform outputs becoming accidental rule sources.

## Non-Goals

- Do not replace the manifest in this refactor. The manifest remains the file-level sync and drift marker.
- Do not make `tools/MaintainerAIWorkspace` syncable.
- Do not move release or manifest maintenance rules into `eframe-*` skills.
- Do not generate all instructions and skills in the first pass.
- Do not change business-project AI behavior without a manifest bump, changelog entry, and release check.
- Do not start platform-specific output tuning until the rule registry, rule views, and platform-output layering are defined.

## Placement Decisions

| Content | Target |
| --- | --- |
| Always-on business EFrame boundaries | `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md` |
| Stable API lookup | `packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md` |
| Multi-step business workflows | `packages/com.eframework.core/AIWorkspace~/skills/eframe-*` |
| Detailed workflow checklists | `packages/com.eframework.core/AIWorkspace~/skills/eframe-*/references/*.md` |
| Rule inventory, migration notes, release hygiene | `tools/MaintainerAIWorkspace/` |
| Human explanation and setup | `packages/com.eframework.core/Documentation~/maintainer/` |
| Rule information architecture | `tools/MaintainerAIWorkspace/AI_RULE_INFORMATION_ARCHITECTURE.md` |
| Visual review guide | `tools/MaintainerAIWorkspace/AI_RULE_VISUAL_REVIEW.md` |
| Output migration plan | `tools/MaintainerAIWorkspace/AI_RULE_OUTPUT_MIGRATION_PLAN.md` |

## Phase 0: Rule Information Architecture

Build the architecture before tuning rule wording.

Deliverables:

- Add `tools/MaintainerAIWorkspace/AI_RULE_INFORMATION_ARCHITECTURE.md`.
- Add `tools/MaintainerAIWorkspace/AI_RULE_VISUAL_REVIEW.md`.
- Define target layers: rule registry, rule sets, rule views, platform outputs, and validation.
- Define the canonical rule schema and rendering levels.
- Define the single-source ownership rule: full normative wording lives in one place, while platform outputs use summaries, checklists, handoffs, or API notes.

Acceptance:

- Maintainers can explain where a rule is defined, which rule view selects it, and which platform output renders it.
- Platform outputs are explicitly out of scope for rule authorship.
- No synced output changes are required for this phase.

## Phase 1: Maintainer Rule Inventory

Create a maintainer-only rule inventory before changing synced outputs. This inventory is a bridge toward the future structured registry, not the final registry format.

Deliverables:

- Add `tools/MaintainerAIWorkspace/AI_RULE_INVENTORY.md`.
- Assign stable IDs to core rules, starting with:
  - `G*`: governance and sync boundaries.
  - `P*`: startup, procedure, context, lifecycle.
  - `U*`: UI business contract.
  - `R*`: resources, Addressables, `ResPath.Generated`.
  - `D*`: directory ownership.
  - `T*`: data tables and events.
  - `E*`: editor tooling, Unity serialization, compile validation.
  - `W*`: workflow and audit routing.
- For each rule, record canonical target, current duplicates, audience, and whether it is synced.
- Mark which rules are ready to become structured registry entries and which need normalization before registry migration.

Acceptance:

- No change to `AIWorkspace~` output.
- `tools/Test-EFrameAIRelease.ps1` still passes or reports no new synced-surface blocker.
- Every high-risk repeated rule has exactly one proposed canonical target.

## Phase 2: Structured Registry Draft

Convert the inventory into a structured registry draft while keeping existing synced files unchanged.

Deliverables:

- Choose YAML or JSON for the draft registry.
- Convert high-confidence inventory rows into structured rule records.
- Add rule sets for UI, resources, data, directory, editor, procedure, audit, release, and platform.
- Add rule views for the current purposes: always-on, workflow, API lookup, audit, release, and setup.
- Add platform outputs for Codex, Copilot, Claude Code, maintainer docs, and package sync files.

Acceptance:

- Each migrated rule has required schema fields.
- Every migrated rule has exactly one canonical text.
- Platform-output mapping uses explicit rendering levels: `full`, `summary`, `checklist`, `handoff`, `api-note`, or `none`.
- No generated output is required.

## Phase 3: Business/Internal Contract Split

Normalize wording where framework internals and business usage are currently mixed.

Target areas:

- UI:
  - Business-facing contract: `EFrame.UI`, `UIControllerBase<TGeneratedView>`, generated `CurrentView`, prefab-first static structure.
  - Internal/advanced contract: `IUIService`, `QUI`, `UIViewHandle<TView>`, lifecycle debugging, framework extensions.
- Unity serialized assets:
  - Business AI prefers direct, narrow, reviewable Unity YAML edits.
  - Unity Editor APIs and other fallback paths are used only when direct YAML editing cannot complete, cannot preserve references, or validation shows abnormal serialized state.
- API index:
  - Keep API lookup factual.
  - Label avoid/recommendation notes as behavioral guidance, not alternate canonical rules.

Acceptance:

- Business rules no longer suggest ordinary feature code should directly own `QUI`, `IUIService`, or `UIViewHandle`.
- `EFRAME_AI_API_INDEX.md` and `eframe-instructions.md` do not contradict each other.
- UI wording remains consistent across `eframe-instructions`, `eframe-ui-feature`, `eframe-guideline-audit`, and UI docs.

## Phase 4: Synced Surface Cleanup

Apply narrow synced changes after the registry draft identifies canonical owners and rule views.

Work items:

- Trim repeated implementation details from `eframe-instructions.md` when a specialized skill or reference owns the workflow.
- Move repeated checklist details into the most specific `references/*.md`.
- Keep managed blocks as short routing summaries only.
- Add links or short handoff text instead of restating full rules across skills.

Acceptance:

- Synced instruction remains short and stable.
- Specialized skills retain enough context to act without loading unrelated docs.
- Manifest version and hashes are updated for every synced change.
- Root and package changelogs describe the business-visible AI contract change.

## Phase 5: Rule-Level Validation Pilot

Extend validation after the inventory stabilizes.

Candidate checks:

- Duplicate rule ID detection in maintainer inventory.
- Dangling canonical target paths.
- Synced files referencing maintainer-only rule IDs.
- `eframe-*` skills containing release, manifest, or maintainer-only workflow terms outside explicit "out of scope" notes.
- UI business files presenting `QUI/IUIService/UIViewHandle` as normal business entry points.
- Instruction/API index contradictions for Unity serialized editing and UI entry points.

Acceptance:

- Initial checks can run as warnings.
- Warnings are actionable and include file paths.
- No generated or heuristic check blocks urgent release work without a clear maintainer override path.

## Phase 6: Optional Generation

Only consider generation after the registry, rule views, platform outputs, and checks have been used in at least one real AI contract release.

Candidate generated outputs:

- Managed block summaries.
- Repeated checklist fragments.
- Directory and resource root tables.
- Rule index pages for maintainers.

Acceptance:

- Generated output diff is deterministic.
- Generated files remain readable and reviewable.
- Manual emergency edits are still possible, but release checks detect drift from source.

## Release Rules For This Refactor

Any change under `packages/com.eframework.core/AIWorkspace~/` must follow the release checklist:

1. Update package and root changelogs.
2. Bump `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` if synced files change.
3. Update manifest hashes.
4. Run `tools/Test-EFrameAIRelease.ps1`.
5. Run `tools/MaintainerAIWorkspace/Test-EFrameAIRuleArchitecture.ps1` when registry, sets, views, or loading policy changes.
6. Run project sync status/force checks when sync tooling or target layout changes.

Maintainer-only plan or inventory files do not require a manifest bump unless release tooling starts tracking them explicitly.

## Recommended Next Patch

Start with Phase 0 and Phase 1 only:

1. Add `AI_RULE_INFORMATION_ARCHITECTURE.md`.
2. Keep `AI_RULE_INVENTORY.md` as the bridge inventory.
3. Add `rules/registry.yaml` as the first structured registry draft.
4. Add `rules/rule-sets.yaml` and `rules/rule-views.yaml` for layered references.
5. Add `rules/loading-policy.yaml` to map task scenarios to minimal rule views and context limits.
6. Add `AI_RULE_OUTPUT_MIGRATION_PLAN.md` before changing synced outputs.
7. Do not change synced rule content while the architecture is still settling.
8. Run release check to ensure no synced-surface regression.
