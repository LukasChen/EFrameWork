# EFrame AI Rule Inventory

This inventory is maintainer-only. It records canonical ownership for EFrame AI rules so synced instructions, skills, support docs, and managed blocks can be maintained without duplicating contradictory text.

This file is a bridge inventory, not the final machine-readable registry. The target architecture is defined in `AI_RULE_INFORMATION_ARCHITECTURE.md`, and the first structured registry draft lives at `rules/registry.yaml`.

Status legend:

- `canonical`: preferred owner for the rule text.
- `summary`: short restatement is acceptable.
- `handoff`: should point to the canonical owner or skill.
- `candidate`: needs wording cleanup before becoming stable.

## Governance And Sync

| ID | Rule | Audience | Canonical Owner | Current Supporting Locations | Status |
| --- | --- | --- | --- | --- | --- |
| G01 | Business-project AI contract sources must live under `packages/com.eframework.core/AIWorkspace~/` so package-only consumers receive them. | Maintainer | `tools/MaintainerAIWorkspace/skills/maintainer-ai-contract/SKILL.md` | `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`, `Documentation~/maintainer/EFRAME_AI_ARCHITECTURE.md`, `Documentation~/maintainer/EFRAME_AI_SETUP.md` | canonical |
| G02 | `Documentation~/` is human-facing documentation, not a synced AI contract source. | Maintainer | `tools/MaintainerAIWorkspace/skills/maintainer-ai-contract/SKILL.md` | root entries, maintainer docs | canonical |
| G03 | `tools/MaintainerAIWorkspace/` is maintainer-only and must not sync to business projects. | Maintainer | `tools/MaintainerAIWorkspace/skills/maintainer-ai-contract/SKILL.md` | root entries, release checklist, maintainer docs | canonical |
| G04 | Business projects own their root AI entry files; EFrame sync only updates managed blocks and framework-owned `eframe-*` files. | Business + Maintainer | `packages/com.eframework.core/AIWorkspace~/managed-blocks/eframe-*.md` | `eframe-instructions.md`, root entries, maintainer docs | summary |
| G05 | Project-specific rules must stay outside synced `eframe-*` files and outside EFrame managed blocks. | Business + Maintainer | `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md` | managed blocks, root entries, release checklist | canonical |
| G06 | The AI manifest is a sync and drift marker, not a product version. | Maintainer | `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md` | README, maintainer docs, changelog preamble | canonical |
| G07 | Changes that affect business-project generation, refactoring, auditing, sync, or setup must update matching AI contract files in the same change set. | Maintainer | `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md` | README, architecture, setup | canonical |
| G08 | Always-on boundaries belong in instructions; multi-step workflows belong in skills or references. | Maintainer | `tools/MaintainerAIWorkspace/skills/maintainer-ai-contract/SKILL.md` | release checklist, architecture, setup | canonical |
| G09 | Synced business files use the `eframe-*` prefix; maintainer-only workflows use `maintainer-*`. | Maintainer | `tools/MaintainerAIWorkspace/skills/maintainer-ai-contract/SKILL.md` | release checklist, architecture, setup | canonical |
| G10 | Questions and analysis requests are discussion first; do read-only inspection before changing project state. | Business | `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md` | managed blocks | canonical |

## Startup, Procedure, And Context

| ID | Rule | Audience | Canonical Owner | Current Supporting Locations | Status |
| --- | --- | --- | --- | --- | --- |
| P01 | State transitions use `EFrameProcedure` and `EFrameProcedureComponent`; do not bypass the framework state machine. | Business | `eframe-instructions.md` | API index, feature skill | canonical |
| P02 | `Procedure` owns state orchestration, preload, enter/leave cleanup, and lifecycle boundaries; simple UI changes should stay in the active flow. | Business | `eframe-instructions.md` | API index, feature skill, audit checklist | canonical |
| P03 | Procedure-owned resources preload in `OnPreloadAsync(IAssetPreloadScope, ProcedureEnterContext)`; one-shot transition data uses `ProcedureEnterContext`. | Business | `eframe-feature-bootstrap/SKILL.md` | API index, resource skill | canonical |
| P04 | `EFrame.Initialize(...)` must complete before accessing framework service shortcuts. | Business | `eframe-instructions.md` | API index | canonical |
| P05 | Framework-aware runtime types prefer injected `Context`; static `EFrame.*` shortcuts are for startup, static entry points, or non-injected code. | Business | `eframe-instructions.md` | API index, skills, managed blocks | canonical |
| P06 | Startup scenes stay minimal and avoid resident business UI or gameplay objects. | Business | `eframe-guideline-audit/references/audit-checklist.md` | feature skill | canonical |
| P07 | Runtime scene cameras that host framework UI overlay use `EFrameSceneCamera`; business code does not poll cameras or maintain camera stacks manually. | Business | `eframe-instructions.md` | API index, UI skill | canonical |
| P08 | Required `QUI` SortingLayers are supplied by initialization/bootstrap, with explicit validation when missing. | Business | `eframe-instructions.md` | setup docs, audit skill | canonical |

## UI Contract

| ID | Rule | Audience | Canonical Owner | Current Supporting Locations | Status |
| --- | --- | --- | --- | --- | --- |
| U01 | Ordinary business UI enters through `EFrame.UI` and `UIControllerBase<TGeneratedView>`. | Business | `eframe-instructions.md` | API index, UI skill, managed blocks | canonical |
| U02 | `IUIService`, `QUI`, and `UIViewHandle` are framework-internal or advanced-extension concepts, not ordinary business entry points. | Business + Maintainer | `EFRAME_AI_API_INDEX.md` | instruction, UI skill, audit skill | candidate |
| U03 | Business UI open/close work goes through controllers and `Show/Hide` APIs, not direct View, Binding, or Handle ownership. | Business | `eframe-instructions.md` | API index, UI skill | canonical |
| U04 | View access classes are generated from UI Binding under `EFramework.Generated.UI`; business users do not hand-write normal View wrappers. | Business | `eframe-instructions.md` | API index, UI skill | canonical |
| U05 | Controllers access live generated views through `CurrentView`, not saved handles or obsolete View facades. | Business | `eframe-ui-feature/SKILL.md` | instruction, UI checklist, audit checklist | candidate |
| U06 | Static UI structure, layout, slicing, visual states, and button hierarchy are authored in Unity prefabs first. | Business | `eframe-instructions.md` | UI skill, UI checklist, changelog | canonical |
| U07 | Runtime controllers handle event binding, data refresh, state switching, and necessary dynamic item instantiation; they do not build static UI at scale. | Business | `eframe-instructions.md` | UI skill, UI checklist | canonical |
| U08 | Repeated dynamic elements prefer prefab templates or standalone widget prefabs. | Business | `eframe-ui-feature/SKILL.md` | instruction, UI checklist | canonical |
| U09 | UI layer defaults map pages to `QuiPanel`, popups to `QuiPopUp`, tooltips to `QuiTooltip`, and top overlays to `QuiTop`. | Business | `eframe-ui-feature/SKILL.md` | UI checklist | canonical |
| U10 | UI lifecycle separates instance-level create/destroy from per-open open/close refresh or pause behavior. | Business | `eframe-ui-feature/SKILL.md` | UI checklist, instruction | canonical |

Normalization note: `U02` and audit wording should clearly distinguish framework-internal runtime structure from business-facing controller-first usage.

## Resources And Addressables

| ID | Rule | Audience | Canonical Owner | Current Supporting Locations | Status |
| --- | --- | --- | --- | --- | --- |
| R01 | Runtime resource IDs come from generated `ResPath.Generated`; do not scatter Addressables address strings or handwritten path centers. | Business | `eframe-instructions.md` | API index, resource skill, managed blocks | canonical |
| R02 | `AssetReference` is allowed for editor authoring fields, but runtime business logic should receive generated asset IDs. | Business | `eframe-resource-flow/SKILL.md` | instruction, resource checklist | canonical |
| R03 | Procedure hot paths preload resources and instantiate/load through `Context.Assets` or framework asset services. | Business | `eframe-resource-flow/SKILL.md` | instruction, API index | canonical |
| R04 | Asset handles and instances must have an owner-specific release path. | Business | `eframe-resource-flow/references/resource-checklist.md` | API index, resource skill | canonical |
| R05 | Avoid `WaitForCompletion` and synchronous Addressables paths unless the reason is documented. | Business | `eframe-resource-flow/SKILL.md` | instruction, API index | canonical |
| R06 | Addressables entries under managed resource roots are framework-owned; do not hand-edit groups, addresses, or labels. | Business | `eframe-instructions.md` | resource skill, checklist | canonical |
| R07 | Managed resource import, move, delete, repair, and ResPath generation flow through EFrame editor automation. | Business | `eframe-resource-flow/SKILL.md` | API index, checklist | canonical |
| R08 | Player builds must preflight managed Addressables and generated ResPath drift. | Business | `eframe-instructions.md` | resource skill, checklist | canonical |
| R09 | Framework-owned audio resources stay under `Assets/Resources/Audio`. | Business | `EFRAME_AI_API_INDEX.md` | setup docs | canonical |

## Directory Ownership

| ID | Rule | Audience | Canonical Owner | Current Supporting Locations | Status |
| --- | --- | --- | --- | --- | --- |
| D01 | Decide App versus Module ownership before choosing path shape. | Business | `eframe-directory-structure/SKILL.md` | instruction, feature checklist | canonical |
| D02 | Runtime, Editor, and Generated code use separate directories. | Business | `eframe-directory-structure/SKILL.md` | instruction | canonical |
| D03 | UI prefabs are classified into Panels, Popups, Widgets, or Common. | Business | `eframe-directory-structure/SKILL.md` | instruction, UI skill | canonical |
| D04 | `.unity` scenes and scene dependency resources are separated. | Business | `eframe-directory-structure/SKILL.md` | instruction | canonical |
| D05 | Do not accumulate long-lived loose scripts, assets, or temporary folders under `Assets` root. | Business | `eframe-instructions.md` | directory skill | canonical |
| D06 | Naming follows `ProcedureXxx`, `XxxViewController`, and `XxxView.prefab`; project-specific exceptions go in project-owned instructions. | Business | `eframe-instructions.md` | directory skill, feature checklist | canonical |

## Data And Events

| ID | Rule | Audience | Canonical Owner | Current Supporting Locations | Status |
| --- | --- | --- | --- | --- | --- |
| T01 | Persistent state uses serializable data plus `DataTable<TData>`, with public parameterless constructor and stable unique storage key. | Business | `eframe-data-table/SKILL.md` | instruction, data checklist | canonical |
| T02 | Tables are registered and retrieved through the framework data service, preferably via injected `Context.Data`. | Business | `eframe-data-table/SKILL.md` | API index | canonical |
| T03 | Data mutation goes through table-owned APIs such as properties, methods, `SetValue`, or `Mutate`; raw data is not externally mutable. | Business | `eframe-data-table/SKILL.md` | instruction, checklist | canonical |
| T04 | Schema changes declare `CurrentVersion` and implement migration. | Business | `eframe-data-table/SKILL.md` | checklist | canonical |
| T05 | Load/save decisions use structured `LastLoadResult` and `LastSaveResult`, not log parsing. | Business | `eframe-data-table/SKILL.md` | instruction, checklist | canonical |
| T06 | Default save timing is pause/quit; explicit saves happen at business checkpoints. | Business | `eframe-instructions.md` | data skill | canonical |
| T07 | Event subscriptions prefer scoped subscription semantics and lifecycle-bound disposal. | Business | `eframe-instructions.md` | API index, audit checklist | canonical |

## Editor And Validation

| ID | Rule | Audience | Canonical Owner | Current Supporting Locations | Status |
| --- | --- | --- | --- | --- | --- |
| E01 | Editor code owns generation, import, menu commands, validation, and inspector extensions; runtime code must not reference Editor assemblies. | Business | `eframe-instructions.md` | audit checklist | canonical |
| E02 | Generators are stable, repeatable, and explicit about overwrite behavior. | Business | `eframe-guideline-audit/references/audit-checklist.md` | historical instruction wording | handoff |
| E03 | Tool scripts validate prerequisites and produce actionable errors. | Business | `eframe-guideline-audit/references/audit-checklist.md` | historical instruction wording | handoff |
| E04 | Business AI prefers direct, narrow, reviewable Unity YAML edits; Unity Editor APIs and other fallback paths are used only when direct YAML editing cannot complete, cannot preserve references, or validation shows abnormal serialized state. | Business | `eframe-instructions.md` | API index | canonical |
| E05 | Unity compile validation should use Unity Editor/Bee artifacts or `tools/Test-EFrameUnityCompile.ps1`, not only `.sln` or `dotnet build`. | Business | `eframe-instructions.md` | audit skill, setup docs | canonical |

Normalization note: `E04` is settled as a YAML-first business AI rule. Future checks should prevent API index or checklist text from reverting to Editor API first.

## Workflow Routing

| ID | Rule | Audience | Canonical Owner | Current Supporting Locations | Status |
| --- | --- | --- | --- | --- | --- |
| W01 | `eframe-feature-bootstrap` is the top-level feature coordinator and delegates specialized UI, data, resource, directory, and audit work. | Business | `eframe-feature-bootstrap/SKILL.md` | release checklist | canonical |
| W02 | `eframe-guideline-audit` is the business-project compliance review workflow and should not mix in framework release checks. | Business | `eframe-guideline-audit/SKILL.md` | audit checklist | canonical |
| W03 | Business sync flow uses status before force, then project health checks when needed. | Business + Maintainer | `Documentation~/maintainer/EFRAME_AI_SETUP.md` | release checklist | canonical |
| W04 | Framework AI contract releases follow package changelog, manifest version/hash, release check, and explicit maintainer confirmation before commit/tag/push. | Maintainer | `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md` | maintainer skill | canonical |

## First Cleanup Targets

1. Normalize `E04` across `eframe-instructions.md`, `EFRAME_AI_API_INDEX.md`, and audit references.
2. Normalize `U02` wording so business-facing files describe `QUI/IUIService/UIViewHandle` as internal or advanced-only.
3. Trim any duplicate low-level generator/tool prerequisites from always-on instructions once audit references own them.
4. Add release-check warnings for `E04` and `U02` contradiction patterns after wording is stable.
