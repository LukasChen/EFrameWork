---
name: eframe-feature-bootstrap
description: 'Create or refactor Unity EFrame features. Use when adding Procedure, UIController, View prefab, resource paths, startup flow, popup flow, or MiniGame scaffolding under EFrame standards.'
argument-hint: 'Describe the feature, target state, and whether it is a Procedure, popup, page, or MiniGame.'
user-invocable: true
---

# EFrame Feature Bootstrap

## When To Use

- 新建业务页面、弹窗、玩法模块或 `Procedure`
- 将演示代码或旧结构重构为正式结构
- 给新项目建立 `View + Controller + Procedure + ResPath` 的最小骨架
- 判断某个需求应该落在主流程、弹层还是独立玩法目录

## Procedure

1. 先判定需求是否真的需要新 `Procedure`。
2. 如果不涉及状态切换、场景生命周期或玩法根对象切换，优先落成当前流程内的 UI 行为。
3. 如果需要新 `Procedure`，约束进入退出对称、资源清理完整、跳转条件明确。

## Structure

1. 把运行时代码放入目标结构：`Assets/App/Runtime/...` 或 `Assets/MiniGames/<Name>/Runtime/...`。
2. 把资源放入可映射目录：`Assets/App/Res/UI/...`、`Assets/App/Res/Scenes/...` 等。
3. 为资源路径建立集中入口，避免散写字符串。

## UI

1. 页面预制体使用 `XxxView.prefab`，控制器使用 `XxxViewController`。
2. 主页面进 `QuiPanel`，弹窗进 `QuiPopUp`，提示类进 `QuiTooltip` 或 `QuiTop`。
3. 控制器负责按钮绑定、事件订阅、数据刷新和销毁清理，不直接深层 `transform.Find`。

## Validation

1. 校验生命周期是否成对：进入/退出、创建/销毁、订阅/退订。
2. 校验命名、目录、资源路径是否符合规范。
3. 如果项目里仍有过渡目录，明确本次改动是兼容旧结构还是推进到目标结构。

需要更细的骨架和核对项时，加载 [feature checklist](./references/feature-checklist.md)。