# EFrame AI 架构

EFrame 作为 Unity 框架和同步的 AI 协作层一起维护。AI 层是框架契约的一部分：它承载编码规则、项目结构规则、skills 和升级流程，让 Copilot 在 EFrame 项目中工作时不需要每次重新发现约定。

## 1. 设计目标

目标是让每个新的 EFrame 项目都从复制 Basic Unity 模板开始，然后让用户通过 Unity Editor 或 package 工具同步受支持的 AI 客户端。

冷启动和升级流程必须保持以下部分一致：

- `packages/com.eframework.core/` 下的 Unity package 代码
- `packages/com.eframework.core/AIWorkspace~/` 下的已同步 AI 工作区源
- 系统要求的框架 AI 入口文件，例如 `AGENTS.md`、`CLAUDE.md` 和 `.github/copilot-instructions.md`
- `tools/` 下的同步和引导脚本
- `packages/com.eframework.core/Documentation~/` 下给人看的文档

当其中一部分改变预期的项目形态时，匹配的 AI 指引必须在同一个变更集中一起审查。

## 2. 层职责

`packages/com.eframework.core/`

- 提供 Runtime 和 Editor 实现。
- 拥有 `EFrame`、`EFrameContext`、`QUI`、资源加载、音频、数据、事件和 Editor 引导工具等框架服务。
- 不应依赖项目私有 AI 规则。

`.github/`

- 只包含必须位于 `.github` 下才能在框架仓库生效的文件，例如 `copilot-instructions.md`。
- 不是已同步 EFrame AI 工作区文件的源目录。

`packages/com.eframework.core/AIWorkspace~/`

- 提供框架托管的 AI 协作源层。
- `managed-blocks/eframe-*.md` 包含注入到项目自有 AI 入口文件中的 EFrame block。
- `instructions/eframe-instructions.md` 包含短小、稳定的框架使用规则，是同步到业务项目的契约的一部分。
- `skills/eframe-*` 包含按需触发的业务项目工作流，会同步到业务项目。
- `eframe-ai.manifest.json` 声明已同步 AI 层版本和框架托管 AI 文件的哈希；业务项目仍会在 `.github/eframe-ai.manifest.json` 接收此文件。
- `support-docs/` 包含面向 AI 的支持文档，必须同步到业务项目，但不应放在人类文档目录下。

`AGENTS.md`

- 提供框架仓库根目录的 Codex 维护者入口。
- 当任务匹配时，引导 Codex 读取同步工作流或维护者工作流文件。
- 不会整份复制到业务项目。业务项目拥有自己的 `AGENTS.md`；EFrame 同步只注入或更新标记出的 EFrame managed block。

`CLAUDE.md`

- 提供框架仓库根目录的 Claude Code 入口。
- 指向同一套共享的 EFrame API 索引、同步工作区源和命名边界，而不是重复完整契约。
- 不会整份复制到业务项目。业务项目拥有自己的 `CLAUDE.md`；EFrame 同步只注入或更新标记出的 EFrame managed block。

`packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md`

- 提供紧凑的、面向 AI 的稳定 Runtime、Editor bootstrap 和 AI tooling API 地图。
- 是面向业务项目 AI 的支持文档，不是 release 或 manifest 维护指南。
- 会作为 `.github/eframe/EFRAME_AI_API_INDEX.md` 同步到业务项目，让选定的 AI 客户端可以从项目工作区读取 API 地图。
- 应优先记录稳定的项目侧入口点，而不是内部实现细节。

`tools/`

- 在业务项目中导入和更新 AI 层。
- 创建冷启动项目结构并复制 Basic 启动模板。
- 安装项目侧同步脚本。
- 在项目自有 AI 入口文件中注入或更新 EFrame managed block，同时不覆盖本地项目规则。
- 在同步或框架升级后检查业务项目 AI 工作区健康状态。
- 允许 Unity Editor 用户同步受支持的 AI 客户端，而不是把 AI 契约同步绑定到冷启动流程。

业务项目 `.github/`

- 接收从框架同步过来的 `eframe-*` 文件。
- 接收 `.github/eframe/EFRAME_AI_API_INDEX.md` 作为框架托管支持文档。
- 拥有根 AI 入口文件和所有本地项目规则文件。
- 不得手动 fork 框架托管的 `eframe-*` 文件。
- 在选定的 AI 入口文件中接收 EFrame managed block；只有 EFrame 标记之间的文本由框架拥有。
- 不接收只供框架维护者使用的 `maintainer-*` skills。
- 不接收 package 人类文档，也不接收 `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md` 等维护者 AI 工作区文件。

## 3. 必须执行的同步规则

以下区域的任何变更都必须包含 AI 层影响检查：

- 启动场景结构、`Boot` 或 `EFrameComponent` 初始化
- `Procedure` 生命周期模板或职责
- `QUI`、`UIController`、UI prefab 或层级管理规则
- `ResPath`、Addressables group 规则或资源目录约定
- 冷启动脚本、Basic/Showcase 模板、可选模块或 Editor 初始化窗口
- 目录结构文档或 release/setup 文档

如果变更会影响 Copilot、Codex 或 Claude Code 应如何生成、重构或审计 EFrame 项目，请在同一个变更集中更新相关的 `AGENTS.md`、`CLAUDE.md`、`.github/copilot-instructions.md` 或 `packages/com.eframework.core/AIWorkspace~` instruction/skill。常驻边界放在 instructions 中；多步骤工作流、审计和详细清单放在 skills 或 skill references 中。

## 4. UI Runtime 契约

UI Runtime 契约以 handle 为先：

- Runtime UI 生命周期通过 `IUIService/QUI` 和 `UIViewHandle<TView>` 流转。
- View wrapper 保持轻量，使用无参构造，并通过 `SetBinding()` / `OnBindingSet()` 接收 prefab 状态。
- Controller 通过 `CurrentView` 访问当前存活的 view；生成代码不使用旧的 `UIControllerBase<TView>.View` 门面。
- Controller 生命周期钩子按作用域拆分：`OnViewCreated()` / `OnViewDestroyed()` 是实例级，`OnViewOpened()` / `OnViewClosed()` 是每次打开/关闭级。
- 宿主拥有的 transition 必须容忍中断，避免旧的 open/close 完成回调覆盖当前活动 handle 状态。

任何改变此 UI 契约的框架变更，都必须在同一个变更集中更新同一契约面：

- `packages/com.eframework.core/Documentation~/user/UI_FRAMEWORK_GUIDE.md`
- `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md`
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-ui-feature`
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-guideline-audit`
- `tools/` 和 `packages/com.eframework.core/Editor/` 下的 bootstrap/editor 模板生成器
- 根 changelog 和 package changelog

## 5. 统一版本契约

EFrame 应作为一个产品面维护，而不是拆成一个 Unity 框架加第二个 AI 产品。AI 协作层、bootstrap 工具和同步脚本都是框架 release 契约的一部分。

- EFrame release version：存储在 `packages/com.eframework.core/package.json`，并记录在根 `CHANGELOG.md` 中。
- AI workspace manifest version：只作为同步标记存储在 `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json`，让业务项目可以检测框架托管 AI 文件漂移。

优先按 EFrame release 思考。发布 framework/package release 时提升 package version。将 manifest 视为同步标记，而不是单独的产品版本。

Manifest 更新规则定义在 `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md`。

Manifest version 是业务项目用来检测 AI 层漂移的宽信号。Manifest file hashes 则提供更窄的完整性检查，用于发现本地编辑、缺失文件或部分同步。

推荐 release 顺序：

1. 更新框架实现或文档。
2. 更新匹配的 instructions 和 skills。
3. 如果这是 framework/package release，提升 `packages/com.eframework.core/package.json`。
4. 更新根 `CHANGELOG.md`；如果 package 代码发生变化，也更新 `packages/com.eframework.core/CHANGELOG.md`。
5. 按 `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md` 更新 `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json`。
6. 运行 `tools/Test-EFrameAIRelease.ps1`。
7. 验证 `Initialize-EFrameAI.ps1 -StatusOnly` 和 `-Force`。
8. 验证受变更影响的冷启动或 Editor bootstrap 路径。
9. 对正式 package release，在正式 tag 前创建 preview entry：优先使用 `preview/<package>-<version>-rc.N` 分支，或记录不可变 commit SHA。
10. 通过 Unity Package Manager Git URL 把该 preview entry 导入真实业务项目，使用 `#preview/...` 或 `#<commit-sha>`，并在创建官方 `vX.Y.Z` tag 前验证项目。

## 6. 命名边界

- `eframe-*` 保留给框架托管的 instructions 和 skills。
- `maintainer-*` 保留给只在框架仓库使用的 skills，不属于同步面。
- 业务项目拥有自己的根 AI 入口文件，并可以按需组织本地 AI 规则。
- 使用 `-Force` 时，框架同步脚本可以覆盖已漂移的 `eframe-*` 文件。
- 框架同步脚本必须保留项目自有 instruction 文本，只更新选定 AI 入口文件中的 EFrame managed block。

这条边界让框架升级保持可重复，同时仍允许每个业务项目添加本地规则。
