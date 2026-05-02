# EFrame AI 发布检查清单

这份清单只关心一件事：当框架 AI 契约、同步工具或会影响业务项目 AI 使用方式的功能发生变化时，发布前要检查哪些阻塞项。

命名边界、instruction/skill 放置规则和 maintainer-only 约束以 `AGENTS.md` 与 `tools/MaintainerAIWorkspace/skills/maintainer-ai-contract/SKILL.md` 为准；本清单不重复展开定义，只列发布前必须复核的事项。

## 1. 何时必须走这份清单

满足任一条件就要执行：

- 修改 `packages/com.eframework.core/AIWorkspace~/` 下会同步到业务项目的 instruction、skill、managed block、support-doc。
- 修改 AI 同步、状态检查、冷启动、bootstrap、项目 updater 等脚本。
- 修改 Runtime、Editor、模板、目录、资源、UI 或生成器行为，并影响业务项目应该如何生成、重构或审查 EFrame 代码。
- 修改 AI 架构、setup、release 契约，或调整 `eframe-*` / `maintainer-*` 的职责边界。

## 2. 发布阻塞项

发布前必须确认：

1. 如果这是框架或 package 发布，`packages/com.eframework.core/package.json` 的 `version` 已递增，或已明确确认本次不变更 package 版本。
2. 根目录 [CHANGELOG.md](../../CHANGELOG.md) 已记录本次仓库级变化；如果 package 代码有变化，`packages/com.eframework.core/CHANGELOG.md` 也已同步记录。
3. 如果本次改动会同步到业务项目，包括 AI 规则、同步脚本、冷启动工具或 manifest 声明的支持文档，`packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` 的 `version` 已递增。
4. manifest 的 `files` 清单覆盖全部可同步项，且 `sha256` 与源文件或 managed block 源内容一致；框架根目录 maintainer-only 文档不进入 manifest。
5. 新增或更新的 instruction / skill 符合命名边界：同步给业务项目的使用 `eframe-*`，框架维护专用使用 `maintainer-*`，业务项目入口只通过 managed block 接入。
6. `Test-EFrameAIRelease.ps1` 通过，并能拦截 manifest 未更新、manifest 版本未递增、文件清单或 hash 不匹配、frontmatter 无效、synced instruction 过长、`eframe-*` skill 元数据缺失、或 maintainer-only 内容误进入同步层等问题。
7. `Test-EFrameConsumer.ps1` 通过，确认 package 以真实业务项目形态导入后能执行 `Initialize-EFrameColdStart.ps1`、解析依赖、编译 asmdef，并从外部代码使用运行时入口与 `EFramework.Runtime.*` 类型。
8. `Initialize-EFrameAI.ps1 -StatusOnly` 与 `Initialize-EFrameAI.ps1 -Force` 都能正常运行。
9. `Initialize-EFrameAI.ps1 -Force` 只同步框架托管项：`.github/instructions/eframe-*`、`.github/skills/eframe-*`、`.github/eframe/EFRAME_AI_API_INDEX.md`，以及 `AGENTS.md`、`CLAUDE.md`、`.github/copilot-instructions.md` 中的 EFrame managed block。
10. `Initialize-EFrameAI.ps1 -StatusOnly` 能按 `-Clients` 报告托管项状态、managed block 接入情况、框架源已移除的 `eframe-*` 项，以及 manifest-tracked 文件或 block 的 hash 漂移。
11. 如果这是正式 release，检查通过后先向维护者确认待提交范围、版本号、tag 和目标远端；确认后再提交 release 变更、创建与 `packages/com.eframework.core/package.json` 版本一致的 git tag，例如 `v0.2.2`，并按确认范围推送。

说明：

- 对外沟通时使用 EFrame 主版本号；manifest 版本只用于业务项目同步检测，不作为另一套产品版本号。
- Always-on 边界留在 instructions；多步骤生成、审查、发布、迁移流程放在 skills 或 skill references。

## 3. 按变更类型追加检查

只有本次变更涉及对应领域时，才需要追加以下检查。

### 3.1 业务项目同步与健康检查

- `Test-EFrameAIProject.ps1` 能在业务项目或测试同步目录中报告 manifest hash 状态、框架版本差异、缺失 managed block 和常见运行时代码风险。
- `Install-EFrameAIProjectUpdater.ps1` 能在测试项目目录中生成项目侧更新脚本。
- `New-EFrameProjectAIOverlay.ps1` 仍是可选辅助脚本，不应被当作推荐同步主路径。

### 3.2 冷启动与 bootstrap

- `Initialize-EFrameBootstrapCode.ps1` 能在测试项目目录中生成最小启动代码和场景说明。
- `Initialize-EFrameColdStart.ps1` 能在空测试目录中完成冷启动，并输出包含目录、AI workspace、updater、bootstrap code 的 summary。
- 冷启动 summary 中没有意外 `WARN`；使用 `-Skip...` 参数时，对应项显示为 `SKIP`。

### 3.3 Unity 初始化窗口与 AI 菜单

- Unity 菜单 `EFrame Tools/项目初始化向导` 能打开窗口，并可创建 `StartUp.unity` 与 `Boot` 结构。
- 初始化窗口和 `EFrame Tools/AI/*` 菜单能按平台选择 Codex、Copilot、Claude Code 或全部平台，并能运行 AI status、sync、health check；框架不是本地 clone 时会给出可理解的降级提示。
- 初始化窗口会自动补齐 `QUI` 依赖的 Unity SortingLayer；手动修复入口可单独执行。

### 3.4 Addressables 与 ResPath

- 初始化窗口能按目录创建默认 Addressables 组，并将 `Assets/App/Res`、`Assets/Scenes`、`Assets/Modules/*` 资源同步进对应组。
- 初始化窗口或 Addressables 同步流程能按 ResPath 目录选择生成 `Assets/App/Runtime/Generated/Res/ResPath.Generated.cs`。
- 托管资源目录导入、移动、删除后会自动同步 Addressables；Player Build 前会执行同步与校验；如果 `ResPath.Generated` 在构建前被刷新，构建会中止并提示等待 Unity 重新编译。

### 3.5 文档与契约一致性

- [EFRAME_AI_SETUP.md](../../packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_SETUP.md) 中的命令示例和流程说明准确。
- [EFRAME_AI_ARCHITECTURE.md](../../packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_ARCHITECTURE.md) 中的层级职责、命名边界和同步契约准确。
- Runtime、Editor、模板重构如果改变推荐写法，对应 instruction 和 skill 已同步维护。
- UI 主链路调整时，`UI_FRAMEWORK_GUIDE.md`、`eframe-instructions`、`eframe-ui-feature`、`eframe-guideline-audit`、bootstrap 脚本和编辑器初始化模板保持同一套 `QUI` / `UIViewHandle` / `UIControllerBase` 契约。
- 新增 `eframe-*` skill 时，确认它面向业务项目常用 workflow，而不是 maintainer-only 发布或同步流程；`eframe-feature-bootstrap` 保持总控职责，避免与 specialized skill 重复。

## 4. 业务项目同步流程

业务项目拉取新的框架版本后：

1. 先执行项目侧 `tools/Sync-EFrameAIFromFramework.ps1 -StatusOnly`。
2. 如果 manifest 不一致，再执行 `tools/Sync-EFrameAIFromFramework.ps1 -Force`。
3. 如有需要，人工检查项目自己的 instruction 是否需要适配新的框架规则。

## 5. 禁止事项

- 不要把框架规则复制进项目 instruction；保留 EFrame managed block 即可。
- 不要在同步脚本里覆盖 managed block 外的项目 instruction 内容，也不要覆盖项目自定义的非 `eframe-*` 项。
- 不要发布未递增 manifest 版本的同步层 AI 规则更新。
- 不要修改框架代码、模板或工具后，遗漏配套 AI instructions、skills、manifest、文档和同步检查。
- 不要把 `maintainer-*` instruction 或 skill 加进 AI 同步白名单。
- 不要自行 commit、tag 或 push 新版本；必须先向维护者确认发布范围、版本号和目标远端。
