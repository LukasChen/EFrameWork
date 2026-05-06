# EFrame Feature Checklist

## 1. 决策

- 这是新 `Procedure`、普通页面、弹窗还是独立 `MiniGame`？
- 是否真的发生了状态切换、相机切换、场景根对象切换？
- 是否只是所在流程里的一个 UI 子状态？

## 2. 目录映射

- 运行时代码：`Assets/App/Runtime/...`
- 主资源：`Assets/App/Res/...`
- 业务模块：`Assets/Modules/<Name>/{Runtime,Res,Editor,Scenes}`
- 生成代码：`Generated` 或固定生成目录
- 新建、移动或归类文件前用 `eframe-directory-structure` 判断细分职责、场景目录和资源归属

## 3. 命名

- `ProcedureXxx`
- `XxxViewController`
- `XxxView.prefab`
- `ResPath.Generated` 自动生成资源 id

## 4. 生命周期模板

`Procedure`:

- 只在状态切换、场景生命周期或玩法根对象切换时新增 `Procedure`
- 使用 `EFramework.Runtime.Procedure.EFrameProcedure`
- Procedure-owned 资源在 `OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)` 中预加载
- 一次性进入参数通过 `ChangeState<TProcedure>(payload)` / `ProcedureEnterContext` 传递
- 持久状态进入 `Context.Data`、模块服务或事件，不放在一次性 enter payload 中
- `OnEnter`: 保存上下文、注册事件、创建对象、派发进入事件
- `OnLeave`: 退订事件、关闭 UI、销毁对象、清空引用

`UIController`:

- 创建时绑定按钮和订阅事件
- 销毁时解除监听并清空缓存引用
- 通过 `Show()` / `Hide()` 暴露显示语义

## 5. 资源与层级

- UI 子任务是否已交给 `eframe-ui-feature`
- 数据表子任务是否已交给 `eframe-data-table`
- 资源/Addressables 子任务是否已交给 `eframe-resource-flow`
- 目录/App vs Module ownership 子任务是否已交给 `eframe-directory-structure`
- 框架源码、同步工具或 AI 发布事项是否已标记为超出业务功能范围

## 6. 验证

- 能否在不改启动场景业务内容的前提下接入
- 是否存在成对清理
- 是否能收敛到标准目录
- 是否避免在 bootstrap skill 中复制 specialized skill 的细节规则
