# EFrame 维护者 README

[返回使用者 README](README.md) | [English](README.maintainer.en.md)

这份文档面向 EFrame 框架维护者，集中放置仓库维护、模板同步、AI 契约、发布检查和版本边界。业务项目使用者默认阅读 [README.md](README.md)。

## 维护者需要知道的仓库结构

- [packages/com.eframework.core/AIWorkspace~](packages/com.eframework.core/AIWorkspace~)：随 package 发布的 AI 协作源，包含 `eframe-*` instructions/skills、managed blocks、manifest 和 AI 支持文档。
- [packages/com.eframework.core/Documentation~/maintainer](packages/com.eframework.core/Documentation~/maintainer)：给人看的维护者文档。
- [test-fixtures](test-fixtures)：用于 Basic 模板、Showcase 模块和 package 消费路径验证的 Unity fixture。
- [tools](tools)：框架仓库维护脚本和 package 脚本包装器。业务项目优先使用 package 内的 `Tools~/`。
- [tools/MaintainerAIWorkspace](tools/MaintainerAIWorkspace)：框架维护专用 AI workspace，不同步到业务项目。
- [AGENTS.md](AGENTS.md)、[CLAUDE.md](CLAUDE.md)、[.github/copilot-instructions.md](.github/copilot-instructions.md)：框架仓库内 AI 客户端入口，只做分流，不复制业务规则全文。

## 标准维护流程

每次维护先判断影响面，再决定要同步哪些契约：

1. 确认改动类型：bug fix、文档修正、模板调整、runtime/editor 行为变更、AI 同步层变更或正式 release。
2. 修改实现、模板或文档后，检查是否改变业务项目推荐写法。如果改变，必须同步维护 `packages/com.eframework.core/AIWorkspace~/` 下对应 instruction、skill 或 support-doc。
3. 如果改动触碰 Basic、Showcase、AI 同步、冷启动、资源目录、UI runtime contract、manifest 或 release 规则，按 [EFRAME_AI_RELEASE_CHECKLIST.md](tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md) 补齐检查。
4. 如果改动会影响业务项目 AI 的触发方式，按 [EFRAME_AI_PROMPT_ROUTING_AUDIT.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_PROMPT_ROUTING_AUDIT.md) 做提示词路由回归。
5. 更新根目录 [CHANGELOG.md](CHANGELOG.md)。如果 package 代码或 package 内文档有变化，也更新 [packages/com.eframework.core/CHANGELOG.md](packages/com.eframework.core/CHANGELOG.md)。
6. 运行与影响面匹配的检查脚本，并把验证结论记录到 PR 或 release 说明里。

常用检查命令：

```powershell
.\tools\Test-EFrameAIRelease.ps1
.\tools\Test-EFrameConsumer.ps1
.\tools\Sync-EFrameBasicTemplate.ps1 -CheckOnly
.\tools\Sync-EFrameShowcaseTemplate.ps1 -CheckOnly
```

## Bug Patch 流程

适用于修复已经发布版本中的缺陷，且不引入破坏性 API 变化。

1. 复现问题，并记录触发条件、受影响 package、Unity 版本和业务项目表现。
2. 做最小修复，避免顺手重构无关模块。
3. 如果 bug 暴露了文档或 AI 规则缺口，同步更新用户文档、`eframe-*` instruction/skill 或 API index；维护专用说明只放在 maintainer 文档或 `maintainer-*` skill。
4. 补充或更新能覆盖该问题的测试、fixture 检查或手动验证步骤。
5. 更新 changelog。对已发布稳定版本的缺陷修复，通常递增 patch 版本，例如 `0.7.5 -> 0.7.6`。
6. 按影响面运行检查。至少运行 release checklist 要求的相关脚本；如果修复影响 package 消费路径，运行 `Test-EFrameConsumer.ps1`。
7. 需要对外提供修复包时，先走预览版验证，再提交正式发布。

## 预览版验证流程

预览版用于让真实业务项目先验证即将发布的 package。它不是正式 release tag。

1. 确认待验证版本号、修复范围、风险点和必须通过的业务项目场景。
2. 创建不可变的验证入口。优先使用 `preview/<package>-<version>-rc.N` 分支，也可以记录具体 commit SHA。
3. 在真实业务项目中通过 Unity Package Manager Git URL 引入预览入口：

```text
https://github.com/ethanhubin/EFrame.git?path=/packages/com.eframework.core#preview/core-0.7.6-rc.1
https://github.com/ethanhubin/EFrame.git?path=/packages/com.eframework.core#<commit-sha>
```

4. 在业务项目中打开 Unity，完成 package import、脚本编译、初始化窗口、Basic 或受影响模块的场景验证。
5. 如果改动涉及 AI 同步，运行业务项目侧 updater：

```powershell
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -StatusOnly
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -Force
```

6. 记录预览验证结论，包括预览入口、验证项目、通过的检查、发现的问题和是否需要新的 `rc.N`。
7. 不要用正式 `vX.Y.Z` tag、`main` 分支或会移动的临时分支替代预览验证入口。

## 正式发布流程

正式发布前必须先完成预览版验证，并向维护者确认发布范围、版本号、tag、目标远端和验证结论。

1. 确认 `packages/com.eframework.core/package.json` 版本已递增，或明确本次不发布 package 版本。
2. 确认根目录 [CHANGELOG.md](CHANGELOG.md) 与 [packages/com.eframework.core/CHANGELOG.md](packages/com.eframework.core/CHANGELOG.md) 已记录本次变化。
3. 如果同步层 AI 文件、managed block、support-doc 或同步脚本发生变化，更新 `packages/com.eframework.core/AIWorkspace~/eframe-ai.manifest.json` 的版本、文件清单和 hash。
4. 运行发布阻塞检查：

```powershell
.\tools\Test-EFrameAIRelease.ps1
.\tools\Test-EFrameConsumer.ps1
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -StatusOnly
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

5. 按影响面追加 Basic、Showcase、Unity compile、AI project 或 PlayMode 验收。
6. 汇总待提交范围、版本号、正式 tag、预览验证结论和目标远端，等待维护者确认。
7. 确认后再提交 release commit、创建与 package 版本一致的正式 tag，例如 `v0.7.6`，并推送到确认的远端。
8. 发布后在业务项目执行一次同步或升级验证，确认正式 tag 的 Unity Package Manager 引用可用。

## Basic 与 Showcase 维护

维护 Basic 模板时，编辑 [test-fixtures/EFrameBasicTemplate](test-fixtures/EFrameBasicTemplate)，再同步回 package template：

```powershell
.\tools\Sync-EFrameBasicTemplate.ps1
.\tools\Sync-EFrameBasicTemplate.ps1 -CheckOnly
```

Extension Showcase 是可选业务模块，安装到 `Assets/Modules/EFrameExtensionShowcase/`，用于演示 UI Virtual List 和 Debug Console。维护 Showcase 时编辑 [test-fixtures/EFrameShowcaseUnity](test-fixtures/EFrameShowcaseUnity)，再同步回 package template：

```powershell
.\tools\Sync-EFrameShowcaseTemplate.ps1
.\tools\Sync-EFrameShowcaseTemplate.ps1 -CheckOnly
```

## AI 协作层维护边界

EFrame 的 AI 协作层是框架发布契约的一部分，不是额外文档包。

- 业务项目同步源位于 [packages/com.eframework.core/AIWorkspace~](packages/com.eframework.core/AIWorkspace~)。
- 同步到业务项目的框架托管文件使用 `eframe-*` 前缀。
- Codex、GitHub Copilot、Claude Code 的项目入口文件由业务项目拥有；EFrame 只更新入口文件中的 EFrame managed block。
- AI-facing 支持文档位于 `AIWorkspace~/support-docs/`，当前 API 索引为 [EFRAME_AI_API_INDEX.md](packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md)。
- 框架维护专用规则位于 [tools/MaintainerAIWorkspace](tools/MaintainerAIWorkspace)，不会同步到业务项目。

检查已同步项目：

```powershell
.\tools\Test-EFrameAIProject.ps1 -TargetRoot "D:\YourUnityProject" -FrameworkRoot "."
```

## 维护检查

发布前至少运行：

```powershell
.\tools\Test-EFrameAIRelease.ps1
```

涉及 Basic、Showcase、AI 同步、资源目录、启动流程、UI runtime contract 或 manifest 的变更，还应按影响面运行对应同步脚本和项目检查。

AI release 与 manifest 规则见 [tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md](tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md)。

## 维护者文档入口

- AI 架构说明：[EFRAME_AI_ARCHITECTURE.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_ARCHITECTURE.md)
- AI setup 说明：[EFRAME_AI_SETUP.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_SETUP.md)
- AI 提示词路由审查：[EFRAME_AI_PROMPT_ROUTING_AUDIT.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_PROMPT_ROUTING_AUDIT.md)
- AI 发布检查清单：[EFRAME_AI_RELEASE_CHECKLIST.md](tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md)
- 维护者 AI contract skill：[maintainer-ai-contract/SKILL.md](tools/MaintainerAIWorkspace/skills/maintainer-ai-contract/SKILL.md)

## 版本规则

EFrame 使用语义化版本。正式起始版本为 `0.1.0`，当前发布版本记录在 [packages/com.eframework.core/package.json](packages/com.eframework.core/package.json)，仓库级发布记录在 [CHANGELOG.md](CHANGELOG.md)。

Unity code、AI collaboration rules、bootstrap tools、sync scripts 和 docs 视为同一个 EFrame release surface。`AIWorkspace~/eframe-ai.manifest.json` 只是业务项目检测同步文件漂移的内部标记，不是单独产品版本。
