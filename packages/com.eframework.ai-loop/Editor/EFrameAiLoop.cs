using System.Threading;
using System.Threading.Tasks;

namespace EFramework.Editor.AILoop
{
    public static class EFrameAiLoop
    {
        public static Task<EFrameScreenshotResult> CaptureGameViewAsync(
            EFrameScreenshotOptions options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new EFrameScreenshotOptions();
            options.CaptureMode = EFrameCaptureMode.GameRendering;
            return EFrameScreenshot.CaptureAsync(options, cancellationToken);
        }

        public static Task<EFrameScreenshotResult> CaptureEditorWindowAsync(
            string windowName,
            CancellationToken cancellationToken = default)
        {
            return EFrameScreenshot.CaptureAsync(
                new EFrameScreenshotOptions
                {
                    CaptureMode = EFrameCaptureMode.EditorWindow,
                    WindowName = windowName
                },
                cancellationToken);
        }

        public static Task<EFrameMouseUiResult> ClickUiAsync(
            float x,
            float y,
            CancellationToken cancellationToken = default)
        {
            return EFrameMouseUiSimulator.ExecuteAsync(
                new EFrameMouseUiRequest
                {
                    Action = EFrameMouseUiAction.Click,
                    X = x,
                    Y = y
                },
                cancellationToken);
        }

        public static Task<EFrameKeyboardResult> PressKeyAsync(
            string key,
            float duration = 0f,
            CancellationToken cancellationToken = default)
        {
            return EFrameKeyboardSimulator.ExecuteAsync(
                new EFrameKeyboardRequest
                {
                    Action = EFrameKeyboardAction.Press,
                    Key = key,
                    Duration = duration
                },
                cancellationToken);
        }

        public static Task<EFrameRecordInputResult> StartRecordingInputAsync(
            CancellationToken cancellationToken = default)
        {
            return EFrameInputRecorder.StartAsync(new EFrameRecordInputRequest(), cancellationToken);
        }

        public static Task<EFrameRecordInputResult> StopRecordingInputAsync(
            string outputPath = "",
            CancellationToken cancellationToken = default)
        {
            return EFrameInputRecorder.StopAsync(outputPath, cancellationToken);
        }

        public static Task<EFrameReplayInputResult> StartReplayInputAsync(
            string inputPath = "",
            bool loop = false,
            CancellationToken cancellationToken = default)
        {
            return EFrameInputReplayer.StartAsync(
                new EFrameReplayInputRequest
                {
                    InputPath = inputPath,
                    Loop = loop
                },
                cancellationToken);
        }

        public static EFrameReplayInputResult StopReplayInput()
        {
            return EFrameInputReplayer.Stop();
        }
    }
}
