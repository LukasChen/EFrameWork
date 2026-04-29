---
name: eframe-guideline-audit
description: 'Review Unity EFrame changes for compliance. Use when auditing startup scene, Procedure lifecycle, QUI usage, UIController structure, directory layout, naming, resource paths, or AI customization sync.'
argument-hint: 'Describe the change set or module to audit.'
user-invocable: true
---

# EFrame Guideline Audit

## When To Use

- 审查新页面、新玩法、新流程是否符合 EFrame 规范
- 评估一个目录或提交是否继续扩散了过渡结构
- 检查 AI 指令层、技能和规范文档是否同步
- 检查框架重构是否同时维护了可同步 AI 协作层、manifest 和冷启动/升级工具链

## Audit Flow

1. 找到启动链路入口，确认没有把业务逻辑塞回启动场景。
2. 检查 `Procedure` 是否只做状态编排，进入退出是否对称清理。
3. 检查 UI 是否经过 `QUI` 层级管理，控制器是否承担了正确职责。
4. 检查资源和代码目录是否可映射，命名是否稳定。
5. 检查新规范是否同时反映到 `.github/` 指令层和项目文档，而不是只改其一。
6. 检查冷启动和升级同步是否仍然成立：`Initialize-EFrameColdStart.ps1`、`Initialize-EFrameAI.ps1`、项目 updater、manifest 和 release checklist 是否与新规则一致。

## AI Layer Audit

当变更涉及 Runtime、Editor、模板、目录、资源路径或启动流程时，额外确认：

1. `.github/copilot-instructions.md` 是否仍准确描述框架一级约束。
2. `eframe-runtime.instructions.md` / `eframe-editor.instructions.md` 是否覆盖新行为。
3. `eframe-feature-bootstrap` 是否会按新结构生成或重构功能。
4. `eframe-guideline-audit` 是否能审查新规则。
5. `.github/eframe-ai.manifest.json` 是否已随 AI 层或同步工具变化递增。
6. `EFRAME_AI_SETUP.md`、`EFRAME_AI_ARCHITECTURE.md`、`EFRAME_AI_RELEASE_CHECKLIST.md` 是否仍与实际工具链一致。

## Output Expectations

- 先给出高风险问题和潜在回归点。
- 明确指出违反的是哪一类边界：启动场景、`Procedure`、UI、目录、命名、资源路径或 AI 配置同步。
- 如果 AI 层未同步，明确指出缺的是 instruction、skill、manifest、setup 文档、冷启动脚本还是升级同步脚本。
- 如果没有问题，也要说明剩余风险，例如缺少验证、仍处于过渡目录、缺少模板沉淀等。

详细核查项见 [audit checklist](./references/audit-checklist.md)。