# EFrame Skill 触发指南

当你希望业务项目 AI 加载某个 EFrame workflow skill 时，可以直接使用下面这些短语。短语只是便捷触发词；正常描述需求也仍然有效。

| 触发短语 | Skill | 适用场景 |
| --- | --- | --- |
| `UI 自动验收` | `eframe-ai-loop-validation` | 通过 AI Loop 输入模拟做 PlayMode UI smoke 验收。 |
| `截图验收` | `eframe-ai-loop-validation` | 做 Game View 截图、UI 标注元素检查和坐标验证。 |
| `录制回放验收` | `eframe-ai-loop-validation` | 验证输入录制、回放和回放停止后的状态清理。 |
| `UI 接入` | `eframe-ui-feature` | 接入或调整 EFrame 页面、弹窗、View、Controller、绑定和 UI 层级。 |
| `页面接入` | `eframe-ui-feature` | 新增或重构页面/Panel 流程。 |
| `弹窗接入` | `eframe-ui-feature` | 新增或重构弹窗流程。 |
| `UI 重构` | `eframe-ui-feature` | 重构 UI prefab、Controller、生成 View 使用方式或生命周期绑定。 |
| `UI 规范` | `eframe-ui-feature` | 检查 UI 层级、Controller、绑定、prefab 和生命周期规则。 |
| `功能接入` | `eframe-feature-bootstrap` | 协调跨 Procedure、UI、数据、资源或模块的功能接入。 |
| `功能骨架` | `eframe-feature-bootstrap` | 规划最小合法 EFrame 功能骨架。 |
| `新功能规划` | `eframe-feature-bootstrap` | 实现前判断功能边界和需要委派的 EFrame 关注点。 |
| `流程接入` | `eframe-feature-bootstrap` | 新增或调整 Procedure 级功能流程。 |
| `模块接入` | `eframe-feature-bootstrap` | 新增或判断模块自有功能结构。 |
| `数据表接入` | `eframe-data-table` | 新增或重构持久化 `DataTable<TData>`。 |
| `存档接入` | `eframe-data-table` | 接入保存/加载、dirty tracking 或恢复处理。 |
| `设置数据` | `eframe-data-table` | 新增持久化选项/设置数据。 |
| `玩家进度` | `eframe-data-table` | 新增持久化玩家进度数据。 |
| `数据迁移` | `eframe-data-table` | 新增或审查数据 schema 版本迁移。 |
| `资源接入` | `eframe-resource-flow` | 接入资源、Addressables 和生成的 `ResPath.Generated` id。 |
| `资源加载` | `eframe-resource-flow` | 规划 load、preload、instantiate、pooling 和 release 归属。 |
| `ResPath 接入` | `eframe-resource-flow` | 对齐运行时资源 id 和生成的 ResPath 流程。 |
| `Addressables 接入` | `eframe-resource-flow` | 同步托管资源目录、组、地址和 label。 |
| `资源释放` | `eframe-resource-flow` | 审查 asset handle、实例和生命周期清理归属。 |
| `目录归位` | `eframe-directory-structure` | 判断文件或资源应该放在哪里。 |
| `文件放哪` | `eframe-directory-structure` | 创建文件前选择 App 或 Module 路径。 |
| `模块归属` | `eframe-directory-structure` | 判断模块私有还是 App 共享归属。 |
| `目录审查` | `eframe-directory-structure` | 审查文件放置、生成代码目录和资源目录边界。 |
| `资源放哪` | `eframe-directory-structure` | 在 Addressables 工作前先判断资源 owner 和目录。 |
| `规范审查` | `eframe-guideline-audit` | 审查已实现改动是否符合 EFrame 规范。 |
| `EFrame 审查` | `eframe-guideline-audit` | 审查 runtime、UI、资源、数据、目录或 AI 同步使用方式。 |
| `接入审查` | `eframe-guideline-audit` | 审查新功能接入是否符合 EFrame 边界。 |
| `合规检查` | `eframe-guideline-audit` | 检查规范违规和缺失验证。 |
| `改动体检` | `eframe-guideline-audit` | 汇总本地改动集的风险。 |

宽泛功能开发优先用 `功能接入`。已完成的改动集优先用 `规范审查`。需要可见 PlayMode 验收时，使用 AI Loop 相关触发短语。
