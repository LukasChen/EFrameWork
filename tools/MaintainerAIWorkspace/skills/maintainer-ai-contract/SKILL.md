---
name: maintainer-ai-contract
description: 'Route and maintain EFrameWork AI contracts. Use inside the framework repository when deciding whether a change belongs in synced instructions, synced skills, managed blocks, support docs, maintainer-only docs, or the AI release/sync toolchain.'
argument-hint: 'Describe the framework, AI contract, sync, or maintainer workflow change to route and maintain.'
user-invocable: true
capabilities:
  - eframe.ai.contract
  - eframe.ai.sync
  - eframe.ai.release
  - eframe.framework.maintenance
owns:
  - AI contract layer placement
  - manifest and release check expectations
  - synced versus maintainer-only boundary
delegatesTo:
  - eframe-guideline-audit
outputs:
  - instruction versus skill placement decision
  - manifest and documentation update requirements
  - release-check applicability and blocking items
forbiddenPatterns:
  - syncing maintainer-only rules into business projects
  - placing manifest or release maintenance in eframe-* skills
  - publishing synced AI files without manifest and release-check review
  - turning synced instructions into long workflow checklists
---

# EFrame Maintainer AI Contract

## When To Use

- 修改 `.github/copilot-instructions.md`、`packages/com.eframework.core/AIWorkspace~/**`、`packages/com.eframework.core/Tools~/**` 或外层 `tools/` wrapper
- 修改 `Initialize-EFrameAI.ps1`、冷启动脚本、AI setup/release/architecture 文档
- 修改框架模板、目录规范、启动流程、UI 主链、资源路径或生成器行为，并影响业务项目应该如何使用 EFrame
- 审查一次框架重构是否需要同步 AI 协作层

## Decision Order

1. 先判断受众是谁：业务项目 AI、框架维护者，还是两者都需要参考。
2. 再判断内容类型：长期稳定边界、API 查询索引、工作流步骤、同步工具约束、发布检查，还是仅对某次维护有效的说明。
3. 只有当内容会影响业务项目对 EFrame 的生成、重构、审查或查询方式时，才进入同步层。
4. 只要变更涉及同步层、同步工具、冷启动或维护边界，就继续判断是否进入 release checklist 覆盖范围。

## Placement Rules

1. `eframe-*` instructions、skills、managed blocks 和 manifest 声明的支持文档会同步到业务项目，只放长期稳定的使用规则、查询入口或常用 workflow。
2. `maintainer-*` skills、release 文档、manifest 维护规则和脚本白名单约束只服务框架维护，不进入 AI sync whitelist。
3. 业务项目根 instruction 和本地 AI 规则归项目自己所有；框架同步只能更新 EFrame managed block、`eframe-*` 文件和 manifest 声明的框架托管支持文档。
4. 如果内容回答“业务项目应该怎样使用 EFrame”，放在 `eframe-*` instruction、skill、managed block 或 API/support-doc。
5. 如果内容回答“框架维护者怎样发布、同步、校验或维护 AI 契约”，放在 maintainer 层。

## Maintenance Workflow

1. 先判断变更影响面：runtime/editor 使用契约、skill 工作流、同步工具、冷启动模板、release 契约，或仅框架内部维护。
2. 决定承载位置：长期边界优先 instruction，查询型内容优先 support-doc，常用多步骤流程优先 synced skill，维护流程优先 maintainer skill 或 maintainer doc。
3. 保持 synced `eframe-*` skill 面向业务项目；不要把 manifest 版本递增、release checklist、同步脚本维护或框架内部维护步骤塞进去。
4. 需要详细审查时，优先扩展 maintainer skill 或 reference，而不是扩大每次都会触发的 instruction 上下文。
5. 进入发布或同步发布流程时，按 `tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md` 执行验证；release commit/tag 规则也以该清单为准。

## Instruction Quality Checks

- frontmatter 必须是合法 YAML；正文说明放在结束 `---` 之后。
- `applyTo` 应尽量精准，避免 runtime/editor/maintainer 指令互相抢触发。
- synced instructions 中避免过程口吻，例如 "temporary"、"cleanup"、"continue to"、"for now"。
- synced instructions 中避免过细实现项；业务项目 AI 只需要知道入口、边界、命名和禁止绕过的框架服务。
- 如果某条规则需要 5 步以上才能执行，优先写进 skill；如果只是长期禁止事项或入口边界，才留在 instruction。

## Skill Quality Checks

- synced `eframe-*` skill 应该对应业务项目常见 workflow，而不是框架维护专用流程。
- skill frontmatter 中的 `capabilities`、`owns`、`outputs`、`forbiddenPatterns` 要保持机器可读，避免空泛描述。
- skill 正文要说明适用场景、职责边界和需要委派给其他 skill 的内容，避免多个 skill 重复承载同一套规则。
- 如果某个 skill 只是转述 instruction，优先收回 instruction 或 support-doc，避免双份维护。

## Output Expectations

- 说明哪些规则属于 instructions，哪些属于 skills，哪些属于 maintainer-only。
- 指出本次是否进入 release checklist 覆盖的发布或同步发布流程。
- 如果发现 `eframe-*` skill 混入 maintainer 事项，建议移到 `maintainer-*` skill。
- 如果发现内容更适合 support-doc、managed block 或 maintainer doc，要直接指出替代落点。
- 如果 release check 未通过，先列出阻塞项，再给出修复建议。
