---
name: "EFrame Runtime Rules"
description: "Use when editing Unity runtime C# for EFrame, EFrameWork, Procedure, QUI, UIController, startup flow, gameplay feature code, or resource loading paths."
applyTo:
  - "**/Assets/**/Runtime/**/*.cs"
  - "**/Assets/Scripts/Runtime/**/*.cs"
---

# EFrame Runtime Rules

- 先判断需求属于哪一层：`Procedure`、`UIController`、场景对象、服务层，避免把状态机、界面交互、资源加载混到同一个类里。
- 新增流程类时坚持 `ProcedureXxx` 命名，`OnEnter` 与 `OnLeave` 的事件订阅、对象创建和清理必须成对出现。
- 如果只是页面、弹窗、提示层切换，优先做成现有 `Procedure` 内的 UI 行为，不要滥增新 `Procedure`。
- UI 只能挂到 `QUI` 对应层级，不能自己新建独立顶层 Canvas 作为常驻主 UI。
- 资源路径不要散落硬编码；如果当前模块还没有 `ResPath`，至少新增集中常量，不要在多个类里重复写字符串。
- 使用 Addressables 动态加载时，优先依赖 `ResPath.Generated`、稳定入口 `ResPath` 或 `AssetReference`，不要直接把完整地址写进业务逻辑。
- 修改启动链路时必须保证 `EFrame.Initialize(...)` 先完成，再访问 `EFrame.UI`、`EFrame.Audio`、`EFrame.DataManager` 等框架服务。
- 如果项目仍处于过渡目录结构，新增功能优先朝标准目录收敛，而不是继续堆临时脚本和临时资源路径。