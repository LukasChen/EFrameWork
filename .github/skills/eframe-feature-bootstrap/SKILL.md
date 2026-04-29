---
name: eframe-feature-bootstrap
description: 'Create or refactor Unity EFrame features. Use when adding Procedure, UIController, View prefab, resource paths, startup flow, popup flow, or MiniGame scaffolding under EFrame standards.'
argument-hint: 'Describe the feature, target state, and whether it is a Procedure, popup, page, or MiniGame.'
user-invocable: true
---

# EFrame Feature Bootstrap

## When To Use

- 新建业务页面、弹窗、业务模块或 `Procedure`
- 将演示代码或旧结构重构为正式结构
- 给新项目建立 `View + Controller + Procedure + ResPath` 的最小骨架
- 判断某个需求应该落在主流程、弹层还是独立玩法目录

## Procedure

1. 先判定需求是否真的需要新 `Procedure`。
2. 如果不涉及状态切换、场景生命周期或玩法根对象切换，优先落成当前流程内的 UI 行为。
3. 如果需要新 `Procedure`，约束进入退出对称、资源清理完整、跳转条件明确。

## Structure

1. 把运行时代码放入目标结构：`Assets/App/Runtime/...` 或 `Assets/Modules/<Name>/Runtime/...`。
2. 把资源放入可映射目录：`Assets/App/Res/UI/...`、`Assets/App/Res/SceneAssets/...`、`Assets/Modules/<Name>/Res/...` 等。
3. 为资源路径建立集中入口，避免散写字符串。
4. 具体目录职责和命名边界以仓库根的 `UNITY_DIRECTORY_STRUCTURE.md` 为准；项目特殊约束只写进 `project-*` overlay。

## UI

1. 页面预制体使用 `XxxView.prefab`，控制器使用 `XxxViewController`。
2. 主页面进 `QuiPanel`，弹窗进 `QuiPopUp`，提示类进 `QuiTooltip` 或 `QuiTop`。
3. 新项目或新模块接入 UI 前，先确认项目初始化链路已经补齐 `QUI` 依赖的 Unity SortingLayer；不要把这一步只留给手工菜单操作。
4. `QUIBinding` 自动生成的访问类默认命名空间保持为 `EFrameWork.Runtime.UI.Generated`；新模板和示例 prefab 不要再写旧的 `EFrameWork.UI.Generated`。
5. 控制器负责按钮绑定、事件订阅、数据刷新和销毁清理，不直接深层 `transform.Find`。
6. 控制器构造阶段不读取业务数据或假设框架服务已初始化；资源加载、View 创建和服务访问放在显示/进入流程之后。
7. 创建、打开、关闭、缓存和释放 View 走 `IUIService/QUI` 与 `UIViewHandle` 语义，不让业务层直接驱动 `BindingViewBase` 生命周期。

## Assets

1. Addressables 资源路径仍通过 `ResPath.Generated`、稳定 `ResPath` 入口或 `AssetReference` 获取。
2. 运行时加载优先使用 `EFrame.Current.Assets.LoadAsync(...)`、`InstantiateAsync(...)` 与 `AssetHandle`/`InstanceHandle` 的 `Dispose` 释放。
3. 新业务功能不使用同步加载和 `WaitForCompletion` 作为默认资源加载路径。

## Events And Data

1. 事件订阅优先使用 `EFrame.Current.Events.SubscribeScoped(...)`，并把返回的 `IDisposable` 纳入当前 `Procedure`、Controller 或服务的生命周期清理。
2. 数据表继承 `DataTable<TData>`，保留无参构造，通过 `EFrame.Current.Data.RegisterTable<T>()` 注册和自动加载。
3. 每张数据表都显式声明稳定 `StorageKey`，不要默认用类名作为持久化键，避免后续重命名导致断档；新增表时同时确认没有和已有表复用同一个键。
4. 数据表对外暴露业务属性或方法，不暴露底层 `Data` 作为外部可变入口；属性 setter 使用 `SetValue(...)`，复杂变更使用表内部方法，并让 `Mutate(...)` 或等价逻辑显式返回是否真的改动了数据。
5. Dirty 只表示数据已变更，框架默认在 pause/quit 保存；业务需要立即保存时显式调用 `SaveAll()` 或表级 `Save()`。
6. 不在业务模块里自行保存第二份数据表单例，统一从 `EFrame.Current.Data.GetTable<T>()` 读取。
7. 需要兼容旧存档时，在表内声明 `CurrentVersion` 并实现 `Migrate(data, fromVersion)`；新文件保存为 versioned envelope，旧裸数据会按 `version 0` 进入迁移链。
8. 如果业务需要根据加载来源决定提示、修复或埋点，优先读取表的 `LastLoadResult.Status` 与 `ReasonCode`，从结构化状态判断是否来自备份、迁移或默认值回退，而不是解析消息文本。
9. 如果业务需要根据保存结果决定提示、重试或埋点，优先读取表的 `LastSaveResult`，区分已写盘、因不 dirty 跳过和保存失败。

## Validation

1. 校验生命周期是否成对：进入/退出、创建/销毁、订阅/退订。
2. 校验命名、目录、资源路径是否符合规范。
3. 如果项目里仍有过渡目录，明确本次改动如何推进到目标结构。
4. 如果在框架仓库内发现本次功能改变了公共模板、冷启动流程或 AI 协作层，改用 maintainer skill 处理同步和发布检查。

需要更细的骨架和核对项时，加载 [feature checklist](./references/feature-checklist.md)。
