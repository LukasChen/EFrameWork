# EFrame AI 提示词路由审查

这份文档用于维护 EFrame AI 契约时做回归审查：检查真实用户表述会触发哪些 `eframe-*` instruction、skill 和 support-doc，并判断分层是否合理。

它是维护者文档，不同步到业务项目。业务项目侧只需要同步精简的 `eframe-*` 契约和 [EFRAME_SKILL_TRIGGER_GUIDE.md](../../AIWorkspace~/support-docs/EFRAME_SKILL_TRIGGER_GUIDE.md)。

## 审查目标

- 用户用自然语言描述需求时，能稳定触发正确 workflow。
- 简单问题不会加载多余 skill 或维护者上下文。
- 宽泛功能需求优先进入 `eframe-feature-bootstrap`，再按需委派 UI、数据、资源或目录 skill。
- 已完成改动的审查需求优先进入 `eframe-guideline-audit`，而不是误当成新功能生成。
- 维护、发布、manifest、同步工具边界只进入 maintainer 层，不进入业务项目 `eframe-*` workflow。

## 何时必须执行

修改任一 AI 契约触发面时，必须执行提示词路由审查：

- `packages/com.eframework.core/AIWorkspace~/instructions/eframe-instructions.md`
- `packages/com.eframework.core/AIWorkspace~/skills/eframe-*/SKILL.md` 的 `description`、`When To Use`、`delegatesTo`、`outputs` 或 `forbiddenPatterns`
- `packages/com.eframework.core/AIWorkspace~/managed-blocks/eframe-*.md`
- `packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_SKILL_TRIGGER_GUIDE.md`
- `packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md`
- 任何会改变业务项目 AI 如何选择 EFrame workflow 的同步脚本、入口文件或 setup 文档

只修改 maintainer-only 文档时不需要更新 manifest，但如果修改会改变业务 AI 的触发方式，必须同时更新 manifest、changelog，并按 release checklist 记录本审查结论。

## 分层判定

| 契约层 | 应承载内容 | 不应承载内容 |
| --- | --- | --- |
| `eframe-instructions.md` | 始终生效、短小稳定的边界，例如 `Context`、`QUI`、`ResPath`、托管资源目录。 | 长流程、发布步骤、模板维护细节、某次 bug 的临时处理。 |
| `eframe-feature-bootstrap` | 跨 UI、数据、资源、Procedure 或模块的功能接入协调。 | 单一 UI 细节、单一资源接入细节、发布维护流程。 |
| specialized `eframe-*` skill | UI、数据表、资源、目录、AI Loop 验收等明确工作流。 | 其他领域的完整规则副本，或 maintainer-only 检查。 |
| `eframe-guideline-audit` | 已实现改动、分支、目录或接入结果的规范审查。 | 新功能详细实现，或正式 release checklist。 |
| support-doc | API 索引、触发短语、查询型参考。 | 每次都要加载的长规则，或维护者发布流程。 |
| maintainer doc / `maintainer-*` skill | AI 契约维护、manifest、release、preview、同步边界审查。 | 业务项目生成代码时必须加载的规则。 |

## 标准审查流程

1. 收集或新增真实用户表述，优先使用用户会自然输入的短句，不刻意堆触发词。
2. 为每条表述标注期望触发的最小契约层。
3. 标注不应触发的契约，避免因为描述过宽导致上下文过载。
4. 检查现有 skill `description`、`When To Use` 和 support trigger guide 是否能稳定覆盖。
5. 如果触发不足，优先调整 skill description 或 support trigger guide；只有长期常驻边界才进入 instruction。
6. 如果触发过多，收窄 skill description，或把“委派关系”留在 coordinator skill 里，避免多个 specialized skill 同时自触发。
7. 修改同步层后按 release checklist 更新 manifest 和 hash；只改 maintainer 文档时不更新 manifest。

## 用户表述回归表

| 用户自然表述 | 真实意图 | 期望触发契约 | 不应触发 | 分层判定 |
| --- | --- | --- | --- | --- |
| `做一个 EFrame Settings 弹窗。` | 新增弹窗 UI。 | `eframe-ui-feature`，必要时委派 `eframe-resource-flow`。 | `eframe-feature-bootstrap` 不应成为必需，除非还涉及流程/模块。 | 合理；`弹窗` + `EFrame` 足够稳定。 |
| `加一个主界面商城入口。` | 修改现有页面 UI。 | `eframe-ui-feature`。 | `eframe-data-table`、`eframe-resource-flow`。 | 合理；若要买卖数据，再由实现过程委派数据层。 |
| `做一个背包页面，能显示道具列表。` | 新页面 + 动态列表。 | `eframe-ui-feature`，可能引用 UI checklist。 | `eframe-data-table` 不应默认触发。 | 合理；“显示道具列表”不等于持久化数据表。 |
| `接入 PlayerProfile 数据表。` | 新增持久化玩家资料。 | `eframe-data-table`。 | `eframe-feature-bootstrap`。 | 合理；数据表是明确 specialized skill。 |
| `保存玩家音量设置。` | 持久化设置数据。 | `eframe-data-table`。 | `eframe-ui-feature`，除非同时要求设置界面。 | 合理；`设置数据` 语义应覆盖。 |
| `玩家进度要支持版本迁移。` | 数据 schema 迁移。 | `eframe-data-table`。 | UI、资源 skill。 | 合理；迁移属于数据表 owns。 |
| `把头像改成 EFrame 托管资源，用 ResPath 加载。` | 资源目录 + 生成路径加载。 | `eframe-resource-flow`，必要时委派 `eframe-directory-structure`。 | `eframe-ui-feature`，除非头像在 UI prefab 内。 | 合理；`托管资源`、`ResPath` 都是强触发词。 |
| `这个 prefab 应该放哪里？` | 判断目录归属。 | `eframe-directory-structure`。 | `eframe-resource-flow` 不应先触发。 | 合理；先判 owner，再决定 Addressables/ResPath。 |
| `把新场景接到流程里。` | Procedure 或场景流程接入。 | `eframe-feature-bootstrap`，再委派目录/资源。 | 单独 UI/data skill。 | 合理；流程接入是 coordinator 入口。 |
| `新增一个抽卡模块。` | 跨 UI、数据、资源的模块功能。 | `eframe-feature-bootstrap`。 | specialized skill 不应全部独立抢触发。 | 合理；由 coordinator 决定后续委派。 |
| `把 demo 代码整理成正式模块。` | 非标准结构收敛。 | `eframe-feature-bootstrap` + `eframe-directory-structure`。 | release/maintainer 契约。 | 合理；这是业务结构重构。 |
| `检查当前分支是否符合 EFrame 约定。` | 规范审查。 | `eframe-guideline-audit`。 | 新功能生成 skill。 | 合理；“检查/约定”应进入 audit。 |
| `看下 UI 有没有绕过 QUI。` | UI 规范审查。 | `eframe-guideline-audit`，可委派 `eframe-ui-feature`。 | 新建 UI workflow。 | 合理；审查优先 audit，specialized skill 作参考。 |
| `查一下有没有手写 Addressables 字符串。` | 资源违规审查。 | `eframe-guideline-audit`，可委派 `eframe-resource-flow`。 | 目录规划。 | 合理；这是已实现代码审查。 |
| `帮我修复一个 HomeView 打不开的 bug。` | UI bug 修复。 | `eframe-ui-feature`，完成后 `eframe-guideline-audit`。 | 数据、AI Loop 默认不触发。 | 合理；如果用户要求截图/点击验证，再触发 AI Loop。 |
| `用截图验收 Settings 弹窗。` | PlayMode UI 可见验收。 | `eframe-ai-loop-validation`，必要时参考 `eframe-ui-feature`。 | 数据/资源 skill。 | 合理；“截图验收”是强触发词。 |
| `录制并回放登录流程。` | AI Loop 输入回放验收。 | `eframe-ai-loop-validation`。 | feature bootstrap，除非还要实现登录流程。 | 合理；验收和实现分离。 |
| `初始化一个新 EFrame 项目。` | 项目初始化。 | 常驻 instruction + 初始化脚本说明；不应强制 skill。 | `eframe-feature-bootstrap`。 | 需注意；这是工具使用，不是业务功能骨架。 |
| `同步 EFrame AI workspace。` | AI 同步工具使用。 | 常驻 instruction / support-doc；框架仓库中用 maintainer 契约。 | 业务 specialized skills。 | 合理；业务侧只执行工具，不进入维护流程。 |
| `更新 eframe-instructions 里的 UI 规则。` | 框架 AI 契约维护。 | `maintainer-ai-contract`。 | 任何业务 `eframe-*` skill。 | 合理；这是 maintainer-only。 |
| `发布 0.7.7 patch。` | 正式发布。 | `maintainer-ai-contract` + release checklist。 | 业务项目 audit skill。 | 合理；必须先确认范围、版本、预览验证和远端。 |
| `做一个 preview/core-0.7.7-rc.1 给项目验证。` | 预览版入口。 | maintainer release flow。 | 业务同步层。 | 合理；不要同步给业务项目。 |
| `这个 API 怎么调用？` | API 查询。 | `EFRAME_AI_API_INDEX.md` 或用户文档。 | workflow skill，除非要实际改代码。 | 合理；查询型任务避免过载。 |
| `解释一下 QUI 和 UIController 的关系。` | 概念解释。 | instruction + user docs/API index。 | 文件编辑 workflow。 | 合理；先回答，未经批准不改文件。 |
| `把资源目录规范写进项目私有规则。` | 项目私有 AI 规则。 | 常驻 instruction 的协作边界。 | 修改同步的 `eframe-*` 文件。 | 合理；项目差异写项目自有 instruction。 |

## 审查结论模板

维护者修改 skill 或 instruction 后，用下面格式记录结论：

```md
### Prompt Routing Audit

- 覆盖范围：UI / data / resource / audit / maintainer
- 新增或修改契约：
- 表述回归结果：通过 N 条，需调整 N 条
- 触发不足：
- 触发过宽：
- 是否需要 manifest 更新：
- 后续验证：
```

## 调整建议

- 如果用户短句无法稳定触发，先把常见中文自然表述补进对应 skill `description` 或 `When To Use`。
- 如果多个 specialized skill 同时触发，优先让 `eframe-feature-bootstrap` 做 coordinator，并从 specialized skill description 中删掉过宽表述。
- 如果一个规则只用于“审查 EFrame 自己怎么维护 AI 契约”，放入 maintainer 文档或 `maintainer-*` skill。
- 如果一条信息只是帮助用户显式触发某个 skill，放入 `EFRAME_SKILL_TRIGGER_GUIDE.md`，不要塞进 always-on instruction。
- 如果一条信息决定业务代码的稳定边界，例如不要手写 Addressables 字符串，才放入 `eframe-instructions.md`。

## 基线审查结果

2026-05-07 按当前 `eframe-*` skill description、`When To Use` 和 [EFRAME_SKILL_TRIGGER_GUIDE.md](../../AIWorkspace~/support-docs/EFRAME_SKILL_TRIGGER_GUIDE.md) 做了一轮人工回归。初次回归发现 3 条需观察表述，随后已在 trigger guide 中补充 `主界面入口`、`EFrame 约定检查` 和 `录制并回放`，并复核通过。

### 结果摘要

- 通过：25 条。
- 需观察：0 条。
- 明确过度触发：0 条。
- 明确欠触发：0 条。
- 需要 manifest 更新：本次修改了同步到业务项目的 `EFRAME_SKILL_TRIGGER_GUIDE.md`。

### 已优化表述

| 表述 | 期望触发 | 观察结果 | 建议 |
| --- | --- | --- | --- |
| `加一个主界面商城入口。` | `eframe-ui-feature` | 已补充 `主界面入口` 触发短语。 | 后续观察是否还需要覆盖 `首页入口`。 |
| `检查当前分支是否符合 EFrame 约定。` | `eframe-guideline-audit` | 已补充 `EFrame 约定检查` 触发短语；README 示例改为更强触发的 `做一次 EFrame 规范审查。`。 | 无。 |
| `录制并回放登录流程。` | `eframe-ai-loop-validation` | 已补充 `录制并回放` 触发短语。 | 无。 |

### 分层结论

- `eframe-feature-bootstrap` 作为跨 UI、数据、资源、Procedure、模块的 coordinator 分层合理；当前没有发现它会稳定抢走单一 UI、数据或资源需求。
- `eframe-guideline-audit` 对“已完成改动/当前分支/有没有违反约定”的审查语义覆盖合理，但中文自然表述可以继续扩充。
- `eframe-directory-structure` 与 `eframe-resource-flow` 的边界合理：先判断“放哪里”，再处理 Addressables 和 `ResPath`。
- maintainer-only 表述，例如 release、preview、manifest、`eframe-instructions` 修改，会进入 `maintainer-ai-contract`，没有混入业务项目 workflow。
