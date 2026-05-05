# EFrame AI 规则可视化审查

这份文档是维护者专用的审查入口，用来在修改同步 AI 文件前，先用图形方式检查规则信息架构。

按需加载模拟结果见 [AI_RULE_LOADING_SIMULATION.md](./AI_RULE_LOADING_SIMULATION.md)。
同步输出迁移顺序见 [AI_RULE_OUTPUT_MIGRATION_PLAN.md](./AI_RULE_OUTPUT_MIGRATION_PLAN.md)。

## 架构图

```mermaid
flowchart LR
    Registry["规则注册表<br/>rules/registry.yaml<br/>规则的唯一结构化记录"]
    Sets["规则集合<br/>rules/rule-sets.yaml<br/>按领域和工作流分组"]
    Views["规则视图<br/>rules/rule-views.yaml<br/>按用途选择规则"]
    Loading["加载策略<br/>rules/loading-policy.yaml<br/>按任务限制上下文"]
    Outputs["平台输出<br/>Codex / Copilot / Claude Code<br/>skills / instructions / docs"]
    Checks["校验<br/>重复 ID<br/>缺失引用<br/>受众越界"]

    Registry --> Sets
    Sets --> Views
    Views --> Loading
    Loading --> Outputs
    Registry --> Checks
    Sets --> Checks
    Views --> Checks
    Loading --> Checks
```

审查问题：每一层是否只有一个明确职责？下游层是否重新定义了本应属于 registry 的规则？

## 领域图

```mermaid
mindmap
  root((EFrame AI Rules))
    G 治理
      G01 AIWorkspace 规则源
      G03 维护者专用边界
      G05 项目私有规则
      G06 Manifest 同步标记
    P 启动
      P01 Procedure 状态机
      P04 先完成初始化
      P05 注入 Context
      P07 UI overlay 相机
    U UI
      U01 EFrame.UI 入口
      U02 内部 UI 边界
      U05 CurrentView
      U06 Prefab-first 静态 UI
    R 资源
      R01 ResPath.Generated
      R04 Handle 释放
      R06 托管 Addressables
      R08 构建预检
    D 目录
      D01 App vs Module
      D03 UI prefab 分类
      D06 命名例外
    T 数据
      T01 DataTable 形态
      T03 表内 mutation
      T05 结构化 load/save 结果
      T07 scoped 事件
    E Editor
      E01 Editor 程序集边界
      E04 YAML-first 编辑
      E05 Unity 编译验证
    W 工作流
      W01 Feature bootstrap
      W02 Guideline audit
      W04 Release flow
```

审查问题：这些领域划分是否正确？在打磨具体文案前，有没有规则应该移动到其他领域？

## 规则集合图

```mermaid
flowchart TB
    subgraph BusinessSets["业务规则集合"]
        Entry["governance.business-entry"]
        Startup["startup.procedure-lifecycle"]
        UI["ui.business-contract"]
        Res["resources.asset-flow"]
        Dir["directory.ownership"]
        Data["data.persistence"]
        Editor["editor.unity-serialization"]
    end

    subgraph WorkflowSets["工作流规则集合"]
        Feature["workflow.feature-bootstrap"]
        Audit["workflow.guideline-audit"]
    end

    subgraph MaintainerSets["维护规则集合"]
        Sync["governance.sync-boundary"]
        Release["maintainer.release-governance"]
    end

    subgraph PlatformSets["平台输出输入集合"]
        Codex["platform.codex"]
        Copilot["platform.copilot"]
        Claude["platform.claude-code"]
    end

    Entry --> Codex
    Entry --> Copilot
    Entry --> Claude
    Startup --> Feature
    UI --> Feature
    Res --> Feature
    Dir --> Feature
    Data --> Feature
    Feature --> Audit
    Sync --> Release
```

审查问题：这些集合是否符合维护者理解 EFrame 工作的方式？有没有集合太大、太小，或应该拆分？

## 视图到输出图

```mermaid
flowchart LR
    subgraph Views["规则视图"]
        Always["instruction.eframe-always-on"]
        Api["support-doc.api-index"]
        Feature["skill.feature-bootstrap"]
        UI["skill.ui-feature"]
        Res["skill.resource-flow"]
        Data["skill.data-table"]
        Dir["skill.directory-structure"]
        Audit["skill.guideline-audit"]
        Codex["managed-block.codex"]
        Copilot["managed-block.copilot"]
        Claude["managed-block.claude-code"]
        Release["maintainer.release-checklist"]
    end

    subgraph Outputs["当前输出文件"]
        Instr["AIWorkspace~/instructions/eframe-instructions.md"]
        Index["AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md"]
        Skills["AIWorkspace~/skills/eframe-*"]
        Blocks["AIWorkspace~/managed-blocks/eframe-*.md"]
        Maint["tools/MaintainerAIWorkspace/*.md"]
    end

    Always --> Instr
    Api --> Index
    Feature --> Skills
    UI --> Skills
    Res --> Skills
    Data --> Skills
    Dir --> Skills
    Audit --> Skills
    Codex --> Blocks
    Copilot --> Blocks
    Claude --> Blocks
    Release --> Maint
```

审查问题：有没有视图包含了过多规则？是否应该从 `summary` 或 `full` 改成 `handoff`？

## 高风险规则路径

```mermaid
flowchart TD
    U02["U02 UI 内部边界"]
    U01["U01 业务 UI 入口"]
    U05["U05 CurrentView 访问"]
    E04["E04 YAML-first Unity 编辑"]
    R06["R06 托管 Addressables 归属"]
    G05["G05 项目私有规则不进入同步文件"]

    U01 --> U02
    U02 --> U05
    R06 --> R07["R07 托管资源自动化"]
    R07 --> R08["R08 构建预检"]
    G05 --> D06["D06 命名和目录例外"]
    E04 --> E05["E05 Unity 编译验证"]
```

优先审查这些规则，因为它们最直接影响 AI 行为和 drift 风险：

| 规则 | 审查重点 |
| --- | --- |
| `U02` | 业务文件不能暗示普通功能代码应该直接持有 `QUI`、`IUIService` 或 `UIViewHandle`。 |
| `U05` | 确认 `CurrentView` 是业务侧唯一推荐的生成 View 访问方式。 |
| `E04` | 确认 YAML-first 编辑口径正确，Editor API 仍然只是 fallback。 |
| `R06` | 确认托管 Addressables 归属边界足够严格。 |
| `G05` | 确认项目私有规则绝不应该写入同步得到的 `eframe-*` 文件。 |

## 建议审查流程

```mermaid
flowchart TD
    Start["开始"]
    IA["审查信息架构"]
    Sets["审查规则集合"]
    Views["审查规则视图"]
    Registry["按领域审查 registry"]
    Risks["审查高风险规则"]
    Feedback["按规则 ID 写反馈"]
    Done["结构通过后再打磨文案"]

    Start --> IA
    IA --> Sets
    Sets --> Views
    Views --> Registry
    Registry --> Risks
    Risks --> Feedback
    Feedback --> Done
```

建议用短反馈，直接按规则 ID 或视图 ID 写：

```text
U02: canonical text 需要更强硬。
E04: OK。
R06/R07: 归属和自动化需要拆得更清楚。
instruction.eframe-always-on: fullRules 太多。
platform.codex: 应该包含 E04 summary。
```
