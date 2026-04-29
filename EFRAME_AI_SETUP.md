# EFrame AI 接入说明

这套配置的目标是把 EFrame 的开发规范沉淀成一套可继承的 AI 工作区层。新项目在引入框架后，只要做一次初始化，就能直接获得统一的 Copilot instruction 和 skills。

AI 工作区层是 EFrameWork 的一级框架能力。框架的 Runtime、Editor、启动模板、目录结构、资源路径规范、冷启动脚本或升级同步流程发生变化时，必须同步检查并更新配套 instructions、skills、manifest 和说明文档，避免业务项目升级后 AI 仍按旧规范生成代码。

架构边界和同步契约见 [EFRAME_AI_ARCHITECTURE.md](EFRAME_AI_ARCHITECTURE.md)。

## 1. 当前包含的内容

- `.github/copilot-instructions.md`：始终生效的 EFrame 开发总规则
- `.github/eframe-ai.manifest.json`：AI 配置版本清单，用于检测项目是否落后于框架规则
- `.github/instructions/*.instructions.md`：按运行时代码、编辑器代码分层附加规则
- `.github/skills/eframe-feature-bootstrap`：用于新功能骨架搭建和重构收敛
- `.github/skills/eframe-guideline-audit`：用于规范审查和回归检查
- `EFRAME_AI_ARCHITECTURE.md`、`EFRAME_AI_SETUP.md`、`EFRAME_AI_RELEASE_CHECKLIST.md`：会随 AI 层同步到业务项目根目录，方便项目内查看架构、接入和发布边界
- `tools/Initialize-EFrameAI.ps1`：把上述工作区文件同步到目标项目根目录
- `tools/Install-EFrameAIProjectUpdater.ps1`：在业务项目里生成一键更新脚本
- `tools/Initialize-EFrameColdStart.ps1`：一键完成新项目冷启动
- `tools/Initialize-EFrameBootstrapCode.ps1`：生成最小启动场景/Procedure 占位代码
- `RESPATH_CONVENTION.md`：资源地址中心类与自动生成规则说明
- `EFRAME_AI_ARCHITECTURE.md`：AI 协作层架构、命名边界和同步契约
- `Packages/com.eframework.core/Editor/EFrameProjectInitializationWindow.cs`：Unity 编辑器初始化窗口

## 2. 新项目初始化方式

如果你要做一个“新项目一键冷启动”，优先使用：

```powershell
.\tools\Initialize-EFrameColdStart.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

它会一次性完成：

- 创建推荐目录骨架
- 目录骨架内容以 `UNITY_DIRECTORY_STRUCTURE.md` 为基准
- 同步框架 AI 基础层到项目 `.github`
- 同步 `EFRAME_AI_ARCHITECTURE.md`、`EFRAME_AI_SETUP.md`、`EFRAME_AI_RELEASE_CHECKLIST.md` 到项目根目录
- 安装项目侧更新脚本 `tools/Sync-EFrameAIFromFramework.ps1`
- 生成项目 overlay 模板 `.github/instructions/project-local.instructions.md`
- 生成最小启动代码骨架、`Assets/App/Res/Bootstrap/README.md`、`Assets/Modules/SampleModule/*` 范例模块和 `Assets/Scenes/StartUp_SETUP.md`

脚本完成后会输出 cold-start summary，逐项报告目录骨架、AI workspace、AI 文档、项目 updater、项目 overlay 和 bootstrap code 的状态。状态含义：

- `OK`：本次已生成或目标项目中已存在。
- `SKIP`：用户通过 `-Skip...` 参数主动跳过。
- `WARN`：预期产物不存在，需要检查前面的脚本输出。

如果你只想同步 AI，不想创建目录骨架，才单独执行 `Initialize-EFrameAI.ps1`。

如果你只想补最小启动代码骨架，可以单独执行：

```powershell
.\tools\Initialize-EFrameBootstrapCode.ps1 -TargetRoot "D:\YourUnityProject" -RootNamespace "YourGame"
```

它会生成：

- `Assets/App/Runtime/Common/ResPath.cs`
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

如果此时 Addressables 已经初始化，窗口会立即重跑目录分组同步并重新生成 `ResPath.Generated.cs`，这样刚复制出来的 UI prefab 会马上进入地址管理。

## 3. Unity 编辑器内初始化方式

当项目通过本地 clone + `file:` path 方式引用 EFrameWork 包时，打开 Unity 后会自动弹出初始化窗口；也可以手动通过菜单打开：

```text
EFrame Tools/项目初始化向导
```

窗口支持：

- `Full Initialize Project`：执行标准初始化链路，包括冷启动、Addressables/ResPath、Audio、DOTween、StartUp 场景与示例模板导入
- `Import Initial Templates`：仅重新导入内置初始范例模板，例如 `HomeView.prefab` 与 `SampleModuleMainView.prefab`

如果当前项目不是通过本地框架仓库 path 引入，而是通过包缓存或远端包引入，窗口仍然可以创建启动场景，但 AI 同步按钮会降级提示，需要手动在框架仓库根目录执行对应脚本。

## 4. 仅同步 AI 的方式

当新项目已经引入这份框架仓库后，在框架仓库根目录执行：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject"
```

如果目标项目已有旧版 `.github` 配置，希望用框架版本覆盖：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

执行后，目标项目根目录会得到一套标准 `.github` 配置，VS Code / Copilot 就能按 EFrame 规范工作。

同步过程也会复制 `EFRAME_AI_ARCHITECTURE.md`、`EFRAME_AI_SETUP.md` 和 `EFRAME_AI_RELEASE_CHECKLIST.md` 到目标项目根目录，让业务项目内的 instruction 链接和升级说明保持可读。

## 5. 框架更新后的同步方式

当框架仓库有人提交了 AI 规则更新，其他项目在拉取框架最新代码后，应该执行两步：

1. 先检查当前项目是否已经落后于框架版本：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -StatusOnly
```

2. 如果输出提示版本过期，再执行覆盖同步：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

`-StatusOnly` 会比较框架仓库 `.github/eframe-ai.manifest.json` 和目标项目 `.github/eframe-ai.manifest.json` 的版本号。只要版本不同，就说明 instruction 或 skills 需要重新同步。

同时，`-StatusOnly` 会输出同步预览，包括：

- 框架托管的根文件、instructions、skills 和 AI 文档
- 目标项目会保留的 `project-*` instruction / skill overlay
- 目标项目里已经不存在于框架源的陈旧 `eframe-*` 项，这些只会在 `-Force` 同步时移除

这让业务项目可以先看清升级影响，再决定是否执行 `-Force`。

## 6. 框架规则和项目规则如何同时生效

真正生效的应该始终是项目根目录 `.github` 下那一套文件，而不是跨仓库引用。推荐分层如下：

- 框架层：由框架仓库同步到项目 `.github/instructions/eframe-*.instructions.md`
- 项目层：项目自己维护 `.github/instructions/project-*.instructions.md`

这样两层会同时被 Copilot 读取，因为它们都位于当前项目工作区根目录下。

需要注意两点：

1. instruction 没有真正的 `import` / `extends` 机制，所以不要依赖“项目 instruction 自动包含框架 instruction”。
2. 项目规则如果要覆盖框架默认值，应该写得更具体，且只写差异，不要复制整份框架规则，否则后续框架升级时会失去同步价值。

如果要给一个新项目快速生成本地 overlay，可以在项目根或框架仓库根执行：

```powershell
.\tools\New-EFrameProjectAIOverlay.ps1 -TargetRoot "D:\YourUnityProject"
```

如果要在业务项目里安装“一键更新 AI 配置”的脚本，可执行：

```powershell
.\tools\Install-EFrameAIProjectUpdater.ps1 -TargetRoot "D:\YourUnityProject"
```

执行后，业务项目会得到 `tools/Sync-EFrameAIFromFramework.ps1`，以后业务项目只需要在自己仓库内执行：

```powershell
.\tools\Sync-EFrameAIFromFramework.ps1 -StatusOnly
.\tools\Sync-EFrameAIFromFramework.ps1 -Force
```

## 7. 推荐继承策略

- 框架仓库中的 `.github` 视为标准源。
- 新项目只在本项目确有差异时做增量扩展，不要直接改坏通用规则。
- 通用规范变化时，优先先改框架仓库，再用初始化脚本同步到业务项目。
- EFrame 托管的 instruction 和 skill 统一使用 `eframe-` 前缀；业务项目自己的 AI 规则请使用其他名称，脚本在 `-Force` 同步时会覆盖 `eframe-*` 项，但会保留项目自定义项。
- 最稳定的落地方式是：框架仓库负责提供和安装同步器，业务项目仓库只负责执行 `Sync-EFrameAIFromFramework.ps1`。

## 8. 何时更新指令层

当下面任一内容变化时，应该同步更新 `.github`：

- 启动场景组织方式
- `Procedure` 生命周期模板
- `QUI` 层级和 UIController 边界
- 目录结构、命名规则、资源路径入口
- 新增统一工作流，例如新玩法模板、配置表接入流程、Addressables 分组策略
- Runtime 或 Editor 重构改变了推荐写法、生成模板或项目落盘结构
- 冷启动、AI 同步、项目 updater 或初始化窗口的行为发生变化

只要上述内容导致 `.github/copilot-instructions.md`、`.github/instructions/`、`.github/skills/`、AI 同步/冷启动工具或 AI setup/release 文档发生变更，就必须同时递增 `.github/eframe-ai.manifest.json` 的 `version`。

## 9. 推荐维护流程

1. 先更新规范源文档。
2. 再更新 `.github/copilot-instructions.md` 和对应 `.instructions.md` / `SKILL.md`。
3. 如果这是框架/包版本发布，更新 `packages/com.eframework.core/package.json` 的 `version`，并维护根目录 [CHANGELOG.md](CHANGELOG.md)。如果 package 代码有变化，也同步维护 `packages/com.eframework.core/CHANGELOG.md`。
4. 如果 AI 规则、AI 文档、同步脚本或冷启动工具发生变化，递增 `.github/eframe-ai.manifest.json` 的版本号。
5. 执行发布检查脚本：

```powershell
.\tools\Test-EFrameAIRelease.ps1
```

6. 用实际项目做一次小范围验证，确认 AI 能按新规范生成或修改代码。
7. 最后再把这套 `.github` 同步到其他项目。

`Test-EFrameAIRelease.ps1` 会检查 AI 影响文件是否伴随 manifest 变更，并检查框架仓库里是否误放了 `project-*` overlay。发布前如果希望 warning 也阻断流程，可以加 `-FailOnWarning`。

版本号建议：

- `packages/com.eframework.core/package.json`：EFrameWork 对外发布版本，遵循 SemVer，是维护和沟通时的主版本号。
- `.github/eframe-ai.manifest.json`：内部同步标记，只用于判断业务项目中的框架托管 AI 文件是否过期，不作为另一套产品版本理解。
- `CHANGELOG.md`：仓库级发布记录，是阅读版本变化的入口；Unity 代码、AI 规则、工具链和文档都记录在同一个 EFrameWork 发布历史里。

维护时不要把 AI 能力当成额外产品线。更合适的理解是：EFrameWork 的一个版本同时包含运行时代码、编辑器工具、AI 协作规则、冷启动模板和同步工具。manifest 只是让业务项目知道“本地同步到哪一版框架 AI 能力”。

更严格的发布与升级边界见 [EFRAME_AI_RELEASE_CHECKLIST.md](EFRAME_AI_RELEASE_CHECKLIST.md)。

附：基础 Unity 目录结构规范见 [UNITY_DIRECTORY_STRUCTURE.md](UNITY_DIRECTORY_STRUCTURE.md)。

资源地址中心与自动生成规范见 [RESPATH_CONVENTION.md](RESPATH_CONVENTION.md)。

补充约定：实际 `.unity` 场景文件优先放在 `Assets/Scenes` 或 `Assets/Modules/<Name>/Scenes`；`Assets/App/Res/SceneAssets` 用于场景依赖资源，不直接放场景文件本体。
