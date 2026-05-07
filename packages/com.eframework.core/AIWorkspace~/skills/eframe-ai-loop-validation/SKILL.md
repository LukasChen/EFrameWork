---
name: eframe-ai-loop-validation
description: 'Validate EFrame UI or PlayMode behavior with com.eframework.ai-loop. Use when the task mentions UI 自动验收, 截图验收, 录制回放验收, AI Loop, Game View screenshots, annotated UI inspection, simulate-mouse-ui, simulate-keyboard, record-input, replay-input, or AI-assisted PlayMode smoke validation.'
argument-hint: 'Describe the PlayMode flow, target UI, input sequence, and expected result to validate.'
user-invocable: true
capabilities:
  - eframe.ai-loop.screenshot
  - eframe.ai-loop.ui-input
  - eframe.ai-loop.record-replay
  - eframe.playmode.validation
owns:
  - AI Loop PlayMode smoke validation workflow
  - screenshot and UI coordinate contract
  - cancelled input and replay cleanup checks
delegatesTo:
  - eframe-ui-feature
  - eframe-feature-bootstrap
outputs:
  - PlayMode validation steps and results
  - AI Loop screenshot/input/replay usage notes
  - residual validation risks
forbiddenPatterns:
  - treating AI Loop as a replacement for authored UI prefabs
  - using replay before scene and Procedure state are stable
  - validating legacy Input.GetKey paths with simulate-keyboard
  - skipping cleanup checks for held pointer or keyboard state
---

# EFrame AI Loop Validation

## When To Use

- 用户要求用 AI 看 Game View 或 Editor/Game 视图截图。
- 用户提到 `UI 自动验收`、`截图验收` 或 `录制回放验收`。
- 用户要求验证 UI 点击、长按、拖拽、键盘输入、录制或回放。
- 项目已安装 `com.eframework.ai-loop`，并需要 AI-assisted PlayMode smoke validation。
- 需要复现或审查 `screenshot`、`simulate-mouse-ui`、`simulate-keyboard`、`record-input`、`replay-input` 相关问题。

## Boundaries

- AI Loop 是 PlayMode smoke harness，不替代 UI prefab authoring、UI Binding 生成、controller 生命周期清理或人工可见验收。
- `simulate-keyboard` 只验证 Unity Input System 路径，不验证 legacy `Input.GetKey`。
- `simulate-mouse-ui` 通过 uGUI `EventSystem` / `ExecuteEvents` 派发事件，不移动 OS cursor。
- 录制/回放前先稳定 scene、Procedure 状态、帧时序和随机种子；否则 replay 只能证明输入被注入，不能证明玩法完全确定。

## Workflow

1. Confirm `com.eframework.ai-loop` is installed and PlayMode is active.
2. Capture Game View with annotation or `ElementsOnly` to identify interactable uGUI elements.
3. Treat agent-facing screenshot element coordinates and mouse request coordinates as Game View pixels with origin at top-left.
4. Use `simulate-mouse-ui` for uGUI click, long press, drag, and drop checks.
5. Use `simulate-keyboard` for Unity Input System key press, key down, and key up checks.
6. Use `record-input` / `replay-input` only after the flow can start from a stable state.
7. For flows that hold pointer or keyboard state, verify cancellation, `StopReplay`, and PlayMode exit cleanup.
8. Report what was validated, what was not validated, and any remaining manual visible-Editor checks.

## Output Checks

- Screenshot results include whether annotations align with visible UI and whether `ElementsOnly` coordinates can drive mouse simulation.
- Mouse UI results include whether pointer up, drop, and end drag are observed after cancel or stop paths.
- Keyboard results include whether held keys are released after cancellation and PlayMode exit.
- Record/replay results include the recording path, replay progress, stop behavior, and any nondeterministic scene assumptions.
- If the project is missing AI Loop, say so and fall back to the nearest available manual or Unity Test Framework validation path.
