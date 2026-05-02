# EFrame AI 发布检查清单

这份清单关注框架 AI 规则的发布与同步。EFrameWork 的 AI 协作层是框架一级能力；Runtime、Editor、模板、目录或资源规范调整只要影响 AI 生成、重构或审查项目，也必须走这份检查清单。

## 1. 命名边界

- `eframe-*`：框架托管文件专用前缀，只能由框架仓库维护。
- `maintainer-*`：框架仓库维护者专用 skill 前缀，不同步到业务项目。
- 业务项目根 instruction 和本地 AI 规则归项目仓库维护；EFrame 只更新 marker 包围的 managed block。
- 不要在业务项目里创建 `eframe-*` 的 instruction 或 skill。
- 不要把 release、manifest、同步脚本或框架维护流程塞进同步给业务项目的 `eframe-*` skill；这些属于 `maintainer-*`。

适用范围：

- `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md`
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-*`
- `packages/com.eframework.core/AIWorkspace~/managed-blocks/eframe-*.md`
- `tools/MaintainerAIWorkspace/skills/maintainer-*`
- `AGENTS.md`

## 2. 框架发布前检查

以下内容属于 AI 规则影响范围：

- `.github/copilot-instructions.md`
- `packages/com.eframework.core/AIWorkspace~/managed-blocks/eframe-*.md`
- `CLAUDE.md`
- `AGENTS.md`
- `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md`
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-*`
- `tools/MaintainerAIWorkspace/skills/maintainer-*`
- `tools/Initialize-EFrameAI.ps1`
- `tools/Test-EFrameAIRelease.ps1`
- `tools/New-EFrameProjectAIOverlay.ps1`
- `tools/Install-EFrameAIProjectUpdater.ps1`
- `tools/Initialize-EFrameColdStart.ps1`
- `tools/Initialize-EFrameBootstrapCode.ps1`
- `tools/Test-EFrameAIProject.ps1`
- `packages/com.eframework.core/Editor/EFrameProjectInitializationWindow.cs`
- `README.md`
- `packages/com.eframework.core/Documentation~/EFRAME_AI_API_INDEX.md`
- `docs/maintainer/EFRAME_AI_ARCHITECTURE.md`
- `docs/maintainer/EFRAME_AI_SETUP.md`
- `packages/com.eframework.core/Documentation~/UNITY_DIRECTORY_STRUCTURE.md`
- `packages/com.eframework.core/Documentation~/RESPATH_CONVENTION.md`

发布前必须确认：

1. 如果这是框架/包发布，`packages/com.eframework.core/package.json` 的 `version` 已递增或确认保持原版本。
2. 根目录 [CHANGELOG.md](CHANGELOG.md) 已记录本次仓库级变化；如果 package 代码有变化，`packages/com.eframework.core/CHANGELOG.md` 已同步记录。
3. 如果同步到业务项目的 AI 规则、同步脚本或冷启动工具变化，`packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` 的 `version` 已递增；仅修改框架根目录维护文档不要求递增 manifest。
4. 对外沟通时使用 EFrameWork 主版本号；manifest 只作为业务项目同步检测标记，不作为另一套产品版本发布。
5. 新增或更新的 instruction / skills 命名符合边界：同步业务项目用 `eframe-*`，框架维护专用用 `maintainer-*`，业务项目入口只通过 managed block 接入。
6. `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` 的 `files` 清单覆盖所有可同步到业务项目的 managed block、API index 和 `eframe-*` instructions/skills，并且 sha256 与源文件或 managed block 源内容一致；框架根目录维护文档不进入同步清单。
7. `Test-EFrameAIRelease.ps1` 能通过，且会在 AI 影响文件变更但 manifest 未变更、manifest 版本未递增、manifest 文件清单/hash 不匹配、frontmatter 无效、`eframe-*` skill 缺少机器可读能力元数据、synced instruction 过长或同步脚本误纳入 `maintainer-*` 时给出错误或警告。
8. `Initialize-EFrameAI.ps1 -StatusOnly` 与 `-Force` 都能正常运行。
9. `Initialize-EFrameAI.ps1 -Force` 会同步 `.github/instructions/eframe-*`、`.github/skills/eframe-*` 和 `.github/eframe/EFRAME_AI_API_INDEX.md`，并只在 `AGENTS.md`、`CLAUDE.md`、`.github/copilot-instructions.md` 中注入或更新 EFrame managed block。
10. `Initialize-EFrameAI.ps1 -StatusOnly` 会按所选 `-Clients` 报告框架托管项、会接收 managed block 的项目入口、框架源已移除的 `eframe-*` 项，以及 manifest-tracked 文件/block hash 漂移。
11. `Test-EFrameAIProject.ps1` 能在业务项目或测试同步目录中报告 manifest hash 状态、框架版本差异、缺失 managed block 和常见运行时代码风险。
12. `Install-EFrameAIProjectUpdater.ps1` 能在测试项目目录中生成项目侧更新脚本。
13. `New-EFrameProjectAIOverlay.ps1` 是可选辅助脚本，不属于推荐同步路径。
14. `Initialize-EFrameBootstrapCode.ps1` 能在测试项目目录中生成最小启动代码和场景说明。
15. `Initialize-EFrameColdStart.ps1` 能在空测试目录中完成冷启动，并输出包含目录、AI workspace、updater、bootstrap code 的 summary。
16. 冷启动 summary 中没有意外 `WARN` 项；使用 `-Skip...` 参数时对应项显示为 `SKIP`。
17. Unity 编辑器菜单 `EFrame Tools/项目初始化向导` 能打开窗口，并可创建 `StartUp.unity` 与 `Boot` 结构。
18. 初始化窗口和 `EFrame Tools/AI/*` 菜单能通过平台下拉选择 Codex、Copilot、Claude Code 或全部平台，并运行 AI status、sync 和 health check 入口；框架不是本地 clone 时应给出可理解的降级提示。
19. 初始化窗口会自动补齐 `QUI` 依赖的 Unity SortingLayer；手动菜单修复入口可单独执行。
20. 初始化窗口能按目录创建默认 Addressables 组，并将 `Assets/App/Res`、`Assets/Scenes`、`Assets/Modules/*` 资源同步进对应组。
21. 初始化窗口或 Addressables 同步流程能按 ResPath 目录选择生成 `Assets/App/Runtime/Generated/Res/ResPath.Generated.cs`。
22. 托管资源目录导入、移动、删除后会自动同步 Addressables，且 Player Build 前会执行同步与校验；如果 `ResPath.Generated` 在构建前被刷新，构建应中止并提示等待 Unity 重新编译。
23. [EFRAME_AI_SETUP.md](EFRAME_AI_SETUP.md) 中的命令示例和流程说明准确。
24. [EFRAME_AI_ARCHITECTURE.md](EFRAME_AI_ARCHITECTURE.md) 中的层级职责、命名边界和同步契约准确。
25. Runtime/Editor/模板重构如改变推荐写法，对应 instruction 和 skill 已同步维护。
26. UI 主链路调整时，确认 `packages/com.eframework.core/Documentation~/UI_FRAMEWORK_GUIDE.md`、`eframe-instructions`、`eframe-ui-feature`、`eframe-guideline-audit`、bootstrap 脚本和编辑器初始化模板都保持同一套 `QUI` / `UIViewHandle` / `UIControllerBase` 契约。
27. Always-on 边界保留在 instructions；多步骤生成、审查、发布、迁移流程放在 skills 或 skill references。
28. 新增 `eframe-*` skill 时，确认它是业务项目常用 workflow，而不是 maintainer-only 发布/同步流程。
29. `eframe-feature-bootstrap` 应保持总控职责；UI、数据表、资源细节应委派给对应 specialized skill，避免重复规则。

## Release Commit And Tag

- Release handling must include git commit and tag: after checks pass, commit the release changes and create a version tag matching `packages/com.eframework.core/package.json`, such as `v0.2.2`. Do not stop at prepared-but-uncommitted release files unless the user explicitly asks for that.

## 3. 业务项目同步流程

业务项目拉取新的框架版本后：

1. 先执行项目侧 `tools/Sync-EFrameAIFromFramework.ps1 -StatusOnly`
2. 如果 manifest 不一致，执行 `tools/Sync-EFrameAIFromFramework.ps1 -Force`
3. 如有需要，人工检查项目自己的 instruction 是否适配框架规则

## 4. 禁止事项

- 不要把框架规则复制一份粘贴进项目 instruction；保留 EFrame managed block 即可。
- 不要在同步脚本里覆盖 managed block 外的项目 instruction 内容。
- 不要发布没有递增 manifest 版本的 AI 规则更新。
- 不要在同步脚本里覆盖项目自定义的非 `eframe-*` 项。
- 不要修改框架代码或模板，却遗漏配套 AI instructions、skills、manifest、setup 文档和同步脚本检查。
- 不要把 `maintainer-*` instruction 或 skill 加进 AI 同步白名单。
