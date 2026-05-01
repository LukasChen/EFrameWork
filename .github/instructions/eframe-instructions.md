---
name: "EFrame 规则"
description: "编辑 EFrame 项目中的 runtime、editor tooling、Procedure、QUI、UIController、启动流程、玩法功能代码、资源加载或生成工具时使用。"
applyTo: "{**/packages/com.eframework.core/Runtime/**/*.cs,**/packages/com.eframework.core/Editor/**/*.cs,**/Assets/App/Runtime/**/*.cs,**/Assets/Modules/**/*.cs,**/Assets/Scripts/**/*.cs,**/Assets/**/Editor/**/*.cs,**/Assets/Scripts/Editor/**/*.cs}"
---

# EFrame Framework 规则

本文件是使用 EFrameWork 的项目共享同步契约，聚焦稳定 runtime/editor 规则。

## UI 契约
- `Procedure` 只负责状态切换、进入/退出编排和生命周期清理；页面、弹窗、提示层切换优先作为现有流程内的 UI 行为，不滥增新 `Procedure`。
- UI 应通过 `EFrame.Current.UI` / `IUIService`、`UIControllerBase` 与 `UIViewHandle` 管理；`QUI` 是默认 UI 服务实现和层级宿主，不要在场景里手工堆常驻顶层 Canvas、EventSystem 或绕过框架的 UI 生命周期入口。
- Runtime UI 组件、适配器、动画、helper 和生成绑定代码统一落在 `EFrameWork.Runtime.UI` 及其稳定子命名空间，例如 `Components`、`Layout`、`UIHelper`、`Generated`。
- 命名保持框架约定：`ProcedureXxx`、`XxxViewController`、`XxxView.prefab`，组件目录、类名和命名空间大小写一致。

## UI 主链

- 创建、打开、关闭、缓存和释放 View 统一走 `IUIService` 与 `UIViewHandle` 语义；`QUI` 作为默认实现承载具体层级、缓存和绑定实例化，业务代码不要直接驱动 `BindingViewBase` 的生命周期。
- `BindingViewBase` 只保留 binding wrapper、组件缓存初始化和底层生命周期原语；资源实例化、缓存命中、释放决策和 transition 策略属于宿主或 handle。
- `UIControllerBase` 负责 UI 编排，不在构造函数中依赖业务数据或未初始化服务；显示 UI 前由框架上下文完成绑定。
- `UIControllerBase<TView>` 的 `OnViewCreated()` / `OnViewDestroyed()` 是 View 实例级钩子；`OnViewOpened()` / `OnViewClosed()` 是每次打开关闭的钩子。缓存 UI 关闭到 `Closed` 状态时不要把实例级清理写进每次 hide 路径。
- 新生成和新业务 View wrapper 保持无参构造，通过 `SetBinding()` / `OnBindingSet()` 接入生成的 `QUIBinding`，不要把可复用的业务状态塞进 View 缓存。
- Controller 访问运行中 View 使用 `TypedViewHandle.TypedView`，不要使用 `UIControllerBase<TView>.View` facade；模板和业务 Controller 不依赖 `assetPath` View 构造。
- UI transition 由宿主策略管理；自定义 transition 必须能处理中断/kill，避免已失效的 open/close 异步结果覆盖活跃 handle 状态。

## 资源路径与 Addressables

- 运行时资源 id 应来自自动生成的 `ResPath.Generated`；不要在业务代码中手写 Addressables address 字符串或维护自定义路径中心类。`AssetReference` 可作为 Editor 配置字段，但进入运行时前应解析为 assetId。
- Runtime 资源热路径应由所属 `Procedure` 预加载，随后通过 `Context.Assets.Instantiate(...)` 或 `Context.Assets.GetFromPool(...)` 实例化；`LoadAsync(...)` 和 `InstantiateAsync(...)` 用于调用方明确拥有异步 handle 的场景，fallback 同步加载只作为诊断路径。
- `Assets/App/Res`、`Assets/Scenes` 和 `Assets/Modules` 下的框架托管资源遵循 EFrame 目录契约。不要手动编辑它们的 Addressables group、address 或 managed label。
- 新增、移动和删除的框架托管资源应由 EFrame editor automation 同步，并在 player build 前验证。业务代码应使用所选目录树生成的 `ResPath.Generated`，不要把 Addressables window 当作事实来源。

## 启动与运行时访问

- 启动流程状态使用 EFrame 自有的 `EFrameProcedure` 和 `EFrameProcedureComponent`，不要绕过框架状态机手工驱动流程切换。
- 修改启动链路时必须保证 `EFrame.Initialize(...)` 先完成，之后访问 `EFrame.Current.UI`、`EFrame.Current.Audio`、`EFrame.Current.Data` 等框架服务。
- `QUI` 依赖的 Unity SortingLayer 配置要由项目初始化链路补齐；运行时如果缺失层级，应保留明确校验和修复提示，不要静默回落。
- 需要成为框架 UI overlay 场景渲染入口的运行时场景相机必须挂载 `EFrameSceneCamera`，由组件自动维护 Camera stack；不要在业务代码里轮询相机或手动维护 Camera stack。

## 事件与数据

- 业务代码事件订阅优先使用 `EFrame.Current.Events.SubscribeScoped(...)` 并在 `OnLeave`、`OnDestroy` 或控制器释放时 `Dispose`；框架底层可直接使用 `EventBus`，但业务层应避免手写订阅/退订不成对。
- 数据表必须有无参构造、稳定且唯一的 `StorageKey`，注册统一走 `EFrame.Current.Data.RegisterTable<T>()` 并复用已注册表。
- 数据修改通过表暴露的属性或方法完成，并使用 `SetValue(...)` / `Mutate(...)` 这类入口标记 dirty；不要把底层 `Data` 当作外部可变入口。
- 数据迁移、加载来源和保存结果优先使用表内 `CurrentVersion` / `Migrate(...)`、`LastLoadResult`、`LastSaveResult` 等结构化状态；不要让业务层解析日志文本或假设 `Save()` 一定写盘。
- 框架层默认只在 pause/quit 触发保存；项目如果需要更细粒度保存，由业务在明确时机调用 `SaveAll()` 或表级 `Save()`。

## Editor 工具

- Editor 代码只负责生成、导入、菜单命令、校验和 Inspector 扩展；Runtime 程序集不要引用 Editor 程序集，也不要把运行时业务逻辑塞进 `Editor` 程序集。
- 生成器必须稳定、可重复执行，并有明确覆盖策略；默认避免无提示覆盖用户文件，生成物进入 `Generated`、`App/Res/UI`、`Resources/UI` 等约定路径。
- 工具脚本先显式校验前置条件，例如目标目录、已有资源、覆盖意图和必需配置；失败时给出可修复的错误信息。
- 修改 prefab、scene object 或 `.asset` 时使用 Unity Editor 正规路径，例如 `Undo`、`PrefabUtility`、`EditorUtility.SetDirty`、`AssetDatabase.SaveAssets/Refresh`；不要用不透明的文本替换制造 Unity 序列化噪音。
- UI 预制体、场景对象或绑定代码必须与运行时命名对齐：`XxxView.prefab`、`XxxViewController`、`ProcedureXxx`。
- UI 基础配置（例如默认 `IUIService` 实现 `QUI` 依赖的 SortingLayer）优先进入 `ProjectBootstrap` 初始化链路；目录按职责分层到 `Editor/UI`、`Editor/ProjectBootstrap`、`Editor/Tools`。

## 框架契约边界

- 不要直接修改同步得到的 `eframe-*` 文件来表达项目私有差异。
- 如果项目需要偏离本契约，优先在本地 instruction 说明差异边界，并保持 `EFrame.Initialize(...)`、`QUI/UIController`、`ResPath.Generated`、Addressables 和数据表持久化入口可被框架工具识别。
