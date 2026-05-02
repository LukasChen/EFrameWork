# EFrame AI 接入说明

这套配置把 EFrame 开发规范沉淀为可继承的 AI 工作区层。新项目引入框架并完成 Unity 冷启动后，可在编辑器窗口选择 Codex、GitHub Copilot 或 Claude Code 平台并同步对应 AI 契约。

AI 工作区层是 EFrameWork 的一级框架能力。修改 Runtime、Editor、启动模板、目录结构、资源路径规范、冷启动脚本或同步流程时，同步维护配套 instructions、skills 和说明文档；发布与 manifest 边界见 [EFRAME_AI_RELEASE_CHECKLIST.md](EFRAME_AI_RELEASE_CHECKLIST.md)。

架构边界和同步契约见 [EFRAME_AI_ARCHITECTURE.md](EFRAME_AI_ARCHITECTURE.md)。

## 1. 包含内容

- `packages/com.eframework.core/AIWorkspace~/managed-blocks/eframe-*.md`：注入到业务项目 AI 入口文件的 EFrame managed block 源
- `AGENTS.md`：框架仓库 Codex 维护入口；业务项目拥有自己的 `AGENTS.md`，同步时只更新 EFrame managed block
- `CLAUDE.md`：框架仓库 Claude Code 维护入口；业务项目拥有自己的 `CLAUDE.md`，同步时只更新 EFrame managed block
- `.github/copilot-instructions.md`：框架仓库 Copilot 维护入口；业务项目拥有自己的 `.github/copilot-instructions.md`，同步时只更新 EFrame managed block
- `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json`：AI 配置版本清单，记录框架托管 AI 文件版本与 hash；业务项目接收位置仍是 `.github/eframe-ai.manifest.json`
- `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md`：同步到业务项目的短规则，覆盖 EFrame runtime 和 editor 使用契约
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-feature-bootstrap`：用于新功能骨架搭建和重构收敛
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-guideline-audit`：用于规范审查和回归检查
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-ui-feature`：用于 UI 页面、弹窗、Controller、View prefab 和 binding 工作流
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-data-table`：用于持久化数据表、dirty、迁移、保存/加载结果工作流
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-resource-flow`：用于资源目录、Addressables、ResPath 和异步句柄释放工作流
- `tools/MaintainerAIWorkspace/skills/maintainer-*`：仅框架仓库维护用，不同步到业务项目
- `packages/com.eframework.core/Documentation~/EFRAME_AI_API_INDEX.md`：业务 AI 和开发者快速查询稳定框架 API 的索引，同步到业务项目 `.github/eframe/EFRAME_AI_API_INDEX.md`
- `docs/maintainer/EFRAME_AI_ARCHITECTURE.md`、`docs/maintainer/EFRAME_AI_SETUP.md`、`docs/maintainer/EFRAME_AI_RELEASE_CHECKLIST.md`：框架仓库维护文档，不同步到业务项目
- `tools/Initialize-EFrameAI.ps1`：把上述工作区文件同步到目标项目根目录
- `tools/Install-EFrameAIProjectUpdater.ps1`：在业务项目里生成一键更新脚本
- `tools/Initialize-EFrameColdStart.ps1`：一键完成新项目冷启动
- `tools/Initialize-EFrameBootstrapCode.ps1`：生成最小启动场景/Procedure 占位代码
- `packages/com.eframework.core/Documentation~/RESPATH_CONVENTION.md`：资源地址中心类与自动生成规则说明
- `docs/maintainer/EFRAME_AI_ARCHITECTURE.md`：AI 协作层架构、命名边界和同步契约
- `Packages/com.eframework.core/Editor/EFrameProjectInitializationWindow.cs`：Unity 编辑器初始化窗口

## 2. 新项目初始化方式

如果你要做一个“新项目一键冷启动”，优先使用：

```powershell
.\tools\Initialize-EFrameColdStart.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

它会一次性完成：

- 创建推荐目录骨架
- 目录骨架内容以 `packages/com.eframework.core/Documentation~/UNITY_DIRECTORY_STRUCTURE.md` 为基准
- 生成最小启动代码骨架、`Assets/App/Res/Bootstrap/README.md`、`Assets/Modules/SampleModule/*` 范例模块和 `Assets/Scenes/StartUp_SETUP.md`

冷启动默认不自动同步 AI 契约。脚本完成后会输出 cold-start summary，逐项报告目录骨架、AI workspace、项目 updater 和 bootstrap code 的状态；AI 相关项默认显示 `SKIP`，由 Unity 初始化窗口或 `EFrame Tools/AI` 菜单选择平台后同步。状态含义：

- `OK`：本次已生成或目标项目中已存在。
- `SKIP`：用户通过 `-Skip...` 参数主动跳过。
- `WARN`：预期产物不存在，需要检查前面的脚本输出。

如果你确实希望命令行冷启动同时同步 AI，可以显式传入 `-IncludeAIWorkspace -AIClients all`；否则推荐在 Unity 初始化窗口里选择平台后手动同步。

如果你只想补最小启动代码骨架，可以单独执行：

```powershell
.\tools\Initialize-EFrameBootstrapCode.ps1 -TargetRoot "D:\YourUnityProject" -RootNamespace "YourGame"
```

它会生成：

- `Assets/App/Runtime/Common/ResPath.cs`（仅作为 namespace anchor；资源常量由 `ResPath.Generated` 生成）
- `Assets/App/Runtime/Procedure/ProcedureLauncher.cs`
- `Assets/App/Runtime/Procedure/ProcedureHome.cs`
- `Assets/App/Runtime/UI/Views/HomeView.cs`
- `Assets/App/Runtime/UI/Controllers/HomeViewController.cs`
- `Assets/App/Res/Bootstrap/README.md`
- `Assets/Modules/SampleModule/README.md`
- `Assets/Modules/SampleModule/Runtime/**/*.cs` 示例模块脚本
- `Assets/Scenes/StartUp_SETUP.md`

默认范例流程为：`ProcedureLauncher -> ProcedureHome -> HomeUI`。HomeUI 会显示一个“模块测试”按钮，点击后切换到 `SampleModule` 的示例流程与示例界面。

在 Unity 初始化窗口里执行 `Import Initial Templates`，会一次性从 EFrame package 导入所有内置初始范例模板，包括：

- `Assets/App/Res/UI/Panels/Home/HomeView.prefab`
- `Assets/Modules/SampleModule/Res/UI/Panels/SampleModuleMain/SampleModuleMainView.prefab`

这两个 prefab 都带有 `QUIBinding`，运行时示例代码会按标准资源路径加载它们。模板复制完成后，项目可以在自己的 `Assets/...` 下直接接管和修改这些 prefab。

生成的 View wrapper 采用 handle-first UI 契约：View 保持无参构造，通过 `OnBindingSet()` 缓存 `QUIBinding` 组件引用；Controller 通过 `TypedViewHandle.TypedView` 访问运行中 View，并按 `OnViewCreated()` / `OnViewDestroyed()` 管理实例级绑定，按 `OnViewOpened()` / `OnViewClosed()` 管理每次打开关闭的刷新或暂停逻辑。

初始化窗口会立即重跑目录分组同步，并按 ResPath 目录选择生成 `ResPath.Generated.cs`；复制出的 UI prefab 会进入地址管理，托管目录里的导入、移动和删除由编辑器自动同步。

## 3. Unity 编辑器内初始化方式

当项目通过本地 clone + `file:` path 方式引用 EFrameWork 包时，打开 Unity 后会自动弹出初始化窗口；也可以手动通过菜单打开：

```text
EFrame Tools/项目初始化向导
```

窗口支持：

- `Full Initialize Project`：执行标准初始化链路，包括冷启动、Addressables/ResPath、Audio、DOTween、StartUp 场景与示例模板导入
- `Import Initial Templates`：仅重新导入内置初始范例模板，例如 `HomeView.prefab` 与 `SampleModuleMainView.prefab`
- `AI Platform`：选择要同步的平台，可选 `All`、`Codex`、`Copilot`、`ClaudeCode`
- `Check AI Sync Status`：运行 `Initialize-EFrameAI.ps1 -Clients <platform> -StatusOnly`，检查 manifest 差异以及 manifest-tracked 文件是否漂移
- `Sync AI Workspace`：运行 `Initialize-EFrameAI.ps1 -Clients <platform> -Force`，覆盖所选平台的框架托管 `eframe-*` 文件，并在项目 AI 入口文件中注入或更新 EFrame managed block；同时安装项目侧 updater
- `Run AI Health Check`：运行 `Test-EFrameAIProject.ps1`，检查同步完整性、managed block 和常见运行时代码风险

这些 AI 操作也可以从菜单 `EFrame Tools/AI/Check Sync Status`、`EFrame Tools/AI/Sync Workspace` 和 `EFrame Tools/AI/Run Health Check` 单独执行。

`Full Initialize Project` 会自动补齐 `QUI` 依赖的 Unity SortingLayer（`QuiBackground`、`QuiPanel`、`QuiPopUp`、`QuiTooltip`、`QuiEffect`、`QuiTop`），避免运行时 UI 排序异常。

如果项目不是通过本地框架仓库 path 引入，而是通过包缓存或远端包引入，窗口可以创建启动场景；AI 同步按钮会提示在框架仓库根目录执行对应脚本。

Audio 初始化资产统一放在 `Assets/Resources/Audio`：`EFrameAudioMixerSettings.mixer` 由 Audio Setup/项目初始化向导生成，`AudioEventConfig.asset` 由冷启动脚本生成，运行时通过 `AudioResourcePaths` 集中加载。

资源新增、移动或删除后，`Assets/App/Res`、`Assets/Scenes` 和 `Assets/Modules` 下的托管资源会自动同步 Addressables 分组。菜单 `EFrame Tools/Addressables/Sync Groups And Generate ResPath` 会打开托管资源窗口，用于选择哪些目录树生成 `Assets/App/Runtime/Generated/Res/ResPath.Generated.cs`。窗口也可查看 Addressables 与 `ResPath.Generated` 对应关系、目录规范问题，以及执行手动修复/验证。

## 4. 仅同步 AI 的方式

当新项目已经引入这份框架仓库后，在框架仓库根目录执行：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject"
```

可以通过 `-Clients` 选择要同步的平台：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients codex
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients copilot
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients claude-code
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all
```

如果目标项目已有 `.github` 配置，希望用框架版本覆盖：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

执行后，目标项目会得到所选平台需要的 EFrame AI 接入内容。所有平台都会收到共享的 `.github/instructions/eframe-*`、`.github/skills/eframe-*` 和 `.github/eframe/EFRAME_AI_API_INDEX.md`；Codex 使用项目自己的 `AGENTS.md` 中的 EFrame managed block，GitHub Copilot 使用项目自己的 `.github/copilot-instructions.md` 中的 EFrame managed block，Claude Code 使用项目自己的 `CLAUDE.md` 中的 EFrame managed block。

同步过程不会复制维护者文档 `EFRAME_AI_ARCHITECTURE.md`、`EFRAME_AI_SETUP.md` 和 `EFRAME_AI_RELEASE_CHECKLIST.md` 到目标项目根目录。业务项目接收的是所选平台需要的 EFrame managed block、`.github/instructions/eframe-*`、`.github/skills/eframe-*`、`.github/eframe/EFRAME_AI_API_INDEX.md` 和 `.github/eframe-ai.manifest.json`。

## 5. 框架同步方式

业务项目同步框架 AI 配置时，执行两步：

1. 检查目标项目与框架 manifest 是否一致：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -StatusOnly
```

2. 如果输出提示 manifest 不一致，执行覆盖同步：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

`-StatusOnly` 会比较 package 内 `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` 和目标项目 `.github/eframe-ai.manifest.json` 的版本号。版本不同表示 instruction 或 skills 需要同步。

同时，`-StatusOnly` 会输出同步预览，包括：

- 框架托管的 manifest、API index、instructions 和 skills
- 项目自有 AI 入口文件中会注入或更新的 EFrame managed block
- 目标项目里已不属于框架源的 `eframe-*` 项，这些只会在 `-Force` 同步时移除
- manifest 记录的框架托管文件和 managed block hash 校验结果，用于发现版本号一致但本地 `eframe-*` 或 EFrame managed block 被误改的情况

这让业务项目可以先看清同步影响，决定是否执行 `-Force`。

如果想对业务项目做一次更完整的 AI 健康检查，可以在框架仓库根目录执行：

```powershell
.\tools\Test-EFrameAIProject.ps1 -TargetRoot "D:\YourUnityProject" -FrameworkRoot "."
```

该检查会验证 manifest-tracked 文件和 managed block hash、框架版本差异、`eframe-*` instruction/skill 是否存在，并提示常见的运行时代码风险，例如同步 Addressables 等待或手工创建 runtime Canvas。

## 6. 框架规则和项目规则如何同时生效

真正生效的应该始终是项目根目录 `.github` 下那一套文件，而不是跨仓库引用。推荐分层如下：

- Codex 入口层：项目拥有根目录 `AGENTS.md`，EFrame 只维护其中 marker 包围的 managed block
- Claude Code 入口层：项目拥有根目录 `CLAUDE.md`，EFrame 只维护其中 marker 包围的 managed block
- Copilot 框架层：由框架仓库同步到项目 `.github/instructions/eframe-instructions.md`
- 项目层：项目自己维护根 instruction、`.github/instructions/*.md` 或其他 AI 工具入口
- API 查询层：框架同步 `.github/eframe/EFRAME_AI_API_INDEX.md`，业务 AI 在生成或重构代码前用它确认稳定 API 入口
- 工作流层：框架同步 `.github/skills/eframe-*`，项目可按自己的方式维护本地技能或规则

Copilot 会读取业务项目 `.github` 下的 instructions；Codex 和 Claude Code 分别从项目根目录入口文件中的 EFrame managed block 跳转到同步后的共享规则、API index 和 workflow。

需要注意两点：

1. instruction 没有真正的 `import` / `extends` 机制，所以不要依赖“项目 instruction 自动包含框架 instruction”。
2. 项目规则如果要覆盖框架默认值，应该写得更具体，且只写差异，不要复制整份框架规则，否则会降低同步价值。

如果要在业务项目里安装“一键同步 AI 配置”的脚本，可执行：

```powershell
.\tools\Install-EFrameAIProjectUpdater.ps1 -TargetRoot "D:\YourUnityProject"
```

执行后，业务项目会得到 `tools/Sync-EFrameAIFromFramework.ps1`，以后业务项目只需要在自己仓库内执行：

```powershell
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -StatusOnly
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -Force
```

## 7. 推荐继承策略

- package 中的 `packages/com.eframework.core/AIWorkspace~` 视为同步标准源；`.github` 只保留必须放在系统识别位置的入口文件。
- 新项目只在本项目确有差异时做增量扩展，不要直接改坏通用规则。
- 修改通用规范时，优先维护框架仓库，并用初始化脚本同步到业务项目。
- EFrame 托管并同步到业务项目的 instruction 和 skill 统一使用 `eframe-` 前缀；脚本在 `-Force` 同步时会覆盖 drifted `eframe-*` 项。
- 业务项目自己的 AI 规则归项目自己所有，可以自由组织文件名和目录；同步脚本只更新 EFrame managed block。
- 如果框架仓库需要维护“只给框架维护者 AI 看”的规则或工作流，请使用 `maintainer-*` 命名；这类 instruction 和 skill 不会被 `Initialize-EFrameAI.ps1` 同步到业务项目。
- 最稳定的落地方式是：框架仓库负责提供和安装同步器，业务项目仓库只负责执行 `Sync-EFrameAIFromFramework.ps1`。

## 8. 指令层维护范围

以下内容属于 `packages/com.eframework.core/AIWorkspace~` 同步维护范围：

- 启动场景组织方式
- `Procedure` 生命周期模板
- `QUI` 层级、`UIViewHandle` 状态语义、UIController 生命周期钩子或 View 模板边界
- 目录结构、命名规则、资源路径入口
- 新增统一工作流，例如新玩法模板、配置表接入流程、Addressables 分组策略
- Runtime 或 Editor 重构改变推荐写法、生成模板或项目落盘结构
- 冷启动、AI 同步、项目 updater 或初始化窗口行为调整

普通开发中，上述内容只需要维护对应规则、脚本和文档；发布与 manifest 规则见 [EFRAME_AI_RELEASE_CHECKLIST.md](EFRAME_AI_RELEASE_CHECKLIST.md)。

维护原则：instructions 只放短、稳定、始终需要生效的边界规则；多步骤生成、审查、发布和迁移流程放进 skills 或 skill references。同步到业务项目的流程放 `eframe-*` skill，框架仓库维护流程放 `maintainer-*` skill。

`eframe-feature-bootstrap` 只负责顶层分流和总装配；UI、数据表、资源/Addressables 的细节分别由 `eframe-ui-feature`、`eframe-data-table`、`eframe-resource-flow` 承担。

## 9. 推荐维护流程

1. 先更新规范源文档。
2. 维护 `AGENTS.md`、`.github/copilot-instructions.md`、`packages/com.eframework.core/AIWorkspace~/managed-blocks/` 和对应 `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md` / `SKILL.md`。
3. 如果这是框架/包版本发布，更新 `packages/com.eframework.core/package.json` 的 `version`，并维护根目录 [CHANGELOG.md](../../CHANGELOG.md)。如果 package 代码有变化，也同步维护 `packages/com.eframework.core/CHANGELOG.md`。
4. 按 [EFRAME_AI_RELEASE_CHECKLIST.md](EFRAME_AI_RELEASE_CHECKLIST.md) 处理 manifest 版本与 hash。
5. 按 release checklist 执行发布检查脚本：

```powershell
.\tools\Test-EFrameAIRelease.ps1
```

6. 用实际项目做一次小范围验证，并运行 `Test-EFrameAIProject.ps1`，确认同步后的 AI 文件没有漂移。
7. 确认 AI 能按新规范生成或修改代码。
8. 将这套 `.github` 同步到其他项目。

`Test-EFrameAIRelease.ps1` 会检查 AI 影响文件是否伴随 manifest 变更、manifest 版本是否真实递增、manifest-tracked 文件清单和 hash 是否匹配、instruction/skill frontmatter 是否有效、`eframe-*` skill 是否带有机器可读能力元数据、synced instruction 是否过长、同步脚本是否误纳入 `maintainer-*`，并报告 `eframe-*` / `maintainer-*` instruction 和 skill 数量。发布前如果希望 warning 也阻断流程，可以加 `-FailOnWarning`。

版本号建议：

- `packages/com.eframework.core/package.json`：EFrameWork 对外发布版本，遵循 SemVer，是维护和沟通时的主版本号。
- `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json`：内部同步标记，只用于判断业务项目中的框架托管 AI 文件状态，不作为另一套产品版本理解。
- `CHANGELOG.md`：仓库级发布记录，是阅读版本变化的入口；Unity 代码、AI 规则、工具链和文档都记录在同一个 EFrameWork 发布历史里。

维护时不要把 AI 能力当成额外产品线。更合适的理解是：EFrameWork 的一个版本同时包含运行时代码、编辑器工具、AI 协作规则、冷启动模板和同步工具。manifest 只是让业务项目知道“本地同步到哪一版框架 AI 能力”。

更严格的发布与同步边界见 [EFRAME_AI_RELEASE_CHECKLIST.md](EFRAME_AI_RELEASE_CHECKLIST.md)。

附：基础 Unity 目录结构规范见 [UNITY_DIRECTORY_STRUCTURE.md](../../packages/com.eframework.core/Documentation~/UNITY_DIRECTORY_STRUCTURE.md)。

资源地址中心与自动生成规范见 [RESPATH_CONVENTION.md](../../packages/com.eframework.core/Documentation~/RESPATH_CONVENTION.md)。

补充约定：实际 `.unity` 场景文件优先放在 `Assets/Scenes` 或 `Assets/Modules/<Name>/Scenes`；`Assets/App/Res/SceneAssets` 用于场景依赖资源，不直接放场景文件本体。
