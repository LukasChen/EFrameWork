---
name: eframe-feature-bootstrap
description: 'Plan and assemble Unity EFrame feature scaffolding across Procedure, UI, data, resources, startup flow, popup flow, or MiniGame modules. Use as the top-level coordinator when a feature spans multiple EFrame concerns, or when the task mentions 功能接入, 功能骨架, 新功能规划, 流程接入, or 模块接入.'
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
  - eframe-directory-structure
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
  - bypassing EFrame.UI/UIControllerBase for feature UI
  - scattering runtime resource address strings
---

# EFrame Feature Bootstrap

## When To Use

- 新建业务页面、弹窗、业务模块或 `Procedure`
- 将演示代码或非标准结构收敛到正式结构
- 给新项目建立 `View + Controller + Procedure + ResPath` 的最小骨架
- 判断某个需求应该落在主流程、弹层还是独立玩法目录

## Coordinator Flow

1. Classify the request: `Procedure`, UI, data table, resource flow, or a combined feature.
2. Decide whether to create new structure or extend existing structure; prefer the smallest feature boundary that preserves EFrame lifecycle rules.
3. Delegate detailed work instead of copying specialized rules:
   - File, resource, scene, generated-code, or module directory placement: use `eframe-directory-structure`.
   - UI pages, popups, prefabs, controllers, bindings: use `eframe-ui-feature`.
   - Persistent gameplay/settings/progress data: use `eframe-data-table`.
   - Addressables, `ResPath`, prefab/audio/scene resources, async handles: use `eframe-resource-flow`.
4. Integrate the outputs so naming, directories, lifecycle cleanup, and resource paths agree across the feature.
5. Validate the whole feature with `eframe-guideline-audit` when the implementation is complete.
6. If the request is mainly about project initialization, editor bootstrap automation, framework release flow, or AI sync tooling, treat it as outside this business feature skill.

## Coordinator Boundaries

- Own only the feature boundary decision, integration plan, and Procedure-level lifecycle placement.
- Use `EFrameProcedure` only when the feature truly changes state, scene lifecycle, or gameplay root ownership.
- Keep detailed UI, data, resource, directory, and audit rules in their specialized skills and references.
- Keep project-specific directory or naming exceptions in project-owned instructions, not synced `eframe-*` files.

## Validation

1. Confirm each delegated concern has been handled by the owning skill or reference.
2. Confirm lifecycle ownership is explicit for procedures, controllers, resources, events, and data.
3. Confirm the feature did not add framework-maintenance, release, or AI sync work to a business workflow.

需要更细的骨架和核对项时，加载 [feature checklist](./references/feature-checklist.md)。
