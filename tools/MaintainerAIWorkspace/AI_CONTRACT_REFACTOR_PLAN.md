# EFrame AI Contract Refactor Plan

This plan is maintainer-only. It does not sync to business projects and must not be added to the AI workspace manifest.

## Goal

Reduce drift and ambiguity in the EFrame AI collaboration layer without changing business-project behavior first. The immediate target is to make rule ownership explicit, separate business-facing rules from framework-internal implementation notes, and prepare release checks for rule-level validation.

## Current Baseline

- EFrame `0.6.16` accepts the synced Unity serialized asset editing guidance and passes `tools/Test-EFrameAIRelease.ps1`.
- Business-project sync still depends on `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` and file-level hashes.
- The synced surface contains managed blocks, one always-on instruction, one API index, six `eframe-*` skills, and skill references.
- The largest maintainability risks are repeated rule text, UI internal/business perspective mixing, and lack of rule-level ownership metadata.

## Non-Goals

- Do not replace the manifest in this refactor. The manifest remains the file-level sync and drift marker.
- Do not make `tools/MaintainerAIWorkspace` syncable.
- Do not move release or manifest maintenance rules into `eframe-*` skills.
- Do not generate all instructions and skills in the first pass.
- Do not change business-project AI behavior without a manifest bump, changelog entry, and release check.

## Placement Decisions

| Content | Target |
| --- | --- |
| Always-on business EFrame boundaries | `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md` |
| Stable API lookup | `packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md` |
| Multi-step business workflows | `packages/com.eframework.core/AIWorkspace~/skills/eframe-*` |
| Detailed workflow checklists | `packages/com.eframework.core/AIWorkspace~/skills/eframe-*/references/*.md` |
| Rule inventory, migration notes, release hygiene | `tools/MaintainerAIWorkspace/` |
| Human explanation and setup | `packages/com.eframework.core/Documentation~/maintainer/` |

## Phase 1: Maintainer Rule Inventory

Create a maintainer-only rule inventory before changing synced outputs.

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

Acceptance:

- No change to `AIWorkspace~` output.
- `tools/Test-EFrameAIRelease.ps1` still passes or reports no new synced-surface blocker.
- Every high-risk repeated rule has exactly one proposed canonical target.

## Phase 2: Business/Internal Contract Split

Normalize wording where framework internals and business usage are currently mixed.

Target areas:

- UI:
  - Business-facing contract: `EFrame.UI`, `UIControllerBase<TGeneratedView>`, generated `CurrentView`, prefab-first static structure.
  - Internal/advanced contract: `IUIService`, `QUI`, `UIViewHandle<TView>`, lifecycle debugging, framework extensions.
- Unity serialized assets:
  - Low-risk local YAML edits are allowed when transparent and identity-preserving.
  - High-risk structure, references, nested prefabs, variants, binding, or uncertain edits use Unity Editor APIs.
- API index:
  - Keep API lookup factual.
  - Label avoid/recommendation notes as behavioral guidance, not alternate canonical rules.

Acceptance:

- Business rules no longer suggest ordinary feature code should directly own `QUI`, `IUIService`, or `UIViewHandle`.
- `EFRAME_AI_API_INDEX.md` and `eframe-instructions.md` do not contradict each other.
- UI wording remains consistent across `eframe-instructions`, `eframe-ui-feature`, `eframe-guideline-audit`, and UI docs.

## Phase 3: Synced Surface Cleanup

Apply narrow synced changes after Phase 1 identifies canonical owners.

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

## Phase 4: Rule-Level Validation Pilot

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

## Phase 5: Optional Generation

Only consider generation after the inventory and checks have been used in at least one real AI contract release.

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
5. Run project sync status/force checks when sync tooling or target layout changes.

Maintainer-only plan or inventory files do not require a manifest bump unless release tooling starts tracking them explicitly.

## Recommended Next Patch

Start with Phase 1 only:

1. Add `AI_RULE_INVENTORY.md`.
2. Seed it with governance, UI, resource, directory, data, editor, and workflow rule IDs.
3. Mark the current UI and Unity serialized editing rules as high-risk normalization candidates.
4. Run release check to ensure no synced-surface regression.

