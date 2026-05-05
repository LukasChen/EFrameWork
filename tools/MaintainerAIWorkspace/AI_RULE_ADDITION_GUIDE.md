# EFrame AI 新增规则治理指南

这份文档是维护者专用指南，用来规范后续新增、修改、拆分或下线 AI 规则时的落点判断。它不作为业务项目同步源。

## 基本原则

1. 先判断是否已有规则能覆盖，再决定是否新增规则。
2. 新规则的规范性全文只写在 `rules/registry.yaml`。
3. 输出文件只按 rule view 渲染或引用规则，不成为新的规则源。
4. 业务项目同步文件只放业务项目 AI 需要执行的稳定规则、API note 或 workflow。
5. release、manifest、sync tooling、发布确认和维护者操作只留在 `tools/MaintainerAIWorkspace/`。

## 新增规则流程

1. 定义需求
   - 这条规则要阻止什么错误？
   - 失败后会造成行为 bug、资源 drift、发布风险，还是只是风格差异？
   - 它是长期规则，还是某次迁移说明？

2. 查重
   - 在 `rules/registry.yaml` 查是否已有规则覆盖。
   - 在 `rules/rule-sets.yaml` 查是否已有 domain/workflow 集合可复用。
   - 在 synced instruction / skill / support doc 中查是否已有重复口径。
   - 能更新已有规则时，不新增 ID。

3. 判断受众
   - `business-ai`：业务项目 AI 需要执行或避免。
   - `maintainer-ai`：框架维护 AI 需要执行。
   - `human-maintainer`：给维护者阅读，不直接作为 AI 同步契约。
   - `platform-adapter`：平台入口、激活、文件形态，不定义业务规则。

4. 判断内容类型
   - 长期稳定边界：registry rule，可 summary 到 instruction。
   - 多步骤 workflow：registry rule + 对应 skill。
   - 详细 checklist：registry rule + skill reference。
   - API 查询：API index 的 `api-note`。
   - 发布、manifest、sync：maintainer-only。
   - 项目私有差异：项目自有 instruction，不进入 EFrame 同步文件。

5. 更新结构层
   - 在 `rules/registry.yaml` 新增或修改 canonical rule。
   - 在 `rules/rule-sets.yaml` 放入合适集合。
   - 在 `rules/rule-views.yaml` 决定哪些输出能看到它，以及 render level。
   - 在 `rules/loading-policy.yaml` 决定什么时候加载对应 view。

6. 判断是否修改同步输出
   - 只有业务项目 AI 需要立即生效时，才修改 `packages/com.eframework.core/AIWorkspace~/`。
   - 修改同步输出时，必须更新 manifest version/hash、root changelog、package changelog，并运行 release check。
   - maintainer-only registry / review / architecture 文档变化不需要 manifest。

7. 验证
   - 运行 `tools/MaintainerAIWorkspace/Test-EFrameAIRuleArchitecture.ps1`。
   - 如改了 `AIWorkspace~`，运行 `tools/Test-EFrameAIRelease.ps1`。
   - 如改了生成、sync、release 工具，按 release checklist 补充对应验证。

## 放在哪里

| 情况 | 落点 |
| --- | --- |
| 业务 AI 必须常驻知道的高风险边界 | `rules/registry.yaml` + `instruction.eframe-always-on` summary/full |
| UI、资源、数据、目录等多步骤任务 | 对应 `eframe-*` skill |
| 某个 workflow 的详细审查项 | 对应 `references/*.md` |
| 框架 API 名称、source path、短 use/avoid note | `EFRAME_AI_API_INDEX.md` |
| 平台入口、激活、文件路由 | managed block / platform view |
| release、manifest、sync tooling、tag/push gate | `tools/MaintainerAIWorkspace/` |
| 人类解释、教程、背景说明 | `Documentation~` |
| 某个业务项目自己的例外 | 项目自有 instruction |

## Rule ID 规则

| Prefix | 领域 |
| --- | --- |
| `G` | governance / sync boundary |
| `P` | startup / Procedure / Context |
| `U` | UI business contract |
| `R` | resources / Addressables / ResPath |
| `D` | directory ownership |
| `T` | data tables / events |
| `E` | editor / Unity serialization / compile validation |
| `W` | workflow / audit / release routing |

规则 ID 一旦发布，不要复用给不同语义。废弃规则应标记 `deprecated` 或 `removed`，不要删除历史语义。

## Render Level 决策

| Level | 使用场景 |
| --- | --- |
| `full` | 该输出是规则主要执行入口，且需要完整语义 |
| `summary` | 该输出只需要短边界，不能引入新语义 |
| `checklist` | 审查或 reference 中转成问题 |
| `handoff` | 只指向 owning skill / view |
| `api-note` | API index 中的短 use/avoid note |
| `none` | 明确禁止出现在该输出 |

默认优先 `handoff` 或 `summary`。只有真正需要执行细节时才用 `full`。

## 新增规则审查清单

- 是否已有规则可以覆盖？
- 是否只有一个 canonical text？
- rule ID 是否属于正确领域？
- audience 是否正确？
- layer 是否正确？
- 是否需要加入 rule set？
- 哪些 rule view 应该看到它？
- render level 是否过重？
- loading policy 是否会导致上下文过载？
- 是否误把 maintainer-only 内容同步给业务项目？
- 如果改了 `AIWorkspace~`，manifest/hash/changelog 是否更新？
- warning check 是否出现 platform output 泄漏或 canonical text 原样复制？

## 示例：新增 UI 业务规则

场景：新增“业务 Controller 不应缓存跨打开周期 View 实例状态”。

处理：

1. 查 `U05/U06/U07` 是否已覆盖。
2. 如果只是 checklist 细化，更新 `eframe-ui-feature/references/ui-checklist.md`，不新增 rule。
3. 如果是新稳定边界，新增 `Uxx`，domain=`ui`，audience=`business-ai`。
4. 加入 `ui.business-contract`。
5. `skill.ui-feature` 可 `full`，`instruction.eframe-always-on` 通常只 `handoff`。
6. 如同步输出变化，更新 manifest/hash/changelog。

## 示例：新增 release 规则

场景：新增“发布前必须确认 tag 不存在”。

处理：

1. audience=`maintainer-ai`。
2. domain=`workflow` 或 `governance`，layer=`maintainer-only`。
3. 加入 `maintainer.release-governance`。
4. 只更新 `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md` 或 maintainer skill。
5. 不进入 `eframe-*` skills，不进入业务项目同步。

## 示例：新增 API note

场景：新增一个稳定 runtime API。

处理：

1. 如果只是 API 查询，更新 `EFRAME_AI_API_INDEX.md`。
2. 如果 API 自带行为约束，先确认 registry 是否已有对应规则。
3. API index 只写 source path 和短 note，不写 workflow。
4. 修改 support doc 属于同步输出变化，更新 manifest/hash/changelog。

## 禁止模式

- 为了某次需求直接在 managed block 里写业务细则。
- 在 API index 里写多步骤 workflow。
- 在 `eframe-feature-bootstrap` 里复制 UI/resource/data 详细规则。
- 在业务 `eframe-*` skill 里写 manifest、release、tag、push 流程。
- 为项目私有目录或命名例外修改同步 `eframe-*` 文件。
- 新增规则但不更新 rule sets / views / loading policy。
