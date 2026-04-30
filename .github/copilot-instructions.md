# EFrameWork AI Collaboration Rules

本仓库是 EFrameWork 框架标准源。EFrameWork 的核心定位是 Unity 轻量游戏框架 + 可同步 AI 协作层 + 项目冷启动/升级同步工具链。涉及 Unity 游戏项目、EFrame 初始化、Procedure、QUI、UIController、目录结构和 AI 协作方式时，以框架仓库中的 `.github`、`tools` 和 AI 文档为准。

AI 协作层是框架一级能力，不是附属文档。后续优化或重构框架代码、编辑器工具、启动模板、目录规范、资源路径规范时，必须同步检查 `.github/copilot-instructions.md`、`.github/instructions/`、`.github/skills/`、`.github/eframe-ai.manifest.json`、`tools/` 同步/冷启动脚本和相关文档是否需要更新。

始终遵守以下原则：

- 启动场景只负责框架启动和流程切换，不承载具体业务玩法和常驻业务 UI。
- `Procedure` 只负责状态切换、进入退出编排和生命周期清理，不承担细碎业务逻辑。
- UI 必须通过 `QUI` 与对应 `UIController` 管理，不在场景里手工堆顶层 Canvas 与 `EventSystem`。
- 需要承载框架 UI overlay 的运行时场景相机必须挂载 `EFrameSceneCamera`，由 `QUI` 事件式维护 UI camera stack；不要在业务代码里轮询相机或手动改 UI 相机 stack。
- 资源路径应该集中管理，优先使用 `ResPath` 或等价路径中心类，避免在业务代码中散写路径字符串。
- 对 Addressables 动态加载，优先使用 `ResPath.Generated`、稳定入口 `ResPath` 或 `AssetReference`，不要在业务代码里直接写裸地址字符串。
- Managed resources under `Assets/App/Res`, `Assets/Scenes`, and `Assets/Modules` are owned by EFrame Addressables automation; business projects should place assets in the mapped directory, avoid manual Addressables group/address edits, and use the report window to choose which directories generate `ResPath.Generated`.
- 新增代码优先落到明确结构，例如 `Assets/App/Runtime`、`Assets/App/Res`、`Assets/Modules/*`，不要扩散临时目录。
- 基础目录结构以 `UNITY_DIRECTORY_STRUCTURE.md` 为准；如果业务项目有差异，只在 `project-*` overlay 里补充差异，不复制整份规范。
- 命名保持稳定：`ProcedureXxx`、`XxxViewController`、`XxxView.prefab`、`StartUp.unity`、`Boot`、`Main Camera`。
- 编辑 Unity 场景、预制体、`.asset` 文件时避免无关改动，尽量只修改任务直接相关内容。
- 修改框架行为、模板或项目结构规则时，不要只改 Unity 代码；必须同步维护 AI instructions、skills、manifest、同步脚本和说明文档，确保业务项目冷启动和后续升级仍能得到一致的 AI 指导。

当这套规则被同步到业务项目后，它们属于“框架基础层”。业务项目可以在自己的 `.github/instructions/project-*.instructions.md` 中补充更具体的项目规则；如果项目规则与框架默认值不同，按更具体、更贴近业务上下文的项目规则执行。

AI 配置命名边界固定如下：

- `eframe-*` 前缀保留给框架同步项。
- `project-*` 前缀保留给业务项目本地 overlay。
- 业务项目不要自行创建或修改 `eframe-*` 项，而是通过同步脚本更新。

只要 `.github/copilot-instructions.md`、`.github/instructions/`、`.github/skills/`、AI 同步脚本、冷启动脚本或 AI setup/release 文档发生变更，就同步更新 `.github/eframe-ai.manifest.json` 的版本号，确保业务项目可以检测到 AI 配置是否过期。

涉及 AI 规则发布、命名边界和项目升级流程时，以 [EFRAME_AI_RELEASE_CHECKLIST.md](../EFRAME_AI_RELEASE_CHECKLIST.md) 为准。

AI 层架构和同步契约见 [EFRAME_AI_ARCHITECTURE.md](../EFRAME_AI_ARCHITECTURE.md)。

