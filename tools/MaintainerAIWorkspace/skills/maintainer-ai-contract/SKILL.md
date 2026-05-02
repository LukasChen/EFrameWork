---
name: maintainer-ai-contract
description: 'Maintain EFrameWork AI contracts. Use inside the framework repository when changing .github instructions, skills, manifest, sync scripts, cold-start templates, setup docs, release docs, or framework templates that affect business-project AI guidance.'
argument-hint: 'Describe the framework or AI-layer change to maintain.'
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
  - release-check blocking items and fixes
forbiddenPatterns:
  - syncing maintainer-only rules into business projects
  - placing manifest or release maintenance in eframe-* skills
  - publishing synced AI files without manifest and release-check review
---

# EFrame Maintainer AI Contract

## When To Use

- 修改 `.github/copilot-instructions.md`、`packages/com.eframework.core/AIWorkspace~/**`、`packages/com.eframework.core/Tools~/**` 或外层 `tools/` wrapper
- 修改 `Initialize-EFrameAI.ps1`、冷启动脚本、AI setup/release/architecture 文档
- 修改框架模板、目录规范、启动流程、UI 主链、资源路径或生成器行为，并影响业务项目应该如何使用 EFrame
- 审查一次框架重构是否需要同步 AI 协作层

## Boundary Rules

1. `eframe-*` instructions/skills 和 manifest 声明的支持文档会同步到业务项目，只放长期稳定的使用规则或查询索引。
2. `maintainer-*` skills 只服务框架仓库维护，不进入 AI sync whitelist。
3. 业务项目根 instruction 和本地 AI 规则归项目自己所有；框架同步只能更新 EFrame managed block、`eframe-*` 文件和 manifest 声明的框架托管支持文档。
4. 如果规则只对框架内部维护、release、manifest 或脚本白名单有意义，放在 maintainer 层。
5. 如果规则描述业务项目需要遵守的 Procedure、UI、资源、数据或 Editor 工具用法，放在 `eframe-*` 层或对应 managed block。

## Maintenance Flow

1. 先判断变更影响面：runtime 使用契约、editor 使用契约、skill 工作流、同步工具、冷启动模板、release 文档，或仅框架内部维护。
2. 保持 instructions 短而稳定；把任务流程、审查清单和细节核对项放进 skill 或 skill reference。
3. 保持 synced `eframe-*` skill 面向业务项目；不要把 manifest 版本递增、release checklist、同步脚本维护或框架内部维护步骤塞进去。
4. 需要详细审查时，优先扩展 maintainer skill 或 reference，而不是扩大每次都会触发的 instruction 上下文。
5. manifest、release check、commit 和 tag 要求以 `docs/maintainer/EFRAME_AI_RELEASE_CHECKLIST.md` 为准。
6. 进入发布或同步发布流程时，按 release checklist 执行验证。
7. Release handling must include git commit and tag: after version, changelog, manifest, and release checks are ready, commit the release changes and create a version tag matching `packages/com.eframework.core/package.json`, such as `v0.2.2`. Only skip commit/tag when the user explicitly asks to prepare release files only.

## Instruction Quality Checks

- frontmatter 必须是合法 YAML；正文说明放在结束 `---` 之后。
- `applyTo` 应尽量精准，避免 runtime/editor/maintainer 指令互相抢触发。
- synced instructions 中避免过程口吻，例如 "temporary"、"cleanup"、"continue to"、"for now"。
- synced instructions 中避免过细实现项；业务项目 AI 只需要知道入口、边界、命名和禁止绕过的框架服务。
- 如果某条规则需要 5 步以上才能执行，优先写进 skill；如果只是长期禁止事项或入口边界，才留在 instruction。

## Output Expectations

- 说明哪些规则属于 instructions，哪些属于 skills，哪些属于 maintainer-only。
- 指出本次是否进入 release checklist 覆盖的发布或同步发布流程。
- 如果发现 `eframe-*` skill 混入 maintainer 事项，建议移到 `maintainer-*` skill。
- 如果 release check 未通过，先列出阻塞项，再给出修复建议。
