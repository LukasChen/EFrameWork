# EFrame AI Rule Information Architecture

This document defines the target information architecture for EFrame AI rules. It is maintainer-only and must not be synced to business projects.

## Design Principles

1. Each rule has one canonical definition.
2. Synced instructions, skills, support docs, managed blocks, and platform-specific entries reference or render rules; they do not become independent sources of truth.
3. Rule content is structured before wording is optimized.
4. Layering is explicit: governance, business contract, workflow, reference, validation, and platform adapter concerns are not mixed.
5. Platform adaptation is the last step. A platform receives the smallest rule view it can reliably apply.

## Target Layers

```mermaid
graph TD
    A["Rule Registry<br/>canonical structured rules"] --> B["Rule Sets<br/>task/domain groupings"]
    B --> C["Rule Views<br/>instruction, skill, support-doc, audit, release"]
    C --> D["Rendered Files<br/>AIWorkspace~, managed blocks, maintainer docs"]
    D --> E["Platform Adapters<br/>Codex, Copilot, Claude Code, future clients"]
    A --> F["Validation<br/>duplicate, dependency, audience, contradiction checks"]
```

## Layer Definitions

| Layer | Purpose | Owns Canonical Text | Syncs To Business Projects |
| --- | --- | --- | --- |
| Rule Registry | Structured source of every AI rule | Yes | No, until generation is deliberately adopted |
| Rule Sets | Groups rules by domain, task, or audience | No | No |
| Rule Views | Selects rule IDs and rendering style for a surface | No | No |
| Rendered Files | Concrete markdown or JSON files used today | No, after migration | Some files do |
| Platform Adapters | Client-specific entry and activation behavior | No | Some adapters render managed blocks |
| Validation | Checks integrity and drift | No | No |

## Canonical Rule Schema

The eventual registry can be YAML or JSON. The schema should stay boring and tool-friendly.

```yaml
id: U01
title: Business UI entry
audience:
  - business-ai
domain: ui
layer: business-contract
status: stable
canonicalText: >
  Ordinary business UI enters through EFrame.UI and
  UIControllerBase<TGeneratedView>.
rationale: >
  Keeps business code on the framework-supported controller lifecycle
  and away from direct handle ownership.
appliesTo:
  paths:
    - "Assets/App/Runtime/**/*.cs"
    - "Assets/Modules/*/Runtime/**/*.cs"
  tasks:
    - ui
    - feature-bootstrap
forbiddenPatterns:
  - direct business UIViewHandle ownership
  - normal feature code using IUIService as entry point
dependsOn:
  - P04
supersedes: []
related:
  - U02
  - U03
canonicalOwner: rule-registry
rendering:
  instructions: summary
  skills:
    eframe-ui-feature: full
    eframe-guideline-audit: checklist
  supportDocs:
    EFRAME_AI_API_INDEX: api-note
validation:
  contradictionTerms:
    - "business code should own UIViewHandle"
```

## Required Fields

| Field | Meaning |
| --- | --- |
| `id` | Stable rule identifier. Never reuse an old ID for a different meaning. |
| `title` | Short maintainer label. |
| `audience` | `business-ai`, `maintainer-ai`, `human-maintainer`, or `platform-adapter`. |
| `domain` | `governance`, `startup`, `ui`, `resources`, `directory`, `data`, `editor`, `workflow`, `release`, or `platform`. |
| `layer` | `governance`, `business-contract`, `workflow`, `reference`, `validation`, `maintainer-only`, or `platform-adapter`. |
| `status` | `draft`, `candidate`, `stable`, `deprecated`, or `removed`. |
| `canonicalText` | The only full normative wording of the rule. |
| `appliesTo` | Path, task, or capability triggers. |
| `dependsOn` | Other rule IDs needed to interpret this rule correctly. |
| `rendering` | Where this rule may appear and at what detail level. |

## Rendering Levels

| Level | Use |
| --- | --- |
| `full` | The rendered file may include the full canonical text. Use sparingly. |
| `summary` | The rendered file may contain a short restatement with no new semantics. |
| `checklist` | The rendered file may turn the rule into a verification question. |
| `handoff` | The rendered file should point to another rule view or skill. |
| `api-note` | The rendered file may mention the API entry and a short avoid/use note. |
| `none` | The rule must not appear in that surface. |

## Rule View Types

| View | Role | Allowed Detail |
| --- | --- | --- |
| Root managed block | Minimal activation and routing | `summary`, `handoff` |
| Always-on instruction | Short stable boundaries | `summary`, rare `full` |
| Workflow skill | Task execution steps | `full`, `summary`, `handoff` |
| Skill reference | Detailed checklist | `checklist`, `full` when it is the owner |
| API index | Lookup map plus short behavioral notes | `api-note`, `summary` |
| Maintainer checklist | Release and sync gate | `full` for maintainer-only rules |
| Platform adapter | Client-specific activation mechanics | `handoff`, `summary` |

## Single-Source Rule Ownership

During migration, `AI_RULE_INVENTORY.md` is a human-maintained bridge. The final desired state is:

1. A rule's normative wording lives in the registry.
2. Rendered markdown may include only permitted render levels.
3. If two rendered files need the same rule, they reference the same rule ID.
4. If wording changes, the registry changes first, then rendered outputs update.
5. Release checks verify that no synced file introduces a second full normative copy unless the registry allows it.

## Platform Adapter Boundary

Platform adapters should answer only these questions:

- Where does this client read entry instructions?
- Can this client load skills automatically, manually, or not at all?
- What file shape does this client require?
- Which rule views should this client receive?
- What activation text is needed to route the client to the right synced files?

Platform adapters must not redefine EFrame business rules. They only render or point to rule views.

## Migration Sequence

1. Freeze the current inventory and assign stable rule IDs.
2. Convert the inventory into a structured registry draft.
3. Define rule sets for common task domains: UI, resources, data, directory, editor, procedure, audit, release.
4. Define rule views for current rendered files.
5. Normalize duplicated rendered files so they carry summaries, checklists, or handoffs instead of full duplicate wording.
6. Add validation for duplicate full wording, dangling dependencies, wrong audience, and platform adapter leakage.
7. Only then consider generating rendered files.
8. After rendered files are stable, add platform-specific adapters for Codex, Copilot, Claude Code, and future clients.

## First Architecture Milestone

The first milestone is complete when maintainers can answer these questions without opening every synced file:

- Which rule is canonical?
- Which files render it?
- Which files only summarize or check it?
- Which audience owns it?
- Which rules depend on it?
- Which platform adapters activate it?

