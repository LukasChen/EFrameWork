---
name: "EFrame 规则"
description: "编辑 EFrame 项目中的 runtime、editor tooling、Procedure、QUI、UIController、启动流程、玩法功能代码、资源加载或生成工具时使用。"
applyTo: "{**/packages/com.eframework.core/Runtime/**/*.cs,**/packages/com.eframework.core/Editor/**/*.cs,**/Assets/App/Runtime/**/*.cs,**/Assets/Modules/**/*.cs,**/Assets/Scripts/**/*.cs,**/Assets/**/Editor/**/*.cs,**/Assets/Scripts/Editor/**/*.cs}"
---

# EFrame Framework 规则

本文件是使用 EFrame 的项目共享同步契约，聚焦稳定 runtime/editor 规则。

## UI 契约
- `Procedure` 只负责状态切换、进入/退出编排和生命周期清理；页面、弹窗、提示层切换优先作为现有流程内的 UI 行为，不滥增新 `Procedure`。
- UI 应通过 `EFrame.Current.UI` / `IUIService`、`UIControllerBase` 与 `UIViewHandle` 管理；`QUI` 是默认 UI 服务实现和层级宿主，不要在场景里手工堆常驻顶层 Canvas、EventSystem 或绕过框架的 UI 生命周期入口。
- Runtime UI 组件、适配器、动画、helper 和生成绑定代码统一落在 `EFrame.Runtime.UI` 及其稳定子命名空间，例如 `Components`、`Layout`、`UIHelper`、`Generated`。
- 命名保持框架约定：`ProcedureXxx`、`XxxViewController`、`XxxView.prefab`，组件目录、类名和命名空间大小写一致。

## UI 主链

- 创建、打开、关闭、缓存和释放 View 统一走 `IUIService`、`UIControllerBase` 与 `UIViewHandle`；业务代码不要直接驱动 `BindingViewBase` 生命周期，也不要绕过 `QUI` 自己拼顶层 UI 宿主。
- View wrapper 保持轻量、无参、可复用；不要把跨打开周期的业务状态塞进 View 实例缓存。
- UI transition、缓存命中、资源实例化和释放决策由框架宿主或 handle 统一管理；不要在业务层自建一套并发状态机覆盖 EFrame 的 UI 生命周期。

## 资源路径与 Addressables

- 运行时资源 id 应来自自动生成的 `ResPath.Generated`；不要在业务代码中手写 Addressables address 字符串或维护自定义路径中心类。`AssetReference` 可作为 Editor 配置字段，但进入运行时前应解析为 assetId。
- Runtime 资源热路径应由所属 `Procedure` 预加载，再通过框架资源服务实例化或复用；不要在业务热路径散落自管 address、同步阻塞加载或脱离生命周期的临时 handle。
- `Assets/App/Res`、`Assets/Scenes` 和 `Assets/Modules` 下的框架托管资源遵循 EFrame 目录契约。不要手动编辑它们的 Addressables group、address 或 managed label。
- 新增、移动和删除的框架托管资源应由 EFrame editor automation 同步，并在 player build 前验证。业务代码应使用所选目录树生成的 `ResPath.Generated`，不要把 Addressables window 当作事实来源。

## 启动与运行时访问

- 启动流程状态使用 EFrame 自有的 `EFrameProcedure` 和 `EFrameProcedureComponent`，不要绕过框架状态机手工驱动流程切换。
- 修改启动链路时必须保证 `EFrame.Initialize(...)` 先完成；在 framework-aware 类型内部优先使用注入的 `Context`，只有启动、静态入口或非注入场景才使用 `EFrame.Current.*`。
- `QUI` 依赖的 Unity SortingLayer 配置要由项目初始化链路补齐；运行时如果缺失层级，应保留明确校验和修复提示，不要静默回落。
- 需要成为框架 UI overlay 场景渲染入口的运行时场景相机必须挂载 `EFrameSceneCamera`，由组件自动维护 Camera stack；不要在业务代码里轮询相机或手动维护 Camera stack。

## 事件与数据

- 业务代码事件订阅优先使用框架提供的 scoped 订阅语义，并在 `OnLeave`、`OnDestroy` 或控制器释放时完成清理；业务层应避免手写不成对的订阅/退订。
- 数据表必须有无参构造、稳定且唯一的持久化 key，并统一通过框架数据服务注册与复用。
- 数据修改通过表暴露的属性或方法完成，并通过表入口标记 dirty；不要把底层 `Data` 当作外部可变入口，也不要让业务层依赖日志文本判断加载或保存结果。
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
