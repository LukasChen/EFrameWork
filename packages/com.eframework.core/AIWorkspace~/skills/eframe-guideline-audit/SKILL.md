---
name: eframe-guideline-audit
description: 'Review Unity EFrame changes for compliance. Use when auditing startup scene, Procedure lifecycle, QUI usage, UIController structure, directory layout, naming, resource paths, or AI customization sync.'
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

1. 找到启动链路入口，确认没有把业务逻辑塞回启动场景。
2. 检查 `Procedure` 是否只做状态编排，进入退出是否对称清理。
3. 检查 UI 是否经过 `QUI` 层级管理，控制器是否承担了正确职责；如果 UI 细节较多，按 `eframe-ui-feature` 的工作流逐项复核。
4. 检查项目初始化链路是否会自动补齐 `QUI` 依赖的 Unity SortingLayer，运行时缺层级时是否有明确校验提示，并确认承载框架 UI overlay 的相机使用 `EFrameSceneCamera`。
5. 检查资源和代码目录是否可映射；如果目录归属、App/Module 边界、生成物或场景/资源分离是主要问题，按 `eframe-directory-structure` 复核。
6. 检查运行时资源是否通过 `ResPath.Generated` 与框架资源服务进入；如果涉及 preload、Addressables、handle ownership 或 build preflight，按 `eframe-resource-flow` 复核。
7. 检查事件订阅与生命周期清理是否成对，数据表是否统一注册到框架数据服务；如果涉及 `StorageKey`、迁移、dirty 或 save/load 结果语义，按 `eframe-data-table` 复核。
8. 检查业务代码是否绕过 `IUIService/QUI`、`UIViewHandle`、`ResPath.Generated`、框架数据表入口或其他已同步的 EFrame 合约。
9. 如果结论依赖 Unity 编译是否通过，优先使用 `tools/Test-EFrameUnityCompile.ps1 -TargetRoot <ProjectRoot>` 或 Unity Editor/Bee 日志复核；不要把 `dotnet build` 单独通过当作 Unity 编译通过。
10. 如果发现问题主要属于框架源码、发布、同步工具或冷启动模板调整，标记为超出业务项目 audit 范围。

## Output Expectations

- 先给出高风险问题和潜在回归点。
- 明确指出违反的是哪一类边界：启动场景、`Procedure`、UI、目录、命名、资源路径或 AI 配置同步。
- 如果发现问题属于框架源码、发布或同步工具调整，说明它超出业务项目审查范围。
- 如果没有问题，也要说明剩余风险，例如缺少验证、存在非标准目录、缺少模板沉淀等。
- 需要更细的核查项时，优先引用对应 specialized skill 或 [audit checklist](./references/audit-checklist.md)，不要在这里重复整套实现规则。

详细核查项见 [audit checklist](./references/audit-checklist.md)。
