---
name: eframe-feature-bootstrap
description: 'Plan and assemble Unity EFrame feature scaffolding across Procedure, UI, data, resources, startup flow, popup flow, or MiniGame modules. Use as the top-level coordinator when a feature spans multiple EFrame concerns; delegate detailed UI, data-table, or resource work to the specialized eframe skills.'
argument-hint: 'Describe the feature, target state, and whether it is a Procedure, popup, page, or MiniGame.'
user-invocable: true
capabilities:
  - eframe.feature.coordinate
  - eframe.procedure.scaffold
  - eframe.module.scaffold
owns:
  - feature boundary classification
  - cross-skill integration plan
  - Procedure-level lifecycle placement
delegatesTo:
  - eframe-ui-feature
  - eframe-data-table
  - eframe-resource-flow
  - eframe-guideline-audit
outputs:
  - smallest valid EFrame feature boundary
  - coordinated file and resource placement
  - validation handoff checklist
forbiddenPatterns:
  - putting business gameplay logic in the startup scene
  - bypassing QUI/UIController for feature UI
  - scattering runtime resource address strings
---

# EFrame Feature Bootstrap

## When To Use

- 新建业务页面、弹窗、业务模块或 `Procedure`
- 将演示代码或旧结构重构为正式结构
- 给新项目建立 `View + Controller + Procedure + ResPath` 的最小骨架
- 判断某个需求应该落在主流程、弹层还是独立玩法目录

## Coordinator Flow

1. Classify the request: `Procedure`, UI, data table, resource flow, editor/bootstrap, or a combined feature.
2. Decide whether to create new structure or extend existing structure; prefer the smallest feature boundary that preserves EFrame lifecycle rules.
3. Delegate detailed work:
   - UI pages, popups, prefabs, controllers, bindings: use `eframe-ui-feature`.
   - Persistent gameplay/settings/progress data: use `eframe-data-table`.
   - Addressables, `ResPath`, prefab/audio/scene resources, async handles: use `eframe-resource-flow`.
   - Framework AI/release/sync/template maintenance: use `maintainer-ai-contract`.
4. Integrate the outputs so naming, directories, lifecycle cleanup, and resource paths agree across the feature.
5. Validate the whole feature with `eframe-guideline-audit` when the implementation is complete.

## Procedure

Use `EFrameWork.Runtime.Procedure.EFrameProcedure` for new procedures, preload Procedure-owned resources in `OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)`, and use `ChangeState<TProcedure>(payload)` for one-shot enter data; persistent state belongs in `Context.Data`, module services, or events.

1. 先判定需求是否真的需要新 `Procedure`。
2. 如果不涉及状态切换、场景生命周期或玩法根对象切换，优先落成当前流程内的 UI 行为。
3. 如果需要新 `Procedure`，约束进入退出对称、资源清理完整、跳转条件明确。

## Structure

1. 把运行时代码放入目标结构：`Assets/App/Runtime/...` 或 `Assets/Modules/<Name>/Runtime/...`。
2. 把资源放入可映射目录：`Assets/App/Res/UI/...`、`Assets/App/Res/SceneAssets/...`、`Assets/Modules/<Name>/Res/...` 等。
3. 为资源路径建立集中入口，避免散写字符串。
4. 具体目录职责和命名边界以仓库根的 `UNITY_DIRECTORY_STRUCTURE.md` 为准；项目特殊约束只写进 `project-*` overlay。

## Validation

1. 校验生命周期是否成对：进入/退出、创建/销毁、订阅/退订。
2. 校验命名、目录、资源路径、UI 层级和数据入口是否由对应 specialized skill 覆盖。
3. 如果项目里仍有过渡目录，明确本次改动如何推进到目标结构。
4. 如果一个子任务需要超过几条具体实现规则，不要把规则复制到这里；改用对应 specialized skill 或 reference。
5. 如果在框架仓库内发现本次功能改变了公共模板、冷启动流程或 AI 协作层，改用 maintainer skill 处理同步和发布检查。

需要更细的骨架和核对项时，加载 [feature checklist](./references/feature-checklist.md)。
