# EFrame AI 契约重构合并说明

这份文档用于合并 `codex/ai-contract-refactor-plan` 前的人工审查。它是 maintainer-only 文档，不同步到业务项目。

## 分支目标

本分支把 EFrame AI 规范从“分散 markdown 文案”整理为“结构化规则 + 分层引用 + 平台输出”的信息架构，目标是：

- 每条规则只有一个 canonical definition。
- 输出文件只渲染、摘要或 handoff，不重新定义规则。
- 业务 AI 按任务加载最小上下文，减少过载和规则污染。
- 维护者 release / manifest / sync 规则不进入业务 `eframe-*` skills。
- 后续新增规则有明确治理流程。

## 最终版本

| 项目 | 合并前 main | 本分支最终 |
| --- | --- | --- |
| Package version | `0.6.16` | `0.6.22` |
| AI manifest version | `0.4.33` | `0.4.39` |
| 分支 | `main` | `codex/ai-contract-refactor-plan` |

## 同步输出变更

| 版本 | Manifest | 范围 |
| --- | --- | --- |
| `0.6.17` | `0.4.34` | Unity serialized asset editing 改为 YAML-first / fallback-only |
| `0.6.18` | `0.4.35` | 三个平台 managed blocks 瘦身为路由 baseline |
| `0.6.19` | `0.4.36` | `eframe-instructions.md` 瘦身为 always-on 稳定边界 |
| `0.6.20` | `0.4.37` | `EFRAME_AI_API_INDEX.md` 收敛为 API lookup 和短 note |
| `0.6.21` | `0.4.38` | coordinator / audit skill 分流化，细节回到 references |
| `0.6.22` | `0.4.39` | 修复 Unity asset `applyTo` 和 UI 入口 forbidden wording |

业务可见结果：

- `AGENTS.md` / Copilot / Claude Code managed blocks 只做入口路由。
- Always-on instruction 覆盖 `.cs`、`.prefab`、`.unity`、`.asset`。
- Unity YAML 编辑口径为业务 AI 优先直接编辑 YAML，Editor API 只作为异常 fallback。
- UI 业务入口统一为 `EFrame.UI` + `UIControllerBase<TGeneratedView>`。
- `QUI`、`IUIService`、`UIViewHandle` 明确不是普通业务入口。
- `ResPath.Generated` 和托管 Addressables 边界保留为常驻高风险规则。

## Maintainer-only 新增内容

| 文件 | 作用 |
| --- | --- |
| `AI_CONTRACT_REFACTOR_PLAN.md` | 总体重构计划 |
| `AI_RULE_INFORMATION_ARCHITECTURE.md` | 规则信息架构 |
| `AI_RULE_INVENTORY.md` | 规则盘点桥接文档 |
| `AI_RULE_VISUAL_REVIEW.md` | Mermaid 可视化审查入口 |
| `AI_RULE_LOADING_SIMULATION.md` | 按需加载模拟 |
| `AI_RULE_OUTPUT_MIGRATION_PLAN.md` | 输出迁移 A-F 计划 |
| `AI_RULE_REVIEW_SUMMARY.md` | 人工审查摘要 |
| `AI_RULE_ADDITION_GUIDE.md` | 后续新增规则治理流程 |
| `rules/registry.yaml` | 结构化规则草案 |
| `rules/rule-sets.yaml` | 规则集合 |
| `rules/rule-views.yaml` | 输出视图 |
| `rules/loading-policy.yaml` | 按任务加载策略 |
| `Test-EFrameAIRuleArchitecture.ps1` | 结构引用校验和 warning 原型 |

## 人工确认状态

已确认：

- `G05`：项目私有规则不能写入同步 `eframe-*` 文件。
- `U02`：明确禁止普通业务代码直接持有 `QUI/IUIService/UIViewHandle`。
- `U05`：`CurrentView` 是业务侧唯一推荐的 generated View 访问方式。
- `E04`：YAML-first / fallback-only 口径确认。
- `R06`：托管 Addressables 归 EFrame editor automation 维护。

`U02` 和 `U05` 已从 `candidate` 转为 `stable`。

## 验证命令

合并前必须通过：

```powershell
./tools/MaintainerAIWorkspace/Test-EFrameAIRuleArchitecture.ps1
./tools/Test-EFrameAIRelease.ps1
git diff --check
git diff main...HEAD --stat
git log main..HEAD --oneline
```

当前验证结果：

- Rule architecture check：通过。
- AI release check：通过。
- `git diff --check`：通过。
- `Test-EFrameAIRuleArchitecture.ps1` warning 原型当前无噪音 warning。

## 已知风险

1. 输出 markdown 仍是人工维护，没有生成器保证 registry 与输出完全一致。
2. `workflow.guideline-audit` 是最宽业务集合，后续需要防止膨胀成全规则入口。
3. `feature-bootstrap` 仍有较多 optional handoff，后续要防止重新复制 specialized rules。
4. 重复 full text 检查目前只是 warning 原型，只能发现原样复制，不能替代人工语义审查。
5. 这次合并会一次性引入多个 AI contract 版本递增；合并前应确认接受 `0.6.22 / 0.4.39` 作为落点。

## 合并后建议

1. 先不要立刻进入完整 Phase F 生成化。
2. 等一次实际业务项目同步和 AI 使用反馈后，再小修规则内容。
3. 如要生成化，先选择 managed block summary 或 handoff list 作为最小试点。
4. 任何后续新增规则先读 `AI_RULE_ADDITION_GUIDE.md`，再决定 registry / set / view / loading / output 落点。
5. 如合并到 `main` 并准备正式发布，应按 release checklist 再确认 tag、push、发布口径。

## 合并决策

建议合并条件：

- 维护者接受同步输出最终版本 `0.6.22`。
- 维护者接受 manifest 最终版本 `0.4.39`。
- 维护者接受本分支新增 maintainer-only 规则架构作为后续规则治理基线。
- 最终验证命令全部通过。
