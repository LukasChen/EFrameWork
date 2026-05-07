# EFrame AI Loop

Editor-only tooling for AI-assisted EFrame development loops.

This package currently provides:

- Game and Editor window screenshot capture.
- EFrame-aware interactive UI element discovery and optional Game View annotation.
- PlayMode uGUI mouse event simulation through `EventSystem` / `ExecuteEvents`.
- PlayMode keyboard simulation through Unity Input System state injection.
- PlayMode keyboard and mouse input recording/replay through JSON recordings.

The package is intentionally separate from `com.eframework.core` so business runtime UI remains routed through `EFrame.UI` and `UIControllerBase<TView>`. These APIs are for editor automation, AI diagnostics, and PlayMode test harnesses.

## Example

```csharp
using EFramework.Editor.AILoop;

await EFrameAiLoop.CaptureGameViewAsync(new EFrameScreenshotOptions
{
    AnnotateElements = true
});

await EFrameAiLoop.ClickUiAsync(640, 360);
await EFrameAiLoop.PressKeyAsync("Space", 0.1f);

await EFrameAiLoop.StartRecordingInputAsync();
var saved = await EFrameAiLoop.StopRecordingInputAsync();
await EFrameAiLoop.StartReplayInputAsync(saved.OutputPath);
```

## Notes

- Screenshot Game rendering requires PlayMode and an available Game View render texture.
- Mouse UI simulation targets uGUI event handlers. It does not move the OS cursor.
- Keyboard simulation targets Unity Input System APIs. It does not affect legacy `Input.GetKey` paths.
- Input replay injects the same frame-indexed input snapshots, but deterministic gameplay still requires a stable scene/procedure setup, fixed seeds, and controlled frame timing.

## Validation

Run the visible-Editor acceptance pass in [Documentation~/manual-test-checklist.md](Documentation~/manual-test-checklist.md) before changing API contracts or releasing this package.
