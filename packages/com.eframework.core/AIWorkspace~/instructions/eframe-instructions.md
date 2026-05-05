---
name: "EFrame 规则"
description: "编辑 EFrame 项目中的 runtime、editor tooling、Procedure、QUI、UIController、启动流程、玩法功能代码、资源加载或生成工具时使用。"
applyTo: "{**/packages/com.eframework.core/Runtime/**/*.cs,**/packages/com.eframework.core/Editor/**/*.cs,**/Assets/App/Runtime/**/*.cs,**/Assets/Modules/**/*.cs,**/Assets/Scripts/**/*.cs,**/Assets/**/Editor/**/*.cs,**/Assets/Scripts/Editor/**/*.cs}"
---

# EFrame Framework 规则

本文件是使用 EFrame 的项目共享同步契约，聚焦稳定 runtime/editor 规则。

## 协作执行边界

- 当用户是在提出问题、要求解释或分析方案时，先按讨论处理：可以做必要的只读检查来回答，但在编辑文件、执行初始化/安装/刷新动作或其他会改变项目状态的操作前，必须先确认方案并等待明确批准。

## UI 契约
- `Procedure` 只负责状态切换、进入/退出编排和生命周期清理；页面、弹窗、提示层切换优先作为现有流程内的 UI 行为，不滥增新 `Procedure`。
- 业务 UI 推荐入口只暴露一条路径：通过 `EFrame.UI` 和 `UIControllerBase<TGeneratedView>` 管理页面、弹窗和提示；`QUI`、`IUIService`、`UIViewHandle` 是框架内部或进阶扩展概念，不作为普通业务代码的直接入口。
- View 访问类由 UI Binding 工具生成，继承 `BindingViewBase`，默认命名空间为 `EFramework.Generated.UI`；业务使用者不手写 View wrapper。
- Controller 内访问生成 View 时使用 `CurrentView`；不要让业务逻辑直接保存或驱动 `UIViewHandle`。
- UI 静态结构、布局、切图组装、九宫拉伸、按钮层级和可视状态优先落实到 Unity prefab。
- Runtime Controller 不应大规模 `new GameObject` 拼装静态 UI；Controller 只负责事件绑定、数据刷新、状态切换和必要的动态列表/Item 实例化。
- 需要动态生成的重复元素应优先使用 prefab 中的 Template 或独立 Widget prefab。
- Core runtime UI 只承载 host、binding、controller、handle、transition、layout 与 `QScroller` 主链路；`CheckableButton`、`Tabbar`、`UIHelper`、`UIAnimation`、virtual list/grid 等扩展组件需要显式依赖对应可选包。业务生成绑定代码使用工具生成的 `EFramework.Generated.UI`。
- 命名保持框架约定：`ProcedureXxx`、`XxxViewController`、`XxxView.prefab`，组件目录、类名和命名空间大小写一致。

## UI 主链

- 业务代码创建、打开、关闭页面时优先写 Controller，并调用 `Show()` / `ShowAsync()` / `Hide()` / `HideAsync()`；不要直接创建 View、Binding 或 Handle。
- 生成 View 只做 prefab 组件访问；不要把跨打开周期的业务状态塞进 View 实例缓存。
- UI transition、缓存命中、资源实例化和释放决策由框架宿主或 handle 统一管理；不要在业务层自建一套并发状态机覆盖 EFrame 的 UI 生命周期。

## 资源路径与 Addressables

- 运行时资源 id 应来自自动生成的 `ResPath.Generated`；不要在业务代码中手写 Addressables address 字符串或维护自定义路径中心类。`AssetReference` 可作为 Editor 配置字段，但进入运行时前应解析为 assetId。
- Runtime 资源热路径应由所属 `Procedure` 预加载，再通过框架资源服务实例化或复用；不要在业务热路径散落自管 address、同步阻塞加载或脱离生命周期的临时 handle。
- `Assets/App/Res`、`Assets/Scenes` 和 `Assets/Modules` 下的框架托管资源遵循 EFrame 目录契约。不要手动编辑它们的 Addressables group、address 或 managed label。
- 新增、移动和删除的框架托管资源应由 EFrame editor automation 同步，并在 player build 前验证。业务代码应使用所选目录树生成的 `ResPath.Generated`，不要把 Addressables window 当作事实来源。

## 目录结构

- 新建、移动或归类 EFrame 业务代码、Editor 工具、生成物、UI prefab、场景、资源或模块内容前，先使用 `eframe-directory-structure` 判断目录归属；小范围编辑已有文件时沿用现有 ownership。
- 主应用代码落在 `Assets/App/Runtime/...`，主应用 Editor 工具落在 `Assets/App/Editor/...`，生成代码落在 `Assets/App/Runtime/Generated/...` 或模块自己的 `Runtime/Generated/...`。
- 主应用资源落在 `Assets/App/Res/...`，场景文件本体落在 `Assets/Scenes/...`，模块私有代码、资源和场景优先闭包在 `Assets/Modules/<Name>/{Runtime,Res,Editor,Scenes}`。
- UI 资源按类型进入 `Res/UI/Panels`、`Res/UI/Popups`、`Res/UI/Widgets` 或 `Res/UI/Common`；场景依赖资源进入 `Res/SceneAssets`，不要和 `.unity` 场景文件混放。
- 不要在 `Assets` 根下长期堆放零散脚本、资源或临时目录；项目确有特殊目录约束时写入项目自有 instruction，而不是修改同步得到的 `eframe-*` 文件。

## Unity 编译验证

- 验证业务项目 Unity 编译、升级迁移或生成代码可用性时，不要只依赖 `.sln`、`.slnx` 或 `dotnet build`；这些结果可能与 Unity Editor/Bee 编译链路不一致。
- 需要接近 Unity Editor 实际 C# 编译结果时，运行 `tools/Test-EFrameUnityCompile.ps1 -TargetRoot <ProjectRoot>`，它会重放 `Library/Bee/artifacts` 下的 Roslyn response files；项目尚未生成 Bee artifacts 时，先打开 Unity 或运行一次 Unity batch import。
- 如果只需要定位某个程序集，可传 `-AssemblyName Assembly-CSharp` 或具体 asmdef 程序集名；修复结论应以该工具或 Unity Editor Console/Bee 日志为准，而不是以 `dotnet build` 单独通过为准。

## 启动与运行时访问

- 启动流程状态使用 EFrame 自有的 `EFrameProcedure` 和 `EFrameProcedureComponent`，不要绕过框架状态机手工驱动流程切换。
- 修改启动链路时必须保证 `EFrame.Initialize(...)` 先完成；在 framework-aware 类型内部优先使用注入的 `Context`，只有启动、静态入口或非注入场景才使用 `EFrame.UI`、`EFrame.Assets`、`EFrame.Data` 等静态快捷入口。
- 框架 UI 宿主依赖的 Unity SortingLayer 配置要由项目初始化链路补齐；运行时如果缺失层级，应保留明确校验和修复提示，不要静默回落。
- 需要成为框架 UI overlay 场景渲染入口的运行时场景相机必须挂载 `EFrameSceneCamera`，由组件自动维护 Camera stack；不要在业务代码里轮询相机或手动维护 Camera stack。

## 事件与数据

- 业务代码事件订阅优先使用框架提供的 scoped 订阅语义，并在 `OnLeave`、`OnDestroy` 或控制器释放时完成清理；业务层应避免手写不成对的订阅/退订。
- 数据表必须有无参构造、稳定且唯一的持久化 key，并统一通过框架数据服务注册与复用。
- 数据修改通过表暴露的属性或方法完成，并通过表入口标记 dirty；不要把底层 `Data` 当作外部可变入口，也不要让业务层依赖日志文本判断加载或保存结果。
- 框架层默认只在 pause/quit 触发保存；项目如果需要更细粒度保存，由业务在明确时机调用 `SaveAll()` 或表级 `Save()`。

## Editor 工具

- Editor 代码只负责生成、导入、菜单命令、校验和 Inspector 扩展；Runtime 程序集不要引用 Editor 程序集，也不要把运行时业务逻辑塞进 `Editor` 程序集。
- 修改 prefab、scene object 或 `.asset` 时，业务 AI 优先直接编辑 Unity YAML；修改必须局部、透明、可审查，并保持 `guid`、`fileID`、`PrefabInstance`、`m_Modification`、组件顺序、层级和引用关系稳定。
- 禁止重写整个 Unity 资源文件、批量重排序列化块，或做不透明的大范围文本替换制造 Unity 序列化噪音。
- 只有直接 YAML 修改出现异常、无法完成、无法保持引用稳定，或验证后发现 Unity 序列化状态异常时，再调用 Unity Editor API 或其他 fallback 手段修复和保存，例如 `Undo`、`SerializedObject`、`PrefabUtility`、`EditorUtility.SetDirty`、`AssetDatabase.SaveAssets/Refresh`。
- UI 预制体、场景对象或绑定代码必须与运行时命名对齐：`XxxView.prefab`、`XxxViewController`、`ProcedureXxx`。
- UI 基础配置（例如框架 UI 宿主依赖的 SortingLayer）优先进入 `ProjectBootstrap` 初始化链路；目录按职责分层到 `Editor/UI`、`Editor/ProjectBootstrap`、`Editor/Tools`。

## 框架契约边界

- 不要直接修改同步得到的 `eframe-*` 文件来表达项目私有差异。
- 如果项目需要偏离本契约，优先在本地 instruction 说明差异边界，并保持 `EFrame.Initialize(...)`、`EFrame.UI/UIController`、`ResPath.Generated`、Addressables 和数据表持久化入口可被框架工具识别。
