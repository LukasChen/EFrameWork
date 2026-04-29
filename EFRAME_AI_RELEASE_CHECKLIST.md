# EFrame AI 发布检查清单

这份清单关注框架 AI 规则的发布与同步。EFrameWork 的 AI 协作层是框架一级能力；普通 Runtime、Editor、模板、目录或资源规范变更只要影响 AI 应该如何生成、重构或审查项目，也必须走这份检查清单。

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
- `tools/Test-EFrameAIRelease.ps1`
- `tools/New-EFrameProjectAIOverlay.ps1`
- `tools/Install-EFrameAIProjectUpdater.ps1`
- `tools/Initialize-EFrameColdStart.ps1`
- `tools/Initialize-EFrameBootstrapCode.ps1`
- `packages/com.eframework.core/Editor/EFrameProjectInitializationWindow.cs`
- `README.md`
- `EFRAME_AI_ARCHITECTURE.md`
- `EFRAME_AI_SETUP.md`
- `UNITY_DIRECTORY_STRUCTURE.md`
- `RESPATH_CONVENTION.md`

发布前必须确认：

1. `.github/eframe-ai.manifest.json` 的 `version` 已递增。
2. 新增或更新的 instruction / skills 命名符合 `eframe-*` 约定。
3. `Test-EFrameAIRelease.ps1` 能通过，且会在 AI 影响文件变更但 manifest 未变更时报错。
4. `Initialize-EFrameAI.ps1 -StatusOnly` 与 `-Force` 都能正常运行。
5. `Initialize-EFrameAI.ps1 -Force` 会同步 `.github`、`EFRAME_AI_ARCHITECTURE.md`、`EFRAME_AI_SETUP.md` 和 `EFRAME_AI_RELEASE_CHECKLIST.md`，且不会覆盖项目自定义的 `project-*` overlay。
6. `Initialize-EFrameAI.ps1 -StatusOnly` 会报告框架托管项、项目 `project-*` overlay、根目录 AI 文档和陈旧 `eframe-*` 项。
7. `Install-EFrameAIProjectUpdater.ps1` 能在一个临时项目目录中生成项目侧更新脚本。
8. `New-EFrameProjectAIOverlay.ps1` 能正常生成 `project-local.instructions.md` 模板。
9. `Initialize-EFrameBootstrapCode.ps1` 能在一个临时项目目录中生成最小启动代码和场景说明。
10. `Initialize-EFrameColdStart.ps1` 能在一个临时空目录中完成冷启动，并输出包含目录、AI workspace、AI 文档、updater、overlay、bootstrap code 的 summary。
11. 冷启动 summary 中没有意外 `WARN` 项；使用 `-Skip...` 参数时对应项显示为 `SKIP`。
12. Unity 编辑器菜单 `EFrame Tools/项目初始化向导` 能打开窗口，并可创建 `StartUp.unity` 与 `Boot` 结构。
13. 初始化窗口能按目录创建默认 Addressables 组，并将 `Assets/App/Res`、`Assets/Scenes`、`Assets/Modules/*` 资源同步进对应组。
14. 初始化窗口或 Addressables 同步流程能生成 `Assets/App/Runtime/Generated/Res/ResPath.Generated.cs`。
15. [EFRAME_AI_SETUP.md](EFRAME_AI_SETUP.md) 中的命令示例和流程说明仍然准确。
16. [EFRAME_AI_ARCHITECTURE.md](EFRAME_AI_ARCHITECTURE.md) 中的层级职责、命名边界和同步契约仍然准确。
17. Runtime/Editor/模板重构如改变推荐写法，对应 instruction 和 skill 已同步更新。

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
- 不要只重构框架代码或模板，却遗漏配套 AI instructions、skills、manifest、setup 文档和同步脚本检查。