---
name: maintainer-ai-contract
description: 'Maintain EFrameWork AI contracts. Use inside the framework repository when changing .github instructions, skills, manifest, sync scripts, cold-start templates, setup docs, release docs, or framework templates that affect business-project AI guidance.'
argument-hint: 'Describe the framework or AI-layer change to maintain.'
user-invocable: true
---

# EFrame Maintainer AI Contract

## When To Use

- 修改 `.github/copilot-instructions.md`、`.github/instructions/**`、`.github/skills/**` 或 `.github/eframe-ai.manifest.json`
- 修改 `Initialize-EFrameAI.ps1`、冷启动脚本、项目 overlay 脚本、AI setup/release/architecture 文档
- 修改框架模板、目录规范、启动流程、UI 主链、资源路径或生成器行为，并影响业务项目应该如何使用 EFrame
- 审查一次框架重构是否需要同步 AI 协作层

## Boundary Rules

1. `eframe-*` instructions 和 skills 是会同步到业务项目的公共契约，只放长期稳定的使用规则。
2. `maintainer-*` instructions 和 skills 只服务框架仓库维护，不进入 AI sync whitelist。
3. `project-*` instructions 和 skills 属于业务项目 overlay，不应出现在框架仓库。
4. 如果规则只对框架内部迁移、release、manifest、脚本白名单或临时重构有意义，放在 maintainer 层。
5. 如果规则描述业务项目冷启动后仍要遵守的 Procedure、UI、资源、数据或 Editor 工具用法，放在 `eframe-*` 层。

## Maintenance Flow

1. 先判断变更影响面：runtime 使用契约、editor 使用契约、skill 工作流、同步工具、冷启动模板、release 文档，或仅框架内部维护。
2. 保持 instructions 短而稳定；把任务流程、审查清单和细节核对项放进 skill 或 skill reference。
3. 保持 synced `eframe-*` skill 面向业务项目；不要把 manifest bump、release checklist、同步脚本维护或框架内部迁移步骤塞进去。
4. 需要详细审查时，优先扩展 maintainer skill 或 reference，而不是扩大每次都会触发的 instruction 上下文。
5. 修改任何 framework-managed AI 文件后，递增 `.github/eframe-ai.manifest.json`，并更新 notes 说明真实意图。
6. 运行 `tools/Test-EFrameAIRelease.ps1`，确认 managed instruction/skill 边界、manifest bump、frontmatter、instruction 长度预算和 release 检查通过。

## Instruction Quality Checks

- frontmatter 必须是合法 YAML；正文说明放在结束 `---` 之后。
- `applyTo` 应尽量精准，避免 runtime/editor/maintainer 指令互相抢触发。
- synced instructions 中避免迁移期口吻，例如 "temporary"、"cleanup"、"continue to"、"for now"。
- synced instructions 中避免过细实现项；业务项目 AI 只需要知道入口、边界、命名和禁止绕过的框架服务。
- 如果某条规则需要 5 步以上才能执行，优先写进 skill；如果只是长期禁止事项或入口边界，才留在 instruction。

## Output Expectations

- 说明哪些规则属于 instructions，哪些属于 skills，哪些属于 maintainer-only。
- 指出是否需要 bump manifest、更新 setup/release/architecture 文档或同步脚本。
- 如果发现 `eframe-*` skill 混入 maintainer 事项，建议移到 `maintainer-*` skill。
- 如果 release check 未通过，先列出阻塞项，再给出修复建议。
