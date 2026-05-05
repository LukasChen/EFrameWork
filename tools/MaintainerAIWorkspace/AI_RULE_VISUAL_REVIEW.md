# EFrame AI Rule Visual Review

This maintainer-only review guide provides visual entry points for checking the rule architecture before editing synced AI files.

## Architecture Map

```mermaid
flowchart LR
    Registry["Rule Registry<br/>rules/registry.yaml<br/>canonical rule records"]
    Sets["Rule Sets<br/>rules/rule-sets.yaml<br/>domain and workflow groups"]
    Views["Rule Views<br/>rules/rule-views.yaml<br/>purpose-specific selections"]
    Outputs["Platform Outputs<br/>Codex / Copilot / Claude Code<br/>skills / instructions / docs"]
    Checks["Validation<br/>duplicate IDs<br/>missing refs<br/>audience leaks"]

    Registry --> Sets
    Sets --> Views
    Views --> Outputs
    Registry --> Checks
    Sets --> Checks
    Views --> Checks
```

Review question: does each layer have a single job, and does any lower layer redefine a rule that should live in the registry?

## Domain Map

```mermaid
mindmap
  root((EFrame AI Rules))
    G Governance
      G01 AIWorkspace source
      G03 Maintainer-only boundary
      G05 Project-specific rules
      G06 Manifest sync marker
    P Startup
      P01 Procedure state machine
      P04 Initialize first
      P05 Injected Context
      P07 UI overlay camera
    U UI
      U01 EFrame.UI entry
      U02 Internal UI boundary
      U05 CurrentView
      U06 Prefab-first static UI
    R Resources
      R01 ResPath.Generated
      R04 Handle release
      R06 Managed Addressables
      R08 Build preflight
    D Directory
      D01 App vs Module
      D03 UI prefab classification
      D06 Naming exceptions
    T Data
      T01 DataTable shape
      T03 Table-owned mutation
      T05 Structured load/save result
      T07 Scoped events
    E Editor
      E01 Editor assembly boundary
      E04 YAML-first editing
      E05 Unity compile validation
    W Workflow
      W01 Feature bootstrap
      W02 Guideline audit
      W04 Release flow
```

Review question: are the domains right, or should any rule move to another domain before we polish wording?

## Rule Set Map

```mermaid
flowchart TB
    subgraph BusinessSets["Business Rule Sets"]
        Entry["governance.business-entry"]
        Startup["startup.procedure-lifecycle"]
        UI["ui.business-contract"]
        Res["resources.asset-flow"]
        Dir["directory.ownership"]
        Data["data.persistence"]
        Editor["editor.unity-serialization"]
    end

    subgraph WorkflowSets["Workflow Rule Sets"]
        Feature["workflow.feature-bootstrap"]
        Audit["workflow.guideline-audit"]
    end

    subgraph MaintainerSets["Maintainer Rule Sets"]
        Sync["governance.sync-boundary"]
        Release["maintainer.release-governance"]
    end

    subgraph PlatformSets["Platform Output Inputs"]
        Codex["platform.codex"]
        Copilot["platform.copilot"]
        Claude["platform.claude-code"]
    end

    Entry --> Codex
    Entry --> Copilot
    Entry --> Claude
    Startup --> Feature
    UI --> Feature
    Res --> Feature
    Dir --> Feature
    Data --> Feature
    Feature --> Audit
    Sync --> Release
```

Review question: do the sets match how maintainers think about EFrame work?

## View To Output Map

```mermaid
flowchart LR
    subgraph Views["Rule Views"]
        Always["instruction.eframe-always-on"]
        Api["support-doc.api-index"]
        Feature["skill.feature-bootstrap"]
        UI["skill.ui-feature"]
        Res["skill.resource-flow"]
        Data["skill.data-table"]
        Dir["skill.directory-structure"]
        Audit["skill.guideline-audit"]
        Codex["managed-block.codex"]
        Copilot["managed-block.copilot"]
        Claude["managed-block.claude-code"]
        Release["maintainer.release-checklist"]
    end

    subgraph Outputs["Current Outputs"]
        Instr["AIWorkspace~/instructions/eframe-instructions.md"]
        Index["AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md"]
        Skills["AIWorkspace~/skills/eframe-*"]
        Blocks["AIWorkspace~/managed-blocks/eframe-*.md"]
        Maint["tools/MaintainerAIWorkspace/*.md"]
    end

    Always --> Instr
    Api --> Index
    Feature --> Skills
    UI --> Skills
    Res --> Skills
    Data --> Skills
    Dir --> Skills
    Audit --> Skills
    Codex --> Blocks
    Copilot --> Blocks
    Claude --> Blocks
    Release --> Maint
```

Review question: should any view include fewer rules and use handoff instead of summary or full text?

## High-Risk Rule Paths

```mermaid
flowchart TD
    U02["U02 UI internals boundary"]
    U01["U01 Business UI entry"]
    U05["U05 CurrentView access"]
    E04["E04 YAML-first Unity editing"]
    R06["R06 Managed Addressables ownership"]
    G05["G05 Project-specific rules outside synced files"]

    U01 --> U02
    U02 --> U05
    R06 --> R07["R07 Managed resource automation"]
    R07 --> R08["R08 Build preflight"]
    G05 --> D06["D06 Naming and directory exceptions"]
    E04 --> E05["E05 Unity compile validation"]
```

Review these first because they most directly affect AI behavior and drift risk:

| Rule | Review Focus |
| --- | --- |
| `U02` | Business files must not imply ordinary feature code should own `QUI`, `IUIService`, or `UIViewHandle`. |
| `U05` | Confirm `CurrentView` is the only business-facing generated view access pattern. |
| `E04` | Confirm YAML-first editing is correct and Editor API remains fallback only. |
| `R06` | Confirm managed Addressables ownership is strict enough. |
| `G05` | Confirm project-specific rules never belong in synced `eframe-*` files. |

## Suggested Review Flow

```mermaid
flowchart TD
    Start["Start"]
    IA["Review information architecture"]
    Sets["Review rule sets"]
    Views["Review rule views"]
    Registry["Review registry by domain"]
    Risks["Review high-risk rules"]
    Feedback["Write feedback by rule ID"]
    Done["Approve structure before content polish"]

    Start --> IA
    IA --> Sets
    Sets --> Views
    Views --> Registry
    Registry --> Risks
    Risks --> Feedback
    Feedback --> Done
```

Use short feedback by rule ID:

```text
U02: make canonical text stronger.
E04: OK.
R06/R07: split ownership and automation more clearly.
instruction.eframe-always-on: too many fullRules.
platform.codex: should include E04 summary.
```

