# EFrame AI 规则整理审查摘要

这份文档是维护者人工审查入口，用来快速确认 A-E 输出迁移后的 AI 规则结构是否值得进入下一阶段。它不作为业务项目同步源。

## 当前结论

当前整理已经达到“先搭稳信息架构，再完善规则内容”的阶段目标：

- 每条规则已有结构化 registry 草案。
- rule sets / rule views / loading policy 已建立分层引用。
- managed blocks、always-on instruction、API index、skills 和 references 已按职责瘦身。
- 常见任务的按需加载模拟成立。
- 维护者 release / manifest 规则未进入业务 `eframe-*` skills。

不建议立刻进入完整生成化。下一步应先做人工审查和少量验证增强，确认规则 ID、候选规则状态和输出边界稳定。

## 审查入口

建议按这个顺序审查：

1. `AI_RULE_REVIEW_SUMMARY.md`：看总体结论、风险和检查项。
2. `AI_RULE_VISUAL_REVIEW.md`：看架构图、领域图和高风险规则路径。
3. `AI_RULE_LOADING_SIMULATION.md`：看按需加载模拟。
4. `AI_RULE_OUTPUT_MIGRATION_PLAN.md`：看 A-E 迁移范围和 Phase F 前置条件。
5. `rules/registry.yaml`：只在需要逐条规则反馈时打开。

## 已完成范围

| Phase | 版本 | Manifest | 结果 |
| --- | --- | --- | --- |
| A：显式映射 | maintainer-only | 无 | `rule-views.yaml` 和迁移计划建立输出映射 |
| B：managed blocks | `0.6.18` | `0.4.35` | 平台入口只保留路由、baseline 和 handoff |
| C：always-on instruction | `0.6.19` | `0.4.36` | 常驻规则收敛到最小稳定边界和 task handoff |
| D：API index | `0.6.20` | `0.4.37` | API index 收敛为 lookup 表和短行为提示 |
| E：skills / references | `0.6.21` | `0.4.38` | coordinator/audit skill 分流化，详细 checklist 回到 references |
| 自审修复 | `0.6.22` | `0.4.39` | 修复 Unity asset `applyTo` 和 UI 入口措辞 |

## 同步输出职责

| 输出 | 当前职责 | 不应承载 |
| --- | --- | --- |
| Managed blocks | 平台入口、文件路由、最小 baseline | 详细 UI / resource / data / release 规则 |
| `eframe-instructions.md` | 必须常驻的稳定边界和 task handoff | 多步骤 workflow、长 checklist |
| `EFRAME_AI_API_INDEX.md` | API lookup、source path、短 use/avoid note | 工作流步骤、发布规则 |
| `eframe-feature-bootstrap` | 总控 workflow、任务分类、specialized skill handoff | UI/resource/data 细节规则 |
| `eframe-guideline-audit` | 业务审查路由和结果格式 | framework release / manifest 结论 |
| Specialized skills | 各领域 workflow | 维护者发布流程 |
| References | 详细 checklist | 平台激活和 release gate |

## 已修复问题

| 问题 | 处理 |
| --- | --- |
| Managed blocks 重复展开业务规则 | 改为平台路由 baseline |
| Always-on instruction 过长 | 下沉 UI、resource、data、directory、audit 细节 |
| API index 像 workflow 文档 | 收敛为 lookup 和短 note |
| Feature bootstrap 复制 specialized skill 细节 | 改为 coordinator |
| Guideline audit 容易变成全规则入口 | 改为按发现类型 handoff |
| Unity YAML-first 规则未覆盖 asset 文件触发 | `applyTo` 增加 `.prefab`、`.unity`、`.asset` |
| `QUI` 被误写成业务 UI bypass 入口 | 改为 `EFrame.UI/UIControllerBase` |

## 高风险规则审查点

| 规则 | 当前状态 | 审查问题 |
| --- | --- | --- |
| `G05` | stable | 项目私有规则是否只放项目自有 instruction，不进入同步 `eframe-*` 文件 |
| `U02` | candidate | 是否足够明确地禁止普通业务代码直接持有 `QUI`、`IUIService`、`UIViewHandle` |
| `U05` | candidate | `CurrentView` 是否已经成为业务侧唯一推荐的 generated View 访问方式 |
| `R06` | stable | 托管 Addressables 是否明确归 EFrame editor automation 维护 |
| `E04` | stable | YAML-first 是否足够强，Editor API 是否只作为异常 fallback |

## 按需加载结论

当前 loading policy 仍成立：

- 普通解释：只加载 always-on，必要时 API index。
- UI / resource / data / directory：只加载对应 specialized skill，跨域时按条件加载。
- 审查：加载 audit，再按发现类型打开 specialized skill。
- 维护 AI 合约或 release：才加载 maintainer skill 和 release checklist。
- 直接编辑 Unity prefab / scene / asset：asset `applyTo` 会触发 always-on，拿到 YAML-first 口径。

## 验证状态

最近验证通过：

- `tools/MaintainerAIWorkspace/Test-EFrameAIRuleArchitecture.ps1`
- `tools/Test-EFrameAIRelease.ps1`
- `git diff --check`

Release check 当前显示无未提交 AI-impacting synced 文件变更。

`Test-EFrameAIRuleArchitecture.ps1` 已增加保守 warning 原型：

- managed block 中出现过细业务/API 术语时提示。
- API index 出现 workflow/checklist 型结构时提示。
- registry `canonicalText` 被原样复制到平台输出时提示。

这些 warning 只用于人工审查，不阻塞 release。

## 剩余风险

1. `U02` 和 `U05` 仍是 candidate，需要人工确认 UI 口径后再考虑 stable。
2. `workflow.guideline-audit` 是最宽的业务集合，后续容易膨胀成全规则入口。
3. `feature-bootstrap` 的 optional skill 较多，需要继续防止它重新复制 specialized rules。
4. 目前输出仍是人工维护，没有生成器保证 registry 与 markdown 完全一致。
5. 重复 full text 检查仍是 warning 原型，只能发现原样复制，不能替代人工审查语义重复。

## 建议审查清单

- `managed-block.*` 是否只做平台路由，不描述业务细节？
- `instruction.eframe-always-on` 是否只保留必须常驻的高风险边界？
- `support-doc.api-index` 是否没有 workflow 步骤和 maintainer release 内容？
- 每个 `skill.*` 是否只拥有自己的 workflow？
- references 是否只承载 checklist，不承担平台入口？
- `E04` 是否在 instruction 和 API index 中保持 YAML-first / fallback-only 一致？
- `U02` 是否没有把 `QUI/IUIService/UIViewHandle` 写成普通业务入口？
- `G05` 是否阻止项目私有规则写进同步文件？
- loading simulation 是否覆盖你的核心业务场景？

## 下一步建议

短期：

1. 人工审查 `U02`、`U05`、`E04`、`R06`、`G05`。
2. 按审查反馈只做小修，不开始生成化。
3. 根据 warning 结果决定是否扩大重复 full text 检查范围。

中期：

1. 选择 managed block summary 或 handoff list 做轻量生成化试点。
2. 生成结果必须可读、可审查、可手工应急修改。
3. 生成化稳定后，再考虑 instruction / skill fragment。
