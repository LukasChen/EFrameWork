# EFrameWork AI Collaboration Rules

本仓库是 EFrameWork 框架标准源。EFrameWork 的核心定位是 Unity 轻量游戏框架 + 可同步 AI 协作层 + 项目冷启动/升级同步工具链。涉及 Unity 游戏项目、EFrame 初始化、Procedure、QUI、UIController、目录结构和 AI 协作方式时，以框架仓库中的 `.github`、`tools` 和 AI 文档为准。

AI 协作层是框架一级能力，不是附属文档。修改框架代码、编辑器工具、启动模板、目录规范或资源路径规范时，同步检查 `.github/copilot-instructions.md`、`.github/instructions/`、`.github/skills/`、`tools/` 同步/冷启动脚本和相关文档。

始终遵守以下原则：

- 启动场景只负责框架启动和流程切换，不承载具体业务玩法和常驻业务 UI。
- `Procedure` 使用 EFrame 自有的 `EFrameProcedure` 和 `EFrameProcedureComponent`；它负责状态切换、`OnPreloadAsync(IAssetPreloadScope, ProcedureEnterContext)`、进入/退出编排和生命周期清理，不承载具体业务逻辑。
- UI 创建、打开、关闭、缓存和释放应通过 `EFrame.Current.UI` / `IUIService`、`UIControllerBase` 与 `UIViewHandle`；`QUI` 是默认 UI 服务实现和层级宿主，不在场景里手工堆顶层 Canvas 与 `EventSystem`。
- 需要承载框架 UI overlay 的运行时场景相机必须挂载 `EFrameSceneCamera`，由组件自动维护 Camera stack；不要在业务代码里轮询相机或手动维护 Camera stack。
- 运行时资源 id 应来自自动生成的 `ResPath.Generated`；不要在业务代码中手写 Addressables address 字符串或维护自定义路径中心类。
- 对 Addressables 动态加载，运行时优先使用 `ResPath.Generated` 传入 asset id；`AssetReference` 更适合作为 Editor 配置字段，业务运行时代码不要把引用对象继续向下传递。
- `Assets/App/Res`、`Assets/Scenes` 和 `Assets/Modules` 下的框架托管资源由 EFrame Addressables 自动化管理；资源应放入映射目录，避免手动修改 Addressables group/address，并通过报告窗口选择哪些目录树生成 `ResPath.Generated`。
- 新增代码优先落到明确结构，例如 `Assets/App/Runtime`、`Assets/App/Res`、`Assets/Modules/*`，不要扩散非标准目录。
- 基础目录结构以 `UNITY_DIRECTORY_STRUCTURE.md` 为准。
- 命名保持稳定：`ProcedureXxx`、`XxxViewController`、`XxxView.prefab`、`StartUp.unity`、`Boot`、`Main Camera`。
- 编辑 Unity 场景、预制体、`.asset` 文件时避免无关改动，尽量只修改任务直接相关内容。

AI 配置命名边界固定如下：

- `eframe-*` 前缀保留给框架同步项。
- 同步脚本只维护 EFrame managed block 和 `eframe-*` 文件。

涉及 AI 规则发布、命名边界和项目同步流程时，以 [EFRAME_AI_RELEASE_CHECKLIST.md](../EFRAME_AI_RELEASE_CHECKLIST.md) 为准。

AI 层架构和同步契约见 [EFRAME_AI_ARCHITECTURE.md](../EFRAME_AI_ARCHITECTURE.md)。

