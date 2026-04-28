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

## Audit Flow

1. 找到启动链路入口，确认没有把业务逻辑塞回启动场景。
2. 检查 `Procedure` 是否只做状态编排，进入退出是否对称清理。
3. 检查 UI 是否经过 `QUI` 层级管理，控制器是否承担了正确职责。
4. 检查资源和代码目录是否可映射，命名是否稳定。
5. 检查新规范是否同时反映到 `.github/` 指令层和项目文档，而不是只改其一。

## Output Expectations

- 先给出高风险问题和潜在回归点。
- 明确指出违反的是哪一类边界：启动场景、`Procedure`、UI、目录、命名、资源路径或 AI 配置同步。
- 如果没有问题，也要说明剩余风险，例如缺少验证、仍处于过渡目录、缺少模板沉淀等。

详细核查项见 [audit checklist](./references/audit-checklist.md)。