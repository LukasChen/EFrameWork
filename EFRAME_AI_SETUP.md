# EFrame AI 接入说明

这套配置的目标是把 EFrame 的开发规范沉淀成一套可继承的 AI 工作区层。新项目在引入框架后，只要做一次初始化，就能直接获得统一的 Copilot instruction 和 skills。

## 1. 当前包含的内容

- `.github/copilot-instructions.md`：始终生效的 EFrame 开发总规则
- `.github/eframe-ai.manifest.json`：AI 配置版本清单，用于检测项目是否落后于框架规则
- `.github/instructions/*.instructions.md`：按运行时代码、编辑器代码分层附加规则
- `.github/skills/eframe-feature-bootstrap`：用于新功能骨架搭建和重构收敛
- `.github/skills/eframe-guideline-audit`：用于规范审查和回归检查
- `tools/Initialize-EFrameAI.ps1`：把上述工作区文件同步到目标项目根目录
- `tools/Install-EFrameAIProjectUpdater.ps1`：在业务项目里生成一键更新脚本
- `tools/Initialize-EFrameColdStart.ps1`：一键完成新项目冷启动
- `tools/Initialize-EFrameBootstrapCode.ps1`：生成最小启动场景/Procedure 占位代码
- `Packages/com.eframework.core/Editor/EFrameProjectInitializationWindow.cs`：Unity 编辑器初始化窗口

## 2. 新项目初始化方式

如果你要做一个“新项目一键冷启动”，优先使用：

```powershell
.\tools\Initialize-EFrameColdStart.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

它会一次性完成：

- 创建推荐目录骨架
- 同步框架 AI 基础层到项目 `.github`
- 安装项目侧更新脚本 `tools/Sync-EFrameAIFromFramework.ps1`
- 生成项目 overlay 模板 `.github/instructions/project-local.instructions.md`
- 生成最小启动代码骨架和 `Assets/Scenes/StartUp_SETUP.md`

如果你只想同步 AI，不想创建目录骨架，才单独执行 `Initialize-EFrameAI.ps1`。

如果你只想补最小启动代码骨架，可以单独执行：

```powershell
.\tools\Initialize-EFrameBootstrapCode.ps1 -TargetRoot "D:\YourUnityProject" -RootNamespace "YourGame"
```

它会生成：

- `Assets/App/Runtime/Common/ResPath.cs`
- `Assets/App/Runtime/Procedure/ProcedureLauncher.cs`
- `Assets/App/Runtime/Procedure/ProcedureHome.cs`
- `Assets/App/Runtime/UI/Controllers/HomeViewController.cs`
- `Assets/Scenes/StartUp_SETUP.md`

## 3. Unity 编辑器内初始化方式

当项目通过本地 clone + `file:` path 方式引用 EFrameWork 包时，打开 Unity 后会自动弹出初始化窗口；也可以手动通过菜单打开：

```text
EFrame Tools/项目初始化向导
```

窗口支持：

- 一键执行冷启动
- 初始化 Addressables 设置
- 创建默认 `App Local Group`
- 将 `Assets/App/Res` 下已有资源注册为 Addressables，并按相对路径去掉扩展名生成地址
- 创建或刷新 `Assets/Scenes/StartUp.unity`
- 自动创建 `Boot + Main Camera`
- 设置 `ProcedureComponent` 与 `EFrameComponent` 引用
- 触发 AI 同步、项目 overlay、项目更新器和 bootstrap code 生成

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

只要上述内容导致 `.github/copilot-instructions.md`、`.github/instructions/` 或 `.github/skills/` 发生变更，就必须同时递增 `.github/eframe-ai.manifest.json` 的 `version`。

## 9. 推荐维护流程

1. 先更新规范源文档。
2. 再更新 `.github/copilot-instructions.md` 和对应 `.instructions.md` / `SKILL.md`。
3. 递增 `.github/eframe-ai.manifest.json` 的版本号。
4. 用实际项目做一次小范围验证，确认 AI 能按新规范生成或修改代码。
5. 最后再把这套 `.github` 同步到其他项目。

更严格的发布与升级边界见 [EFRAME_AI_RELEASE_CHECKLIST.md](EFRAME_AI_RELEASE_CHECKLIST.md)。