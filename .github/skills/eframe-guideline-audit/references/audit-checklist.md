# EFrame Audit Checklist

## 启动场景

- 是否保持唯一启动场景
- 是否只有 `Boot`、必要相机和框架组件
- 是否避免业务 UI、业务角色、玩法场景内容常驻

## Procedure

- 是否只做状态跳转与资源编排
- `OnEnter` / `OnLeave` 是否对称
- 是否避免在流程里堆 UI 动画、临时数据计算和深层节点操作

## UI / QUI

- 是否通过 `QUI` 层级挂载
- 是否存在手工新建顶层 Canvas 或多余 `EventSystem`
- `UIController` 是否负责绑定、刷新、销毁，而不是承担全部业务逻辑

## 目录与命名

- 代码与资源是否分离
- 是否继续向临时目录写入新功能
- 命名是否符合 `ProcedureXxx`、`XxxViewController`、`XxxView.prefab`

## 资源路径

- 是否集中管理路径
- 是否出现重复硬编码字符串
- 是否为迁移到 `ResPath` 预留明确位置

## AI 配置

- 新规范是否同步到 `.github/copilot-instructions.md`
- 是否需要新增或更新 `.github/instructions/*.instructions.md`
- 是否需要新增或更新 `.github/skills/*`
- 初始化脚本和接入文档是否仍可用