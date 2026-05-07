#if EFRAME_AI_LOOP_HAS_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EFramework.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EFramework.Editor.AILoop
{
    [InitializeOnLoad]
    public static class EFrameInputRecorder
    {
        private static readonly Key[] DefaultKeys =
        {
            Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M,
            Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7,
            Key.Digit8, Key.Digit9, Key.Space, Key.LeftShift, Key.RightShift, Key.LeftCtrl, Key.RightCtrl,
            Key.LeftAlt, Key.RightAlt, Key.Tab, Key.Escape, Key.Enter, Key.Backspace,
            Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow
        };

        private static readonly EFrameMouseButton[] MouseButtons =
        {
            EFrameMouseButton.Left,
            EFrameMouseButton.Right,
            EFrameMouseButton.Middle
        };

        private static readonly List<EFrameInputFrameEvents> Frames = new();
        private static readonly List<EFrameRecordedInputEvent> FrameEvents = new();
        private static readonly HashSet<Key> PreviousKeys = new();
        private static readonly HashSet<Key> CurrentKeys = new();
        private static readonly HashSet<EFrameMouseButton> PreviousButtons = new();
        private static readonly HashSet<EFrameMouseButton> CurrentButtons = new();
        private static Key[] s_keysToScan = DefaultKeys;
        private static int s_startFrame;
        private static float s_startTime;

        public static bool IsRecording { get; private set; }

        static EFrameInputRecorder()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static Task<EFrameRecordInputResult> StartAsync(
            EFrameRecordInputRequest request,
            CancellationToken cancellationToken = default)
        {
            request ??= new EFrameRecordInputRequest();

            if (!EditorApplication.isPlaying)
            {
                return Task.FromResult(Fail("PlayMode is not active."));
            }

            if (EditorApplication.isPaused)
            {
                return Task.FromResult(Fail("PlayMode is paused."));
            }

            if (IsRecording)
            {
                return Task.FromResult(Fail("Input recording is already active."));
            }

            if (EFrameInputReplayer.IsReplaying)
            {
                return Task.FromResult(Fail("Cannot record while input replay is active."));
            }

            Frames.Clear();
            PreviousKeys.Clear();
            PreviousButtons.Clear();
            s_keysToScan = EFrameInputRecordingFileStore.ParseKeyFilter(request.Keys) ?? DefaultKeys;
            s_startFrame = Time.frameCount;
            s_startTime = Time.realtimeSinceStartup;
            CaptureInitialState();
            EmitInitialHeldEvents();

            InputSystem.onAfterUpdate -= OnAfterUpdate;
            InputSystem.onAfterUpdate += OnAfterUpdate;
            IsRecording = true;

            return Task.FromResult(new EFrameRecordInputResult
            {
                Success = true,
                Message = "Input recording started."
            });
        }

        public static Task<EFrameRecordInputResult> StopAsync(
            string outputPath = "",
            CancellationToken cancellationToken = default)
        {
            if (!IsRecording)
            {
                return Task.FromResult(Fail("Input recording is not active."));
            }

            InputSystem.onAfterUpdate -= OnAfterUpdate;
            IsRecording = false;

            EFrameInputRecordingData data = new()
            {
                metadata = new EFrameInputRecordingMetadata
                {
                    recordedAt = DateTime.UtcNow.ToString("o"),
                    unityVersion = Application.unityVersion,
                    eframeVersion = EFrame.Initialized ? "initialized" : "not-initialized",
                    screenWidth = Screen.width,
                    screenHeight = Screen.height,
                    totalFrames = Time.frameCount - s_startFrame,
                    durationSeconds = Time.realtimeSinceStartup - s_startTime
                },
                frames = new List<EFrameInputFrameEvents>(Frames)
            };

            string resolvedPath = EFrameInputRecordingFileStore.ResolveOutputPath(outputPath);
            EFrameInputRecordingFileStore.Save(data, resolvedPath);

            EFrameRecordInputResult result = new()
            {
                Success = true,
                Message = $"Input recording saved: {resolvedPath}",
                OutputPath = resolvedPath,
                TotalFrames = data.metadata.totalFrames,
                DurationSeconds = data.metadata.durationSeconds,
                EventCount = data.GetTotalEventCount()
            };
            result.ReportPath = EFrameAiLoopReport.WriteInputRecordingReport(result);
            return Task.FromResult(result);
        }

        internal static void CancelActiveRecording()
        {
            InputSystem.onAfterUpdate -= OnAfterUpdate;
            IsRecording = false;
            Frames.Clear();
            FrameEvents.Clear();
            PreviousKeys.Clear();
            CurrentKeys.Clear();
            PreviousButtons.Clear();
            CurrentButtons.Clear();
            s_keysToScan = DefaultKeys;
            s_startFrame = 0;
            s_startTime = 0f;
        }

        private static void OnAfterUpdate()
        {
            if (!IsRecording || !IsTargetUpdate())
            {
                return;
            }

            FrameEvents.Clear();
            RecordKeyboardEvents();
            RecordMouseButtonEvents();
            RecordMouseDelta();
            RecordMouseScroll();
            RecordMousePosition();

            if (FrameEvents.Count == 0)
            {
                return;
            }

            Frames.Add(new EFrameInputFrameEvents
            {
                frame = Time.frameCount - s_startFrame,
                events = new List<EFrameRecordedInputEvent>(FrameEvents)
            });
        }

        private static void RecordKeyboardEvents()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            CurrentKeys.Clear();
            foreach (Key key in s_keysToScan)
            {
                if (keyboard[key].isPressed)
                {
                    CurrentKeys.Add(key);
                }
            }

            foreach (Key key in CurrentKeys)
            {
                if (!PreviousKeys.Contains(key))
                {
                    FrameEvents.Add(new EFrameRecordedInputEvent { type = EFrameInputEventTypes.KeyDown, data = key.ToString() });
                }
            }

            foreach (Key key in PreviousKeys)
            {
                if (!CurrentKeys.Contains(key))
                {
                    FrameEvents.Add(new EFrameRecordedInputEvent { type = EFrameInputEventTypes.KeyUp, data = key.ToString() });
                }
            }

            PreviousKeys.Clear();
            foreach (Key key in CurrentKeys)
            {
                PreviousKeys.Add(key);
            }
        }

        private static void RecordMouseButtonEvents()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            CurrentButtons.Clear();
            foreach (EFrameMouseButton button in MouseButtons)
            {
                if (GetButtonControl(mouse, button).isPressed)
                {
                    CurrentButtons.Add(button);
                }
            }

            foreach (EFrameMouseButton button in CurrentButtons)
            {
                if (!PreviousButtons.Contains(button))
                {
                    FrameEvents.Add(new EFrameRecordedInputEvent { type = EFrameInputEventTypes.MouseDown, data = button.ToString() });
                }
            }

            foreach (EFrameMouseButton button in PreviousButtons)
            {
                if (!CurrentButtons.Contains(button))
                {
                    FrameEvents.Add(new EFrameRecordedInputEvent { type = EFrameInputEventTypes.MouseUp, data = button.ToString() });
                }
            }

            PreviousButtons.Clear();
            foreach (EFrameMouseButton button in CurrentButtons)
            {
                PreviousButtons.Add(button);
            }
        }

        private static void RecordMouseDelta()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            if (delta != Vector2.zero)
            {
                FrameEvents.Add(new EFrameRecordedInputEvent
                {
                    type = EFrameInputEventTypes.MouseDelta,
                    data = EFrameInputRecordingFileStore.FormatVector2(delta)
                });
            }
        }

        private static void RecordMouseScroll()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 scroll = mouse.scroll.ReadValue();
            if (scroll != Vector2.zero)
            {
                FrameEvents.Add(new EFrameRecordedInputEvent
                {
                    type = EFrameInputEventTypes.MouseScroll,
                    data = EFrameInputRecordingFileStore.FormatVector2(scroll)
                });
            }
        }

        private static void RecordMousePosition()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            FrameEvents.Add(new EFrameRecordedInputEvent
            {
                type = EFrameInputEventTypes.MousePosition,
                data = EFrameInputRecordingFileStore.FormatVector2(mouse.position.ReadValue())
            });
        }

        private static void CaptureInitialState()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                foreach (Key key in s_keysToScan)
                {
                    if (keyboard[key].isPressed)
                    {
                        PreviousKeys.Add(key);
                    }
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                foreach (EFrameMouseButton button in MouseButtons)
                {
                    if (GetButtonControl(mouse, button).isPressed)
                    {
                        PreviousButtons.Add(button);
                    }
                }
            }
        }

        private static void EmitInitialHeldEvents()
        {
            FrameEvents.Clear();
            foreach (Key key in PreviousKeys)
            {
                FrameEvents.Add(new EFrameRecordedInputEvent { type = EFrameInputEventTypes.KeyDown, data = key.ToString() });
            }

            foreach (EFrameMouseButton button in PreviousButtons)
            {
                FrameEvents.Add(new EFrameRecordedInputEvent { type = EFrameInputEventTypes.MouseDown, data = button.ToString() });
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                FrameEvents.Add(new EFrameRecordedInputEvent
                {
                    type = EFrameInputEventTypes.MousePosition,
                    data = EFrameInputRecordingFileStore.FormatVector2(mouse.position.ReadValue())
                });
            }

            if (FrameEvents.Count > 0)
            {
                Frames.Add(new EFrameInputFrameEvents { frame = 0, events = new List<EFrameRecordedInputEvent>(FrameEvents) });
            }
        }

        internal static UnityEngine.InputSystem.Controls.ButtonControl GetButtonControl(Mouse mouse, EFrameMouseButton button)
        {
            return button switch
            {
                EFrameMouseButton.Right => mouse.rightButton,
                EFrameMouseButton.Middle => mouse.middleButton,
                _ => mouse.leftButton
            };
        }

        private static bool IsTargetUpdate()
        {
            return EFrameAiLoopInputUpdateTypeResolver.IsMatch(
                InputState.currentUpdateType,
                EFrameAiLoopInputUpdateTypeResolver.Resolve());
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                CancelActiveRecording();
            }
        }

        private static EFrameRecordInputResult Fail(string message)
        {
            return new EFrameRecordInputResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
#else
using System.Threading;
using System.Threading.Tasks;

namespace EFramework.Editor.AILoop
{
    public static class EFrameInputRecorder
    {
        public static Task<EFrameRecordInputResult> StartAsync(EFrameRecordInputRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EFrameRecordInputResult { Success = false, Message = "Unity Input System package is not available." });
        }

        public static Task<EFrameRecordInputResult> StopAsync(string outputPath = "", CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EFrameRecordInputResult { Success = false, Message = "Unity Input System package is not available." });
        }
    }
}
#endif
