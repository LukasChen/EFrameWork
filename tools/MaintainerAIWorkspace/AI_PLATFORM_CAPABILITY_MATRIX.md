# EFrame AI 平台能力矩阵

这份文档用于维护 EFrame AI 输出适配。它只描述平台入口、原生 skill 目录和同步策略，不定义业务规则。

## 当前目标平台

| 平台 | 根入口 | 原生 EFrame skill 输出 | 同步条件 | 说明 |
| --- | --- | --- | --- | --- |
| ChatGPT Codex / Codex | `AGENTS.md` managed block | `.agents/skills/eframe-*` | `-Clients codex` | Codex 使用 `AGENTS.md` 做项目入口，EFrame skills 同步到 Codex-native repository skill 目录；根入口不列举 skill 路由。 |
| VS Code GitHub Copilot | `.github/copilot-instructions.md` managed block | `.github/skills/eframe-*` | `-Clients copilot` | `.github/skills` 是 Copilot native skill 目录。 |
| Claude Code | `CLAUDE.md` managed block | `.claude/skills/eframe-*` | `-Clients claude-code` | Claude Code 使用 `.claude/skills` 作为 project skill 目录；根入口不列举 skill 路由。 |

## 共享输出

| 输出 | 路径 | 客户端 |
| --- | --- | --- |
| Always-on instruction | `.github/instructions/eframe-instructions.md` | all selected clients |
| API lookup index | `.github/eframe/EFRAME_AI_API_INDEX.md` | all selected clients |
| Sync manifest | `.github/eframe-ai.manifest.json` | all selected clients |

## 适配原则

1. `packages/com.eframework.core/AIWorkspace~/skills/eframe-*` 是唯一 skill 源。
2. 平台原生 skill 目录是输出路径，不是新的规则源。
3. 每个平台的根入口只做 EFrame baseline 和维护边界，不复制 workflow 细节，也不列举 native skill 路径。
4. 新增平台时先补本矩阵，再补 `rule-views.yaml`、manifest、sync script 和 release check。
5. 如果某个平台没有稳定的 repository-native skill 目录，先只提供 managed block 路由，不要伪装成 native skill。
