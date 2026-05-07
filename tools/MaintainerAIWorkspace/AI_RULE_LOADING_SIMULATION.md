# EFrame AI 规则按需加载模拟

这份文档是维护者专用的模拟结果，用来验证 `rules/loading-policy.yaml` 是否能减少上下文过载和规则污染。

## 模拟结论

当前策略满足按需加载目标，且已经经过 A-E 输出迁移后的二次自审：

- 普通提问只加载 always-on 视图，不加载 audit / release / specialized skills。
- UI、资源、数据、目录任务默认只加载对应 specialized skill，其他领域走 optional + loadWhen。
- 审查任务会加载 audit 视图，但 specialized skills 仍是按发现类型加载。
- AI 契约维护和 release check 才会加载 maintainer 规则。

已完成并复查：

- Managed blocks 只保留平台路由和最小 baseline。
- `instruction.eframe-always-on` 只保留最小 full rules，并把 startup / UI / resource / data / directory 细节改为 handoff。
- `EFRAME_AI_API_INDEX.md` 只承载 API lookup 和短行为提示。
- Coordinator/audit skills 只做分流和审查框架，详细 checklist 回到 references。
- Unity asset edits 已通过 instruction `applyTo` 覆盖 `.prefab`、`.unity`、`.asset`。

## 模拟 1：普通解释问题

用户请求：

```text
解释一下 EFrame UI 为什么不建议业务代码直接操作 UIViewHandle。
```

匹配场景：`question-or-analysis`

加载：

- `instruction.eframe-always-on`
- `support-doc.api-index`：仅当需要确认 `UIViewHandle` API 位置时加载

避免：

- `skill.guideline-audit`
- `maintainer.release-checklist`
- `maintainer.ai-contract-skill`

预期上下文：

- `U01`、`U02` 的 summary/full 边界
- 可能带 `EFRAME_AI_API_INDEX` 的 UI API note

污染风险：低。不会加载 release、data、resource、audit checklist。

## 模拟 2：新增一个 UI 弹窗

用户请求：

```text
新增一个设置弹窗，有 prefab、controller 和按钮事件。
```

匹配场景：`ui-feature`

加载：

- `instruction.eframe-always-on`
- `skill.ui-feature`

按条件加载：

- `skill.directory-structure`：需要决定 prefab/controller 目录时
- `skill.resource-flow`：需要 ResPath、Addressables、prefab handle 时
- `support-doc.api-index`：需要确认 UI API 名称时

避免：

- `skill.data-table`：除非弹窗涉及持久化设置数据
- `maintainer.release-checklist`
- `maintainer.ai-contract-skill`

预期上下文：

- `U01` 到 `U10`
- 与 UI 相关的 `P07/P08/D03/R01`
- 不加载数据表迁移、release manifest、框架发布规则

污染风险：中低。UI prefab 涉及目录和资源时会加载两个 optional skill，但这是必要扩展。

## 模拟 3：新增玩家设置持久化表

用户请求：

```text
新增玩家设置数据表，支持音量、震动开关和版本迁移。
```

匹配场景：`data-table`

加载：

- `instruction.eframe-always-on`
- `skill.data-table`

按条件加载：

- `support-doc.api-index`：需要确认 `IDataService` / `DataTable<TData>` API 时
- `skill.guideline-audit`：实现后要求审查时

避免：

- `skill.ui-feature`
- `skill.resource-flow`
- `maintainer.release-checklist`

预期上下文：

- `T01` 到 `T07`
- `P05` 注入 Context 规则

污染风险：低。不会加载 UI 视图和资源流，除非任务同时触及 UI 或资源。

## 模拟 4：移动一个模块资源并修复 ResPath

用户请求：

```text
把模块内的一个 UI prefab 移到正确目录，并确保 ResPath 使用正确。
```

匹配场景：`resource-flow`，同时触发 optional `directory-structure` 和 `ui-feature`

加载：

- `instruction.eframe-always-on`
- `skill.resource-flow`
- `skill.directory-structure`
- `skill.ui-feature`

按条件加载：

- `support-doc.api-index`：需要确认 Addressables / ResPath API 时

避免：

- `skill.data-table`
- `maintainer.release-checklist`

预期上下文：

- `R01` 到 `R08`
- `D01/D03`
- `U01/U02/U06` 等 UI prefab 相关规则

污染风险：中。任务横跨资源、目录和 UI，加载三类 skill 是合理的；仍然避免了数据和 release 规则。

## 模拟 5：做一次业务项目规范审查

用户请求：

```text
审查这个 feature 是否符合 EFrame 规范。
```

匹配场景：`guideline-audit`

加载：

- `instruction.eframe-always-on`
- `skill.guideline-audit`

按发现类型加载：

- `skill.ui-feature`：发现 UI lifecycle / prefab / controller 问题
- `skill.resource-flow`：发现 Addressables / ResPath / handle 问题
- `skill.data-table`：发现持久化 / dirty / migration 问题
- `skill.directory-structure`：发现目录 ownership 问题
- `support-doc.api-index`：发现依赖具体 API 名称

避免：

- `maintainer.release-checklist`

预期上下文：

- audit checklist 的横向规则
- specialized skill 只在具体问题出现时加载

污染风险：中。审查天然需要更宽上下文，但 release / manifest 规则仍被排除。

## 模拟 6：维护 AI 规则或 manifest

用户请求：

```text
调整 EFrame AI 同步规则，并准备发布。
```

匹配场景：`ai-contract-maintenance` 或 `release-check`

加载：

- `maintainer.ai-contract-skill`
- `maintainer.release-checklist`

按条件加载：

- `instruction.eframe-always-on`：同步业务规则被修改
- `support-doc.api-index`：API index 被修改
- `skill.guideline-audit`：审查 workflow 被修改

避免：

- 普通业务 UI / resource / data skill，除非对应同步文件被修改

预期上下文：

- `G01/G03/G06/G07/G08/G09/W04`
- release checklist
- manifest/hash/changelog gate

污染风险：低。maintainer 规则只在维护场景加载，不污染业务任务。

## 模拟 7：直接编辑 Unity prefab YAML

用户请求：

```text
直接修改 HomeView.prefab，把按钮文本绑定到新的节点引用。
```

匹配场景：`ui-feature` 或 `resource-flow`，同时由 `instruction.eframe-always-on` 的 asset `applyTo` 触发

加载：

- `instruction.eframe-always-on`
- `skill.ui-feature`

按条件加载：

- `skill.resource-flow`：涉及 prefab asset id、Addressables、ResPath 或加载路径时
- `support-doc.api-index`：需要确认 UI / binding API 名称时

避免：

- `maintainer.release-checklist`
- Unity Editor API 作为第一选择

预期上下文：

- `E04` YAML-first：优先直接、局部、透明、可审查地编辑 Unity YAML
- `U01/U02/U05`：业务 UI 入口、内部 UI 边界、`CurrentView`
- `R01/R06`：需要资源路径时使用 `ResPath`，不手改托管 Addressables 条目

污染风险：低。资产编辑触发 always-on 后能拿到 YAML-first 和 fallback-only 口径；只有 UI/资源相关 skill 会按需进入。

## 发现的问题和处理

| 问题 | 处理 |
| --- | --- |
| `instruction.eframe-always-on` 初稿 summary 引入太多业务集合 | 已改为 handoff，fullRules 收窄到最小业务边界 |
| `skill.guideline-audit` 用 `render: none` 表达排除 | 已改为 `exclusions` |
| 架构校验没有检查 view handoffs | 已补充 handoff 引用校验 |
| `eframe-instructions.md` 的 `applyTo` 未覆盖 Unity asset YAML | 已加入 `.prefab`、`.unity`、`.asset` |
| `feature-bootstrap` 将 `QUI` 写进业务 UI bypass 口径 | 已改为 `EFrame.UI/UIControllerBase` |

## 剩余观察点

- `workflow.guideline-audit` 是最宽的业务集合，需要持续防止它变成“全规则加载入口”。
- `feature-bootstrap` 的 optional skill 列表较长，但它现在只做总控和分流，当前可以接受。
- `U02` 和 `U05` 已经人工确认并转为 stable；后续只按实际业务反馈微调措辞。
