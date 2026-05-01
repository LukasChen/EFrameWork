# EFrameWork Claude Code 指南

本仓库是 EFrameWork 框架标准源。EFrameWork 将轻量 Unity 游戏框架、可同步 AI 协作层和项目冷启动/升级工具链组合在一起。

Claude Code 应将本文件视为项目入口，然后阅读共享的 EFrame AI 指引：

- `AGENTS.md`：与 Codex 共享的仓库级 AI 协作契约
- `EFRAME_AI_API_INDEX.md`：稳定 Runtime、Editor bootstrap 和 AI 工具入口
- `.github/instructions/eframe-instructions.md`：同步到业务项目的框架使用规则
- `.github/skills/eframe-*`：面向具体任务的 EFrame 工作流
- `EFRAME_AI_ARCHITECTURE.md` 和 `EFRAME_AI_SETUP.md`：同步边界和项目接入说明

## 核心边界

- Runtime 工作必须遵守 EFrame 服务边界：`EFrame`、`EFrameContext`、`EFrameProcedure`、由 `QUI` 实现的 `EFrame.Current.UI` / `IUIService`、`UIControllerBase`、`ResPath.Generated`、框架管理的 Addressables 和数据表。
- 框架管理的同步文件使用 `eframe-*` 前缀，由本框架仓库维护并同步到业务项目。
- EFrame 同步只更新 marker 包围的 managed block 和 `eframe-*` 文件。
- 仅框架维护者使用的工作流使用 `maintainer-*` 前缀，不能同步到业务项目。

修改框架代码、模板、启动流程、资源约定或 AI 同步工具时，同步维护匹配的 AI 文档、instructions、skills 和脚本。AI 规则发布、manifest 和 release check 边界以 `EFRAME_AI_RELEASE_CHECKLIST.md` 为准。
