---
name: eframe-guideline-audit
description: 'Review Unity EFrame changes for compliance. Use when auditing startup scene, Procedure lifecycle, QUI usage, UIController structure, directory layout, naming, resource paths, or AI customization sync.'
argument-hint: 'Describe the change set or module to audit.'
user-invocable: true
capabilities:
  - eframe.audit.runtime
  - eframe.audit.ui
  - eframe.audit.resources
  - eframe.audit.data
owns:
  - EFrame guideline compliance review
  - runtime lifecycle risk detection
  - business-project AI contract drift triage
delegatesTo:
  - maintainer-ai-contract
outputs:
  - prioritized guideline findings
  - missing validation risks
  - maintainer-skill handoff when scope is framework AI release
forbiddenPatterns:
  - mixing maintainer-only release checks into business project audit conclusions
  - treating logs as data/load/save contract
  - approving unmanaged Addressables edits under managed resource directories
---

# EFrame Guideline Audit

## When To Use

- 审查新页面、新玩法、新流程是否符合 EFrame 规范
- 评估一个目录或提交是否继续扩散了过渡结构
- 检查业务项目是否正确使用已同步的 EFrame runtime/editor 契约

## Audit Flow

- Check runtime cameras that should host the framework UI overlay use `EFrameSceneCamera` instead of per-frame polling or manual UI camera stack code.

1. 找到启动链路入口，确认没有把业务逻辑塞回启动场景。
2. 检查 `Procedure` 是否只做状态编排，进入退出是否对称清理。
3. 检查 UI 是否经过 `QUI` 层级管理，控制器是否承担了正确职责。
4. 检查项目初始化链路是否会自动补齐 `QUI` 依赖的 Unity SortingLayer，运行时缺层级时是否有明确校验提示。
5. 检查 UIController 是否避免在构造阶段读取未初始化服务，缓存 UI 是否只复用 `QUIBinding`/GameObject 而不是复用一次性 View wrapper 状态。
6. 检查 `OnViewCreated()` / `OnViewDestroyed()` 是否只承担 View 实例级绑定与释放，`OnViewOpened()` / `OnViewClosed()` 是否承担每次打开关闭的刷新或暂停逻辑。
7. 检查 Addressables 运行时加载是否优先使用异步 `EFrame.Current.Assets` 入口和句柄释放，同步 `WaitForCompletion` 是否有明确理由。
8. 检查事件订阅是否通过作用域订阅或明确成对退订完成清理，数据表是否统一注册到 `EFrame.Current.Data`。
9. 检查数据表是否声明稳定 `StorageKey`，而不是把类名或命名空间变化暴露为持久化键名；同时确认不同表没有复用同一持久化键。
10. 检查数据表是否通过属性/方法封装修改并自动 dirty，复杂变更是否只在实际发生修改时标记 dirty，且避免外部直接修改底层数据对象。
11. 检查需要持久化升级的数据表是否声明 `CurrentVersion` / `Migrate(...)`，以及旧裸数据文件是否能按 `version 0` 进入迁移链。
12. 检查需要感知存档恢复或回退的业务是否读取 `LastLoadResult` 的结构化状态与 `ReasonCode`，而不是依赖运行日志文本或消息字符串做分支判断。
13. 检查需要感知保存成功、跳过或失败的业务是否读取 `LastSaveResult`，而不是默认认为每次 `Save()` 调用都实际写盘。
14. 检查资源和代码目录是否可映射，命名是否稳定。
15. 检查 `QUIBinding` 默认访问类命名空间与模板 prefab 是否保持一致，避免新生成代码继续落入旧的 `EFrameWork.UI.Generated`。
16. 检查业务代码是否绕过 `IUIService/QUI` 与 `UIViewHandle` 语义直接驱动 `BindingViewBase` 生命周期，或重新引入 `assetPath` View 构造函数 / 旧 `View` facade。

## AI Layer Audit

如果审查对象是 EFrameWork 框架仓库自身，并且变更涉及 `.github`、skills、manifest、冷启动、同步脚本、模板或 release 文档，改用 maintainer skill 做 AI 层审查；不要把框架维护检查混入业务项目审查结论。

## Output Expectations

- 先给出高风险问题和潜在回归点。
- 明确指出违反的是哪一类边界：启动场景、`Procedure`、UI、目录、命名、资源路径或 AI 配置同步。
- 如果发现问题属于框架维护而不是业务项目使用，明确建议切换到 maintainer skill。
- 如果没有问题，也要说明剩余风险，例如缺少验证、仍处于过渡目录、缺少模板沉淀等。

详细核查项见 [audit checklist](./references/audit-checklist.md)。
