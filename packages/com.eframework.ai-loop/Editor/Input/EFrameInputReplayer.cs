#if EFRAME_AI_LOOP_HAS_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace EFramework.Editor.AILoop
{
    [InitializeOnLoad]
    public static class EFrameInputReplayer
    {
        private static readonly Key[] AllKeys = BuildAllKeys();
        private static readonly HashSet<Key> HeldKeys = new();
        private static readonly HashSet<EFrameMouseButton> HeldButtons = new();
        private static EFrameInputRecordingData s_data;
        private static int s_eventIndex;
        private static bool s_loop;
        private static Vector2? s_mousePosition;
        private static PointerEventData s_pointerData;
        private static GameObject s_pressTarget;
        private static GameObject s_dragTarget;
        private static bool s_wasLeftHeld;
        private static bool s_isDragging;
        private static Vector2 s_pressPosition;

        public static bool IsReplaying { get; private set; }
        public static int CurrentFrame { get; private set; }
        public static int TotalFrames => s_data?.metadata.totalFrames ?? 0;

        static EFrameInputReplayer()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static Task<EFrameReplayInputResult> StartAsync(
            EFrameReplayInputRequest request,
            CancellationToken cancellationToken = default)
        {
            request ??= new EFrameReplayInputRequest();

            if (!EditorApplication.isPlaying)
            {
                return Task.FromResult(Fail("PlayMode is not active."));
            }

            if (EditorApplication.isPaused)
            {
                return Task.FromResult(Fail("PlayMode is paused."));
            }

            if (IsReplaying)
            {
                return Task.FromResult(Fail("Input replay is already active."));
            }

            if (EFrameInputRecorder.IsRecording)
            {
                return Task.FromResult(Fail("Cannot replay while input recording is active."));
            }

            string inputPath = EFrameInputRecordingFileStore.ResolveInputPath(request.InputPath);
            if (string.IsNullOrEmpty(inputPath) || !File.Exists(inputPath))
            {
                return Task.FromResult(Fail($"Recording file not found: {inputPath}"));
            }

            s_data = EFrameInputRecordingFileStore.Load(inputPath);
            if (s_data == null)
            {
                return Task.FromResult(Fail($"Failed to read recording: {inputPath}"));
            }

            HeldKeys.Clear();
            HeldButtons.Clear();
            ResetUiState();
            CurrentFrame = 0;
            s_eventIndex = 0;
            s_loop = request.Loop;
            IsReplaying = true;

            InputSystem.onAfterUpdate -= OnAfterUpdate;
            InputSystem.onAfterUpdate += OnAfterUpdate;

            return Task.FromResult(new EFrameReplayInputResult
            {
                Success = true,
                Message = $"Input replay started: {inputPath}",
                InputPath = inputPath,
                TotalFrames = TotalFrames,
                IsReplaying = true
            });
        }

        public static EFrameReplayInputResult Stop()
        {
            if (!IsReplaying)
            {
                return Fail("Input replay is not active.");
            }

            int stoppedFrame = CurrentFrame;
            int totalFrames = TotalFrames;
            StopInternal(true);
            return new EFrameReplayInputResult
            {
                Success = true,
                Message = $"Input replay stopped at frame {stoppedFrame}/{totalFrames}.",
                CurrentFrame = stoppedFrame,
                TotalFrames = totalFrames
            };
        }

        private static void OnAfterUpdate()
        {
            if (!IsReplaying || s_data == null || !IsTargetUpdate())
            {
                return;
            }

            Vector2 frameDelta = Vector2.zero;
            Vector2 frameScroll = Vector2.zero;
            while (s_eventIndex < s_data.frames.Count && s_data.frames[s_eventIndex].frame <= CurrentFrame)
            {
                foreach (EFrameRecordedInputEvent inputEvent in s_data.frames[s_eventIndex].events)
                {
                    ProcessEvent(inputEvent, ref frameDelta, ref frameScroll);
                }

                s_eventIndex++;
            }

            ApplyKeyboardSnapshot(Keyboard.current);
            ApplyMouseSnapshot(Mouse.current, frameDelta, frameScroll);
            ApplyUiEvents();

            CurrentFrame++;
            if (s_eventIndex >= s_data.frames.Count && CurrentFrame > TotalFrames)
            {
                if (s_loop)
                {
                    DispatchUiReleaseForStop();
                    ReleaseAllInputs();
                    ResetUiState();
                    CurrentFrame = 0;
                    s_eventIndex = 0;
                }
                else
                {
                    StopInternal(true);
                }
            }
        }

        private static void ProcessEvent(
            EFrameRecordedInputEvent inputEvent,
            ref Vector2 frameDelta,
            ref Vector2 frameScroll)
        {
            switch (inputEvent.type)
            {
                case EFrameInputEventTypes.KeyDown:
                    if (Enum.TryParse(inputEvent.data, true, out Key downKey)) HeldKeys.Add(downKey);
                    break;
                case EFrameInputEventTypes.KeyUp:
                    if (Enum.TryParse(inputEvent.data, true, out Key upKey)) HeldKeys.Remove(upKey);
                    break;
                case EFrameInputEventTypes.MouseDown:
                    if (Enum.TryParse(inputEvent.data, true, out EFrameMouseButton downButton)) HeldButtons.Add(downButton);
                    break;
                case EFrameInputEventTypes.MouseUp:
                    if (Enum.TryParse(inputEvent.data, true, out EFrameMouseButton upButton)) HeldButtons.Remove(upButton);
                    break;
                case EFrameInputEventTypes.MouseDelta:
                    frameDelta = EFrameInputRecordingFileStore.ParseVector2(inputEvent.data);
                    break;
                case EFrameInputEventTypes.MouseScroll:
                    frameScroll = EFrameInputRecordingFileStore.ParseVector2(inputEvent.data);
                    break;
                case EFrameInputEventTypes.MousePosition:
                    s_mousePosition = EFrameInputRecordingFileStore.ParseVector2(inputEvent.data);
                    break;
            }
        }

        private static void ApplyKeyboardSnapshot(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return;
            }

            using (StateEvent.From(keyboard, out InputEventPtr eventPtr))
            {
                foreach (Key key in AllKeys)
                {
                    KeyControl control = keyboard[key];
                    if (control != null)
                    {
                        control.WriteValueIntoEvent(0f, eventPtr);
                    }
                }

                foreach (Key key in HeldKeys)
                {
                    keyboard[key].WriteValueIntoEvent(1f, eventPtr);
                }

                InputState.Change(keyboard, eventPtr, EFrameAiLoopInputUpdateTypeResolver.Resolve());
            }
        }

        private static void ApplyMouseSnapshot(Mouse mouse, Vector2 delta, Vector2 scroll)
        {
            if (mouse == null)
            {
                return;
            }

            using (StateEvent.From(mouse, out InputEventPtr eventPtr))
            {
                mouse.leftButton.WriteValueIntoEvent(0f, eventPtr);
                mouse.rightButton.WriteValueIntoEvent(0f, eventPtr);
                mouse.middleButton.WriteValueIntoEvent(0f, eventPtr);
                mouse.delta.WriteValueIntoEvent(delta, eventPtr);
                mouse.scroll.WriteValueIntoEvent(scroll, eventPtr);
                if (s_mousePosition.HasValue)
                {
                    mouse.position.WriteValueIntoEvent(s_mousePosition.Value, eventPtr);
                }

                foreach (EFrameMouseButton button in HeldButtons)
                {
                    EFrameInputRecorder.GetButtonControl(mouse, button).WriteValueIntoEvent(1f, eventPtr);
                }

                InputState.Change(mouse, eventPtr, EFrameAiLoopInputUpdateTypeResolver.Resolve());
            }
        }

        private static void ApplyUiEvents()
        {
            if (!s_mousePosition.HasValue)
            {
                return;
            }

            EventSystem eventSystem = EFrameMouseUiSimulator.ResolveEventSystem();
            if (eventSystem == null)
            {
                return;
            }

            Vector2 screenPos = s_mousePosition.Value;
            bool leftHeld = HeldButtons.Contains(EFrameMouseButton.Left);
            bool justPressed = leftHeld && !s_wasLeftHeld;
            bool justReleased = !leftHeld && s_wasLeftHeld;
            s_wasLeftHeld = leftHeld;

            if (justPressed)
            {
                OnPointerDown(screenPos, eventSystem);
            }
            else if (leftHeld)
            {
                OnPointerDrag(screenPos);
            }
            else if (justReleased)
            {
                OnPointerUp(screenPos, eventSystem);
            }
        }

        private static void OnPointerDown(Vector2 screenPos, EventSystem eventSystem)
        {
            RaycastResult? hit = EFrameMouseUiSimulator.RaycastUi(screenPos, eventSystem);
            s_pointerData = new PointerEventData(eventSystem)
            {
                position = screenPos,
                pressPosition = screenPos,
                button = PointerEventData.InputButton.Left
            };
            s_pressPosition = screenPos;
            s_isDragging = false;
            s_pressTarget = null;
            s_dragTarget = null;

            if (!hit.HasValue)
            {
                return;
            }

            GameObject rawTarget = hit.Value.gameObject;
            s_pointerData.pointerCurrentRaycast = hit.Value;
            s_pointerData.pointerPressRaycast = hit.Value;

            s_pressTarget = ExecuteEvents.GetEventHandler<IPointerDownHandler>(rawTarget)
                            ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(rawTarget);
            if (s_pressTarget != null)
            {
                s_pointerData.pointerPress = s_pressTarget;
                s_pointerData.rawPointerPress = rawTarget;
                ExecuteEvents.ExecuteHierarchy(rawTarget, s_pointerData, ExecuteEvents.pointerDownHandler);
            }

            s_dragTarget = ExecuteEvents.GetEventHandler<IDragHandler>(rawTarget);
            if (s_dragTarget != null)
            {
                ExecuteEvents.Execute(s_dragTarget, s_pointerData, ExecuteEvents.initializePotentialDrag);
            }
        }

        private static void OnPointerDrag(Vector2 screenPos)
        {
            if (s_pointerData == null)
            {
                return;
            }

            Vector2 previous = s_pointerData.position;
            Vector2 delta = screenPos - previous;
            if (delta == Vector2.zero)
            {
                return;
            }

            s_pointerData.position = screenPos;
            s_pointerData.delta = delta;

            if (!s_isDragging && s_dragTarget != null &&
                Vector2.Distance(screenPos, s_pressPosition) > ResolveDragThreshold())
            {
                s_isDragging = true;
                s_pointerData.dragging = true;
                s_pointerData.pointerDrag = s_dragTarget;
                ExecuteEvents.Execute(s_dragTarget, s_pointerData, ExecuteEvents.beginDragHandler);
            }

            if (s_isDragging && s_dragTarget != null)
            {
                ExecuteEvents.Execute(s_dragTarget, s_pointerData, ExecuteEvents.dragHandler);
            }
        }

        private static void OnPointerUp(Vector2 screenPos, EventSystem eventSystem)
        {
            FinalizePointer(screenPos, eventSystem, true);
        }

        private static void FinalizePointer(Vector2 screenPos, EventSystem eventSystem, bool sendClick)
        {
            if (s_pointerData == null)
            {
                return;
            }

            s_pointerData.position = screenPos;
            if (s_pressTarget != null)
            {
                ExecuteEvents.Execute(s_pressTarget, s_pointerData, ExecuteEvents.pointerUpHandler);
            }

            if (s_isDragging && s_dragTarget != null)
            {
                RaycastResult? dropHit = EFrameMouseUiSimulator.RaycastUi(screenPos, eventSystem);
                if (dropHit.HasValue)
                {
                    ExecuteEvents.ExecuteHierarchy(dropHit.Value.gameObject, s_pointerData, ExecuteEvents.dropHandler);
                }

                ExecuteEvents.Execute(s_dragTarget, s_pointerData, ExecuteEvents.endDragHandler);
            }
            else if (sendClick && s_pressTarget != null)
            {
                GameObject clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(
                    s_pointerData.rawPointerPress ?? s_pressTarget);
                if (clickTarget != null)
                {
                    ExecuteEvents.Execute(clickTarget, s_pointerData, ExecuteEvents.pointerClickHandler);
                }
            }

            ResetUiState();
        }

        private static void StopInternal(bool dispatchUiRelease)
        {
            InputSystem.onAfterUpdate -= OnAfterUpdate;
            IsReplaying = false;
            if (dispatchUiRelease)
            {
                DispatchUiReleaseForStop();
            }
            else
            {
                ResetUiState();
            }

            ReleaseAllInputs();
            s_data = null;
            CurrentFrame = 0;
            s_eventIndex = 0;
            ResetUiState();
        }

        private static void DispatchUiReleaseForStop()
        {
            if (s_pointerData == null)
            {
                return;
            }

            EventSystem eventSystem = EFrameMouseUiSimulator.ResolveEventSystem();
            if (eventSystem == null)
            {
                ResetUiState();
                return;
            }

            FinalizePointer(s_mousePosition ?? s_pointerData.position, eventSystem, false);
        }

        private static void ReleaseAllInputs()
        {
            HeldKeys.Clear();
            HeldButtons.Clear();
            ApplyKeyboardSnapshot(Keyboard.current);
            ApplyMouseSnapshot(Mouse.current, Vector2.zero, Vector2.zero);
        }

        private static void ResetUiState()
        {
            s_mousePosition = null;
            s_pointerData = null;
            s_pressTarget = null;
            s_dragTarget = null;
            s_wasLeftHeld = false;
            s_isDragging = false;
            s_pressPosition = Vector2.zero;
        }

        private static Key[] BuildAllKeys()
        {
            List<Key> keys = new();
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (key != Key.None)
                {
                    keys.Add(key);
                }
            }

            return keys.ToArray();
        }

        private static bool IsTargetUpdate()
        {
            return EFrameAiLoopInputUpdateTypeResolver.IsMatch(
                InputState.currentUpdateType,
                EFrameAiLoopInputUpdateTypeResolver.Resolve());
        }

        private static int ResolveDragThreshold()
        {
            EventSystem eventSystem = EFrameMouseUiSimulator.ResolveEventSystem();
            return eventSystem != null ? eventSystem.pixelDragThreshold : 5;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (IsReplaying || HeldKeys.Count > 0 || HeldButtons.Count > 0 || s_pointerData != null)
                {
                    StopInternal(false);
                }
                else
                {
                    InputSystem.onAfterUpdate -= OnAfterUpdate;
                    ResetUiState();
                }
            }
        }

        private static EFrameReplayInputResult Fail(string message)
        {
            return new EFrameReplayInputResult
            {
                Success = false,
                Message = message,
                CurrentFrame = CurrentFrame,
                TotalFrames = TotalFrames,
                Progress = TotalFrames > 0 ? (float)CurrentFrame / TotalFrames : 0f,
                IsReplaying = IsReplaying
            };
        }
    }
}
#else
using System.Threading;
using System.Threading.Tasks;

namespace EFramework.Editor.AILoop
{
    public static class EFrameInputReplayer
    {
        public static bool IsReplaying => false;

        public static Task<EFrameReplayInputResult> StartAsync(EFrameReplayInputRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EFrameReplayInputResult { Success = false, Message = "Unity Input System package is not available." });
        }

        public static EFrameReplayInputResult Stop()
        {
            return new EFrameReplayInputResult { Success = false, Message = "Unity Input System package is not available." };
        }
    }
}
#endif
