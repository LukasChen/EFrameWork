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
- `UIController` 是否区分实例级 `OnViewCreated()` / `OnViewDestroyed()` 与每次打开关闭的 `OnViewOpened()` / `OnViewClosed()`
- View 是否保持无参构造并通过 `OnBindingSet()` 接入 `QUIBinding`
- Controller 是否通过 `TypedViewHandle.TypedView` 访问 View，而不是使用 `View` facade
- 自定义 transition 是否能处理中断，避免已失效 open/close 异步结果覆盖活跃状态

## 目录与命名

- 代码与资源是否分离
- 是否向非标准目录写入新功能
- 命名是否符合 `ProcedureXxx`、`XxxViewController`、`XxxView.prefab`

## 资源路径

- 是否集中管理路径
- 是否出现重复硬编码字符串
- 是否使用 `ResPath.Generated` 或明确资源 id 入口

## AI 配置

- 业务项目是否保留 EFrame managed block，不复制整份框架规则到项目自有 instruction
- 是否存在手改同步得到的 `.github/instructions/eframe-*` 或 `.github/skills/eframe-*`
- `.github/eframe/EFRAME_AI_API_INDEX.md` 和 `.github/eframe-ai.manifest.json` 是否存在
- 初始化脚本和接入说明是否可用
