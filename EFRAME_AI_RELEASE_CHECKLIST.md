# EFrame AI 发布检查清单

这份清单只关注框架 AI 规则的发布与同步，不讨论普通运行时代码发布。

## 1. 命名边界

- `eframe-*`：框架托管文件专用前缀，只能由框架仓库维护。
- `project-*`：业务项目本地 overlay 专用前缀，由项目仓库维护。
- 不要在业务项目里创建 `eframe-*` 的 instruction 或 skill。
- 不要在框架仓库里创建 `project-*` 的 instruction 或 skill。

适用范围：

- `.github/instructions/eframe-*.instructions.md`
- `.github/skills/eframe-*`
- `.github/instructions/project-*.instructions.md`
- 业务项目自定义技能目录名也建议使用 `project-*` 前缀

## 2. 框架发布前检查

只要以下任一内容发生变化，就视为 AI 规则更新：

- `.github/copilot-instructions.md`
- `.github/instructions/eframe-*.instructions.md`
- `.github/skills/eframe-*`
- `tools/Initialize-EFrameAI.ps1`
- `tools/New-EFrameProjectAIOverlay.ps1`
- `tools/Install-EFrameAIProjectUpdater.ps1`
- `EFRAME_AI_SETUP.md`

发布前必须确认：

1. `.github/eframe-ai.manifest.json` 的 `version` 已递增。
2. 新增或更新的 instruction / skills 命名符合 `eframe-*` 约定。
3. `Initialize-EFrameAI.ps1 -StatusOnly` 与 `-Force` 都能正常运行。
4. `Install-EFrameAIProjectUpdater.ps1` 能在一个临时项目目录中生成项目侧更新脚本。
5. `New-EFrameProjectAIOverlay.ps1` 能正常生成 `project-local.instructions.md` 模板。
6. [EFRAME_AI_SETUP.md](EFRAME_AI_SETUP.md) 中的命令示例和流程说明仍然准确。

## 3. 业务项目升级流程

业务项目拉取新的框架版本后：

1. 先执行项目侧 `tools/Sync-EFrameAIFromFramework.ps1 -StatusOnly`
2. 如果提示版本落后，再执行 `tools/Sync-EFrameAIFromFramework.ps1 -Force`
3. 如有需要，再人工检查项目自己的 `project-*` overlay 是否仍然适配新的框架规则

## 4. 禁止事项

- 不要把框架规则复制一份粘贴进项目 overlay。
- 不要让项目 overlay 覆盖整份框架规范，只写差异。
- 不要发布没有 bump manifest 版本的 AI 规则更新。
- 不要在同步脚本里覆盖项目自定义的非 `eframe-*` 项。