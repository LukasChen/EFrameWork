---
name: eframe-guideline-audit
description: 'Review Unity EFrame changes for compliance. Use when auditing startup scene, Procedure lifecycle, QUI usage, UIController structure, directory layout, naming, resource paths, AI customization sync, or when the task mentions 规范审查, EFrame 审查, 接入审查, 合规检查, or 改动体检.'
argument-hint: 'Describe the change set or module to audit.'
user-invocable: true
capabilities:
  - eframe.audit.runtime
  - eframe.audit.ui
  - eframe.audit.resources
  - eframe.audit.data
owns:
  - EFrame guideline compliance review
  - runtime lifecycle risk detection
  - business-project AI contract drift triage
delegatesTo:
  - eframe-directory-structure
  - eframe-ui-feature
  - eframe-resource-flow
  - eframe-data-table
outputs:
  - prioritized guideline findings
  - missing validation risks
  - business-project contract drift summary
forbiddenPatterns:
  - mixing framework release checks into business project audit conclusions
  - treating logs as data/load/save contract
  - approving unmanaged Addressables edits under managed resource directories
---

# EFrame Guideline Audit

## When To Use

- 审查新页面、新玩法、新流程是否符合 EFrame 规范
- 评估一个目录或提交是否扩散了非标准结构
- 检查业务项目是否正确使用已同步的 EFrame framework 契约

## Audit Flow

1. Classify the changed surface: startup, Procedure, UI, directory, resources, data/events, AI config, or Unity compile validation.
2. Review business-facing EFrame boundaries first: initialization, injected `Context`, `EFrame.UI/UIControllerBase`, `ResPath.Generated`, managed Addressables, data table entry, and generated View access.
3. Delegate detailed checks to the owning skill when a surface dominates the review:
   - Directory and ownership: `eframe-directory-structure`.
   - UI lifecycle, binding, layer, overlay camera, or prefab structure: `eframe-ui-feature`.
   - ResPath, Addressables, preload, handle ownership, or build preflight: `eframe-resource-flow`.
   - StorageKey, dirty state, migration, or save/load result semantics: `eframe-data-table`.
4. If the conclusion depends on Unity compilation, use `tools/Test-EFrameUnityCompile.ps1 -TargetRoot <ProjectRoot>` or Unity Editor/Bee logs; do not treat `dotnet build` alone as Unity compile proof.
5. If the finding belongs to framework source maintenance, release, sync tooling, cold-start template authoring, or AI contract maintenance, mark it as outside business-project audit scope.

## Output Expectations

- 先给出高风险问题和潜在回归点。
- 明确指出违反的是哪一类边界：启动场景、`Procedure`、UI、目录、命名、资源路径或 AI 配置同步。
- 如果发现问题属于框架源码、发布或同步工具调整，说明它超出业务项目审查范围。
- 如果没有问题，也要说明剩余风险，例如缺少验证、存在非标准目录、缺少模板沉淀等。
- 需要更细的核查项时，优先引用对应 specialized skill 或 [audit checklist](./references/audit-checklist.md)，不要在这里重复整套实现规则。

详细核查项见 [audit checklist](./references/audit-checklist.md)。
