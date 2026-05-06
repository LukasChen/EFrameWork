# EFrame Audit Checklist

## 启动场景

- 是否保持唯一启动场景
- 是否只有 `Boot`、必要相机和框架组件
- 是否避免业务 UI、业务角色、玩法场景内容常驻

## Procedure

- 是否只做状态跳转与资源编排
- `OnEnter` / `OnLeave` 是否对称
- 是否避免在流程里堆 UI 动画、一次性数据计算和深层节点操作

## UI / QUI

- 是否通过 `QUI` 层级挂载
- 是否存在手工新建顶层 Canvas 或多余 `EventSystem`
- 承载框架 UI overlay 的运行时场景相机是否挂载 `EFrameSceneCamera`
- `UIController` 是否负责绑定、刷新、销毁，而不是承担全部业务逻辑
- UI 静态结构、布局、切图组装、九宫拉伸、按钮层级和可视状态是否已落实到 Unity prefab
- 移动端安全区、刘海屏、圆角屏适配是否使用 prefab 上的 `SafeAreaFitter` / `FullScreenFitter`，而不是业务代码直接读取 `Screen.safeArea` 或手写 offset
- 静态 UI 标准组件是否序列化在 prefab/YAML 上，避免 runtime 为弥补配置缺失而临时 `AddComponent`
- Runtime Controller 是否避免大规模 `new GameObject` 拼装静态 UI，只保留必要的动态列表/Item 实例化
- 动态生成的重复元素是否优先使用 prefab 中的 Template 或独立 Widget prefab
- `UIController` 是否区分实例级 `OnViewCreated()` / `OnViewDestroyed()` 与每次打开关闭的 `OnViewOpened()` / `OnViewClosed()`
- View 是否保持无参构造并通过 `OnBindingSet()` 接入 `QUIBinding`
- Controller 是否通过 `CurrentView` 访问生成 View，而不是直接保存/驱动 `UIViewHandle` 或回退到旧 `View` facade
- 自定义 transition 是否能处理中断，避免已失效 open/close 异步结果覆盖活跃状态

## 目录与命名

- 代码与资源是否分离
- 是否向非标准目录写入新功能
- 新建或移动的文件是否已按 `eframe-directory-structure` 判定 App/Module ownership
- UI prefab 是否按 Panels、Popups、Widgets、Common 归类
- `.unity` 场景文件是否与 `SceneAssets` 依赖资源分离
- 命名是否符合 `ProcedureXxx`、`XxxViewController`、`XxxView.prefab`

## 资源路径

- 是否集中管理路径
- 是否出现重复硬编码字符串
- 是否使用 `ResPath.Generated` 或明确资源 id 入口

## Unity 编译验证

- 是否避免只用 `.sln`、`.slnx` 或 `dotnet build` 证明 Unity 项目可编译
- 是否运行 `tools/Test-EFrameUnityCompile.ps1 -TargetRoot <ProjectRoot>` 或检查 Unity Editor/Bee 编译日志
- 如果缺少 `Library/Bee/artifacts`，是否先打开 Unity 或运行一次 batch import 后再复核

## AI 配置

- 业务项目是否保留 EFrame managed block，不复制整份框架规则到项目自有 instruction
- 是否存在手改同步得到的 `.github/instructions/eframe-*` 或平台原生 `eframe-*` skill 输出
- `.github/eframe/EFRAME_AI_API_INDEX.md` 和 `.github/eframe-ai.manifest.json` 是否存在
- 初始化脚本和接入说明是否可用
