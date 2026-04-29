---
name: "EFrame Runtime Rules"
description: "Use when editing Unity runtime C# for EFrame, EFrameWork, Procedure, QUI, UIController, startup flow, gameplay feature code, or resource loading paths."
applyTo: "{**/packages/com.eframework.core/Runtime/**/*.cs,**/Assets/App/Runtime/**/*.cs,**/Assets/Modules/**/*.cs,**/Assets/Scripts/**/*.cs}"
---

# EFrame Runtime Rules

This file is the synced runtime contract for projects that use EFrameWork. Keep it focused on stable runtime rules that should also apply in business projects after AI cold-start and sync.

Use these rules by reading top-down: first decide the runtime layer, then follow the relevant section.

Project-local `project-*.instructions.md` overlays may add more specific business rules. When they conflict with this framework contract, follow the more specific project rule while preserving the EFrame runtime service boundaries below.


## UI Contract
- `Procedure` 只负责状态切换、进入/退出编排和生命周期清理；页面、弹窗、提示层切换优先作为现有流程内的 UI 行为，不滥增新 `Procedure`。
- UI 必须通过 `QUI`、`UIController` 和框架 UI 层级管理；不要在场景里手工堆常驻顶层 Canvas、EventSystem 或绕过框架的 UI 生命周期入口。
- Runtime UI 组件、适配器、动画、helper 和生成绑定代码统一落在 `EFrameWork.Runtime.UI` 及其稳定子命名空间，例如 `Components`、`Layout`、`UIHelper`、`Generated`。
- 命名保持框架约定：`ProcedureXxx`、`XxxViewController`、`XxxView.prefab`，组件目录、类名和命名空间大小写一致。

## UI Main Chain

- 创建、打开、关闭、缓存和释放 View 统一走 `IUIService/QUI` 与 `UIViewHandle` 语义；业务代码不要直接驱动 `BindingViewBase` 的生命周期。
- `BindingViewBase` 只保留 binding wrapper、组件缓存初始化和底层生命周期原语；资源实例化、缓存命中、释放决策和 transition 策略属于宿主或 handle。
- `UIControllerBase` 负责 UI 编排，不在构造函数中依赖业务数据或未初始化服务；显示 UI 前由框架上下文完成绑定。
- View 模板保持无参构造，通过 `SetBinding()` / `OnBindingSet()` 接入生成的 `QUIBinding`，不要把可复用的业务状态塞进 View 缓存。

## Resource Paths And Addressables

- 资源路径不要散落硬编码；如果当前模块还没有 `ResPath`，至少新增集中常量，不要在多个类里重复写字符串。
- 使用 Addressables 动态加载时，优先依赖 `ResPath.Generated`、稳定入口 `ResPath` 或 `AssetReference`，不要直接把完整地址写进业务逻辑。
- 使用 Addressables 加载或实例化运行时资源时，走 `EFrame.Current.Assets.LoadAsync(...)`、`InstantiateAsync(...)` 和句柄释放。

## Startup And Runtime Access

- 修改启动链路时必须保证 `EFrame.Initialize(...)` 先完成，再访问 `EFrame.Current.UI`、`EFrame.Current.Audio`、`EFrame.Current.Data` 等框架服务。
- `QUI` 依赖的 Unity SortingLayer 配置要由项目初始化链路补齐；运行时如果缺失层级，应保留明确校验和修复提示，不要静默回落。

## Events And Data

- 事件订阅优先使用 `EFrame.Current.Events.SubscribeScoped(...)` 并在 `OnLeave`、`OnDestroy` 或控制器释放时 `Dispose`，避免手写订阅/退订不成对。
- 数据表必须有无参构造、稳定且唯一的 `StorageKey`，注册统一走 `EFrame.Current.Data.RegisterTable<T>()` 并复用已注册表。
- 数据修改通过表暴露的属性或方法完成，并使用 `SetValue(...)` / `Mutate(...)` 这类入口标记 dirty；不要把底层 `Data` 当作外部可变入口。
- 数据迁移、加载来源和保存结果优先使用表内 `CurrentVersion` / `Migrate(...)`、`LastLoadResult`、`LastSaveResult` 等结构化状态；不要让业务层解析日志文本或假设 `Save()` 一定写盘。
- 框架层默认只在 pause/quit 触发保存；项目如果需要更细粒度保存，由业务在明确时机调用 `SaveAll()` 或表级 `Save()`。

## Framework Contract Boundary

- 业务项目可以用 `project-*` overlay 补充本地 Procedure、UI、资源和数据规则；不要直接修改同步得到的 `eframe-*` 文件来表达项目私有差异。
- 如果项目确实需要偏离本契约，优先在本地 overlay 说明差异边界，并保持 `EFrame.Initialize(...)`、`QUI/UIController`、`ResPath`、Addressables 和数据表持久化入口仍然可被框架工具识别。
