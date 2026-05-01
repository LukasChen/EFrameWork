# EFrameWork Codex 指南

本仓库是 EFrameWork 框架标准源。EFrameWork 是轻量 Unity 游戏框架，同时包含可同步的 AI 协作层和项目冷启动/升级工具链。

Codex 应将本文件视为仓库级入口。GitHub Copilot 读取 `.github/copilot-instructions.md`；Claude Code 读取 `CLAUDE.md`；Codex 读取 `AGENTS.md`。框架 AI 契约必须保持所有客户端入口一致。

## 核心规则

- 遵守 `.github/copilot-instructions.md` 中关于 Unity 启动、`Procedure`、`QUI`、`UIController`、资源路径、Addressables、目录结构和 AI 同步边界的 EFrame 框架规则。
- 运行时代码应落在清晰的 Unity 项目结构中，例如 `Assets/App/Runtime`、`Assets/App/Res` 和 `Assets/Modules/*`；不要为生产代码引入非标准目录。
- 只用 EFrame 自有的 `EFrameProcedure` 和 `EFrameProcedureComponent` 处理状态切换、`OnPreloadAsync` 资源准备和生命周期编排。页面、弹窗和玩法行为应放在对应 UI、controller 或 module 层。
- UI 通过 `EFrame.Current.UI` / `IUIService`、`UIControllerBase` 和 `UIViewHandle` 管理；`QUI` 是默认 UI 服务实现和层级宿主，不要绕过它在场景里手工堆常驻顶层 Canvas 或 EventSystem。
- 运行时资源 id 应来自自动生成的 `ResPath.Generated`。`AssetReference` 可以作为 Editor 配置字段，但运行时调用应先解析为生成的 asset id，不要让引用对象继续流入业务逻辑。
- `Assets/App/Res`、`Assets/Scenes` 和 `Assets/Modules` 下的资源视为 EFrame 管理的 Addressables 条目。资源应放入映射目录，由框架自动化维护 group、address 和 label；需要生成 `ResPath.Generated` 的目录树通过 report window 选择。
- 编辑 Unity scene、prefab 和 `.asset` 文件时保持窄改，避免无关序列化噪音。

## AI 层维护

- AI 协作层是框架一级能力。修改框架代码、模板、启动流程、目录规则、资源规则或同步工具时，同步检查匹配的 `.github` instructions、skills、脚本和文档。
- AI 规则发布、manifest 和 release check 边界以 `EFRAME_AI_RELEASE_CHECKLIST.md` 为准。
- `eframe-*` 文件由框架管理，并同步到业务项目。
- 框架同步只能更新 EFrame managed block 和 `eframe-*` 文件。
- `maintainer-*` 文件只服务框架仓库，不应同步到业务项目。

## Codex Skill 发现

Codex 不会自动加载 GitHub Copilot 工作区 skills。当任务匹配以下工作流时，修改前先阅读对应的 `SKILL.md`：

- 功能骨架和跨层玩法工作：`.github/skills/eframe-feature-bootstrap/SKILL.md`
- EFrame 规范审查和回归检查：`.github/skills/eframe-guideline-audit/SKILL.md`
- UI 页面、弹窗、prefab、controller 和 binding：`.github/skills/eframe-ui-feature/SKILL.md`
- 持久化数据表、dirty state、迁移和保存/加载结果：`.github/skills/eframe-data-table/SKILL.md`
- 资源、Addressables、`ResPath` 和异步 handle：`.github/skills/eframe-resource-flow/SKILL.md`
- 框架 AI/release/sync/template 维护：`.github/skills/maintainer-ai-contract/SKILL.md`

AI 相关 API 查询、架构、setup 和 release 边界见 `EFRAME_AI_API_INDEX.md`、`EFRAME_AI_ARCHITECTURE.md`、`EFRAME_AI_SETUP.md` 和 `EFRAME_AI_RELEASE_CHECKLIST.md`。
