# EFrame AI 规则输出迁移计划

这份文档是维护者专用迁移计划，用来说明如何把结构化规则安全落到现有同步输出文件。当前阶段不直接修改 `AIWorkspace~` 正文。

## 目标

把现有同步文件从“多处重复描述规则”逐步迁移为“根据 rule view 渲染或引用规则”，降低上下文过载、重复冲突和平台差异污染。

## 基线

当前结构化层已经具备：

- `rules/registry.yaml`：规则唯一结构化草案。
- `rules/rule-sets.yaml`：按领域和 workflow 分组。
- `rules/rule-views.yaml`：当前输出视图映射。
- `rules/loading-policy.yaml`：按任务加载策略。
- `Test-EFrameAIRuleArchitecture.ps1`：结构引用校验。
- `AI_RULE_LOADING_SIMULATION.md`：按需加载模拟结果。

当前同步输出仍然是人工维护的 markdown 文件，尚未由 registry 生成。

## 当前进度

- Phase A：已通过 `rules/rule-views.yaml` 的 `targetOutput` 和本计划的输出文件映射建立显式对应关系。
- Phase B：已在 `0.6.18` / manifest `0.4.35` 中瘦身三个 managed blocks，使平台入口只保留路由、最小 baseline 和 handoff。
- Phase C 以后尚未执行。

## 输出文件映射

| 当前输出源 | 同步目标 | Rule View | 迁移目标 |
| --- | --- | --- | --- |
| `AIWorkspace~/instructions/eframe-instructions.md` | `.github/instructions/eframe-instructions.md` | `instruction.eframe-always-on` | 最小 always-on 规则，只保留短边界和 handoff |
| `AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md` | `.github/eframe/EFRAME_AI_API_INDEX.md` | `support-doc.api-index` | API lookup + 极短 use/avoid note，不承载 workflow |
| `AIWorkspace~/skills/eframe-feature-bootstrap/SKILL.md` | `.github/skills/eframe-feature-bootstrap/SKILL.md` | `skill.feature-bootstrap` | 总控 workflow，只做分类、边界和 specialized skill handoff |
| `AIWorkspace~/skills/eframe-directory-structure/SKILL.md` | `.github/skills/eframe-directory-structure/SKILL.md` | `skill.directory-structure` | 目录和 ownership 规则主入口 |
| `AIWorkspace~/skills/eframe-ui-feature/SKILL.md` | `.github/skills/eframe-ui-feature/SKILL.md` | `skill.ui-feature` | UI 业务契约主入口 |
| `AIWorkspace~/skills/eframe-resource-flow/SKILL.md` | `.github/skills/eframe-resource-flow/SKILL.md` | `skill.resource-flow` | 资源和 Addressables flow 主入口 |
| `AIWorkspace~/skills/eframe-data-table/SKILL.md` | `.github/skills/eframe-data-table/SKILL.md` | `skill.data-table` | 数据表和事件持久化主入口 |
| `AIWorkspace~/skills/eframe-guideline-audit/SKILL.md` | `.github/skills/eframe-guideline-audit/SKILL.md` | `skill.guideline-audit` | 业务审查入口，不混入 release governance |
| `AIWorkspace~/managed-blocks/eframe-codex-root.md` | `AGENTS.md` managed block | `managed-block.codex` | Codex 平台路由和最小 baseline |
| `AIWorkspace~/managed-blocks/eframe-copilot-root.md` | `.github/copilot-instructions.md` managed block | `managed-block.copilot` | Copilot 平台路由和最小 baseline |
| `AIWorkspace~/managed-blocks/eframe-claude-root.md` | `CLAUDE.md` managed block | `managed-block.claude-code` | Claude Code 平台路由和最小 baseline |
| `AIWorkspace~/skills/*/references/*.md` | `.github/skills/*/references/*.md` | specialized skill references | 详细 checklist，只服务对应 skill |

## 迁移原则

1. 每次只迁移一个输出面，避免一次性改动过大。
2. 先迁移高重复、低风险的 summary / handoff，再迁移 full rule 文案。
3. 每次同步输出变化都必须更新 manifest version/hash 和 changelog。
4. 每次迁移后运行：
   - `tools/MaintainerAIWorkspace/Test-EFrameAIRuleArchitecture.ps1`
   - `tools/Test-EFrameAIRelease.ps1`
5. 不把 maintainer-only 规则放进 `eframe-*` skills。
6. 不把 platform output 写成新的规则源。
7. 不在第一轮做自动生成；先人工瘦身并保持可审查 diff。

## Phase A：加显式映射，不改语义

目标：让现有输出文件和 rule view 对齐，但不改变业务 AI 实际行为。

工作：

- 在 maintainer-only 规则文件中补齐每个输出的 `targetOutput`。
- 检查每个输出文件当前内容是否能映射到对应 rule IDs。
- 必要时在维护文档中记录“当前输出中的哪些段落对应哪些 rule IDs”。

不做：

- 不改 `AIWorkspace~` 正文。
- 不修改 manifest。
- 不改变业务项目同步内容。

验收：

- `Test-EFrameAIRuleArchitecture.ps1` 通过。
- 迁移计划能回答“每个输出文件由哪个 rule view 管”。

## Phase B：瘦身 managed blocks

目标：平台入口只保留路由和最小 baseline，不重复详细规则。

候选文件：

- `AIWorkspace~/managed-blocks/eframe-codex-root.md`
- `AIWorkspace~/managed-blocks/eframe-copilot-root.md`
- `AIWorkspace~/managed-blocks/eframe-claude-root.md`

策略：

- 保留 `G04/G05/G10` 和最小业务入口摘要。
- 保留指向 instruction、API index、skills 的 handoff。
- 删除和 instruction / skills 重复的详细 UI、资源、Procedure 文案。

风险：

- 平台入口太薄可能导致某些 AI 不继续读取 handoff 文件。

缓解：

- 三个平台输出保留清晰的“先读 instruction，再按任务读 skill”的强提示。
- Codex 特别保留 skill 发现提示，因为 Codex 不会自动加载 Copilot workspace skills。

验收：

- managed block 仍能让平台找到 `eframe-instructions`、API index 和 `eframe-*` skills。
- manifest 更新，release check 通过。

## Phase C：瘦身 always-on instruction

目标：`eframe-instructions.md` 只保留稳定、短、始终需要生效的业务边界。

候选保留 full rules：

- `G05`：项目私有规则不进入同步文件。
- `G10`：提问/分析先只读。
- `P04/P05`：初始化和 Context。
- `U01/U02`：业务 UI 入口和内部边界。
- `R01/R06`：ResPath 和托管 Addressables。
- `E04`：YAML-first Unity 编辑。

候选下沉：

- UI 细节下沉 `skill.ui-feature` 和 UI checklist。
- 资源生命周期细节下沉 `skill.resource-flow`。
- 数据表细节下沉 `skill.data-table`。
- 目录细节下沉 `skill.directory-structure`。
- 审查细节下沉 `skill.guideline-audit`。

风险：

- always-on instruction 变薄后，AI 可能遗漏具体规则。

缓解：

- 使用 loading policy 和 managed block 强化按任务读取 skill。
- `eframe-instructions` 保留明确 handoff 列表。

验收：

- 文件明显变短。
- 高风险规则 `U02/E04/R06/G05` 没有弱化。
- loading simulation 仍成立。

## Phase D：收敛 API index

目标：`EFRAME_AI_API_INDEX.md` 只承担 API 查询和极短行为提示，不承载 workflow。

策略：

- 保留 API 表格和 source path。
- use/avoid note 控制在 API 语义附近。
- 不复制 skill workflow 步骤。
- 对多步骤行为改为指向对应 skill。

风险：

- API index 过薄可能失去“查 API 前的行为约束”。

缓解：

- 保留 `api-note` 渲染级别。
- 对高风险 API 保留短 avoid note，例如 `UIViewHandle`、Addressables strings、YAML fallback。

验收：

- API index 不再像 instruction 或 checklist。
- API index 中没有 release / manifest / maintainer-only 内容。

## Phase E：整理 specialized skills 和 references

目标：每个 skill 只拥有自己的 workflow；reference 拥有详细 checklist。

策略：

- `eframe-feature-bootstrap` 只保留总控和 handoff。
- `eframe-ui-feature` 拥有 UI workflow full text。
- `eframe-resource-flow` 拥有资源 workflow full text。
- `eframe-data-table` 拥有数据 workflow full text。
- `eframe-directory-structure` 拥有目录 ownership full text。
- `eframe-guideline-audit` 保持审查入口，详细项尽量放 reference。

风险：

- skill 间 handoff 太多，AI 可能漏读依赖。

缓解：

- loading policy 为常见场景明确 optional + loadWhen。
- 审查和模拟报告持续更新。

验收：

- 同一规则 full text 不在多个 skill 中重复出现。
- `Test-EFrameAIRuleArchitecture.ps1` 增加重复规则检查后通过。

## Phase F：考虑生成化

前置条件：

- registry / sets / views / loading policy 至少经过一轮人工迁移验证。
- 输出文件人工瘦身后稳定。
- release check 可以校验 registry 和输出之间的基本一致性。

可生成对象优先级：

1. managed block summaries。
2. handoff 列表。
3. checklist fragments。
4. 规则索引页。
5. 最后才考虑完整 instruction / skill 生成。

## 每次迁移的固定检查清单

1. 本次是否修改 `AIWorkspace~`？
2. 是否需要 manifest version bump？
3. manifest hash 是否更新？
4. 根 `CHANGELOG.md` 是否更新？
5. package `CHANGELOG.md` 是否更新？
6. `Test-EFrameAIRuleArchitecture.ps1` 是否通过？
7. `Test-EFrameAIRelease.ps1` 是否通过？
8. loading simulation 是否仍成立？
9. 高风险规则是否被弱化？
10. 是否误把 maintainer-only 规则同步给业务项目？

## 建议第一刀

从 Phase B 开始：瘦身 managed blocks。

原因：

- managed blocks 是平台输出，最适合验证“Platform Outputs 不做规则源”的原则。
- 它们文件短、风险相对可控。
- 它们天然应该只做路由和最小 baseline。
- 改完后能直接验证平台入口是否仍能指向 instruction / skills / API index。
