# EFrame AI Loop Manual Test Checklist

This checklist is the acceptance pass for `com.eframework.ai-loop`. The package drives Unity Editor and PlayMode state, so these tests should be run in a visible Editor session before changing API contracts or releasing the package.

## Test Project

Use `test-fixtures/EFrameConsumerUnity` unless a business project exposes the bug more clearly.

Before testing:

- Add `com.eframework.ai-loop` to the test project's `Packages/manifest.json` as a file dependency if it is not already installed:

```json
"com.eframework.ai-loop": "file:../../../packages/com.eframework.ai-loop"
```

- Open `Assets/Scenes/StartUp.unity`.
- Enter PlayMode and wait until the EFrame startup procedure reaches a stable UI.
- Keep Game View visible at a fixed resolution. Record the resolution used in the test notes.
- Use Unity Input System as the active input backend.

## Coordinate Contract

Agent-facing screenshot element coordinates and mouse request coordinates are Game View pixel coordinates with origin at top-left.

Internal uGUI event dispatch still uses Unity screen coordinates with origin at bottom-left. Do not expose the internal coordinate system through public request/result data.

## Screenshot

| Case | Steps | Expected Result |
| --- | --- | --- |
| Game rendering screenshot | Call `EFrameAiLoop.CaptureGameViewAsync()` in PlayMode. | Result succeeds, PNG exists, dimensions match Game View capture scale, no annotation overlay remains in the scene. |
| Annotated screenshot | Call `CaptureGameViewAsync(new EFrameScreenshotOptions { AnnotateElements = true })`. | PNG contains visible labels and boxes over interactable uGUI elements. Labels match `Elements`. Boxes are not vertically flipped. |
| Elements only | Call `CaptureGameViewAsync(new EFrameScreenshotOptions { AnnotateElements = true, ElementsOnly = true })`. | No PNG is written. `Elements` contains top-left coordinates that can be passed directly to mouse simulation. |
| Editor window screenshot | Call `EFrameScreenshot.CaptureAsync` with `CaptureMode = EditorWindow` and `WindowName = "Game"`. | Result succeeds when the target EditorWindow is open, and fails cleanly when the name is invalid. |

## Mouse UI Simulation

| Case | Steps | Expected Result |
| --- | --- | --- |
| Click button | Use an element center from `ElementsOnly`, then call `EFrameAiLoop.ClickUiAsync(x, y)`. | The target uGUI click handler runs once. Result names the hit object when available. |
| Long press | Call `EFrameMouseUiSimulator.ExecuteAsync` with `Action = LongPress` and a positive duration. | Pointer down is held for at least one frame and released. UI is not left in pressed state. |
| Cancelled long press | Start long press with a cancellation token and cancel before duration completes. | The task throws cancellation, but the target receives pointer up and subsequent clicks still work. |
| Drag | Call drag from a draggable element to a valid drop point. | `initializePotentialDrag`, `beginDrag`, `drag`, `drop`, and `endDrag` execute in order. |
| Cancelled drag | Cancel after `beginDrag`. | The task throws cancellation, but `pointerUp` and `endDrag` still execute. The next drag starts from a clean UI state. |

## Keyboard Simulation

| Case | Steps | Expected Result |
| --- | --- | --- |
| Press key | Call `EFrameAiLoop.PressKeyAsync("Space", 0.1f)`. | `Keyboard.current.spaceKey` is pressed for at least one frame and released. |
| Cancelled press | Cancel `PressKeyAsync` after key down. | The task throws cancellation, and the key is released in Input System state. |
| Key down/up | Call `EFrameKeyboardSimulator` with `KeyDown`, then `KeyUp`. | The key remains held between calls and is released by `KeyUp`. Duplicate down/up calls fail cleanly. |
| PlayMode exit while held | Start a held key, then exit PlayMode before `KeyUp`. | PlayMode exit clears held keyboard state and the next PlayMode session can start key simulation. |

## Record Input

| Case | Steps | Expected Result |
| --- | --- | --- |
| Start/stop recording | Call `StartRecordingInputAsync`, perform keyboard and mouse actions, then `StopRecordingInputAsync`. | JSON is saved under `.eframe/outputs/InputRecordings` by default. Metadata and frame events are populated. |
| Already active guard | Call start twice. | Second start fails with an already-active message. |
| Replay conflict guard | Start replay, then attempt recording. | Recording fails while replay is active. |
| PlayMode exit while recording | Start recording, exit PlayMode without stop. | Recorder unsubscribes from `InputSystem.onAfterUpdate`; next PlayMode can start recording. |

## Replay Input

| Case | Steps | Expected Result |
| --- | --- | --- |
| Replay saved file | Start replay with a saved JSON path. | Keyboard/mouse snapshots are injected frame by frame and UI events are dispatched for left mouse input. |
| Stop mid-press | Replay a file that holds left mouse down, then call `StopReplayInput`. | Input System buttons/keys are released and uGUI receives pointer up. No pressed UI state remains. |
| Stop mid-drag | Replay a file that starts drag, then call `StopReplayInput`. | UI receives pointer up, optional drop, and end drag. Next replay/click works. |
| Loop replay | Start replay with `loop = true`. | Replay resets to frame zero after `TotalFrames`, releases held input between loops, and continues. |
| PlayMode exit while replaying | Start replay, then exit PlayMode. | Replay unsubscribes from `InputSystem.onAfterUpdate`, clears held input state, and next PlayMode can start replay. |

## Release Gate

Before release:

- Run the editor assembly compile check against Unity 6000.x.
- Run `git diff --check`.
- Run this checklist in a visible Editor session.
- Update this file when a known limitation becomes an explicit contract.
