# EFrame Claude Code Entry

本仓库是 EFrame 框架标准源。Claude Code 在本仓库工作时，先按任务类型读取对应的 package 业务契约或维护者文档，不要在本入口文件里重复展开具体框架使用规则。

Codex 读取 `AGENTS.md`；GitHub Copilot 读取 `.github/copilot-instructions.md`；Claude Code 读取 `CLAUDE.md`。三份客户端入口应保持同一套分流和维护边界。

## 入口分流

- AI 生效的稳定 API 查询：`packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md`
- 给人看的业务项目目录、资源、UI 等详细使用文档：`packages/com.eframework.core/Documentation~/user/`
- 会同步到业务项目的短规则源：`packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md`
- 会同步到业务项目的工作流 skills：`packages/com.eframework.core/AIWorkspace~/skills/eframe-*`
- 给人看的框架 AI 架构和 setup 文档：`packages/com.eframework.core/Documentation~/maintainer/`
- 给维护 AI 生效的 release 和 manifest 契约：`tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md`
- 框架维护专用 skill：`tools/MaintainerAIWorkspace/skills/maintainer-ai-contract/SKILL.md`

## 维护边界

- 业务项目需要依赖的 AI 契约文件必须位于 `packages/com.eframework.core/AIWorkspace~/` 内，确保 package-only 接入也能同步生效。
- `packages/com.eframework.core/Documentation~/` 只放给人看的文档，可以同时包含维护者文档和业务使用文档；这些文档不作为 AI 同步契约源。
- 外层 `tools/MaintainerAIWorkspace/` 只服务框架维护者，不同步到业务项目。
- `.github` 只保留系统要求必须放在 `.github` 下的入口文件；不要把同步源重新放回 `.github`。
- EFrame 同步只更新项目 AI 入口中的 EFrame managed block、`eframe-*` 文件和 manifest 声明的框架托管支持文档。
- 项目私有规则必须写在项目自有 instruction 中，不要修改同步得到的 `eframe-*` 文件。

涉及 AI 同步、发布或命名边界变更时，按 `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md` 校验。
