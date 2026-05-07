#if EFRAME_AI_LOOP_HAS_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EFramework.Editor.AILoop
{
    [InitializeOnLoad]
    public static class EFrameKeyboardSimulator
    {
        private static readonly HashSet<Key> HeldKeys = new();
        private static Key? s_pressKey;

        static EFrameKeyboardSimulator()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static async Task<EFrameKeyboardResult> ExecuteAsync(
            EFrameKeyboardRequest request,
            CancellationToken cancellationToken = default)
        {
            request ??= new EFrameKeyboardRequest();

            if (!EditorApplication.isPlaying)
            {
                return Fail("PlayMode is not active.", request.Key);
            }

            if (EditorApplication.isPaused)
            {
                return Fail("PlayMode is paused.", request.Key);
            }

            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return Fail("Key is required.", request.Key);
            }

            string normalized = NormalizeKeyName(request.Key);
            if (!Enum.TryParse(normalized, true, out Key key) || key == Key.None)
            {
                return Fail($"Invalid Input System key: {request.Key}.", request.Key);
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Fail("No Keyboard.current device exists.", normalized);
            }

            switch (request.Action)
            {
                case EFrameKeyboardAction.Press:
                    return await PressAsync(keyboard, key, request.Duration, cancellationToken);
                case EFrameKeyboardAction.KeyDown:
                    return await KeyDownAsync(keyboard, key, cancellationToken);
                case EFrameKeyboardAction.KeyUp:
                    return await KeyUpAsync(keyboard, key, cancellationToken);
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Action), request.Action, null);
            }
        }

        private static async Task<EFrameKeyboardResult> PressAsync(
            Keyboard keyboard,
            Key key,
            float duration,
            CancellationToken cancellationToken)
        {
            if (duration < 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            {
                return Fail($"Duration must be non-negative, got {duration}.", key.ToString());
            }

            if (HeldKeys.Contains(key))
            {
                return Fail($"Key '{key}' is already held. Release it before Press.", key.ToString());
            }

            s_pressKey = key;
            SetKeyState(keyboard, key, true);
            try
            {
                await WaitForPressLifetime(duration, cancellationToken);
            }
            finally
            {
                ReleaseTransientPress(keyboard, key);
            }

            await EFrameAiLoopDelay.DelayFrames(1, cancellationToken);

            return Ok($"Pressed '{key}'{(duration > 0f ? $" for {duration:F1}s" : "")}.", key.ToString());
        }

        private static async Task<EFrameKeyboardResult> KeyDownAsync(
            Keyboard keyboard,
            Key key,
            CancellationToken cancellationToken)
        {
            if (HeldKeys.Contains(key))
            {
                return Fail($"Key '{key}' is already held.", key.ToString());
            }

            HeldKeys.Add(key);
            bool committed = false;
            try
            {
                SetKeyState(keyboard, key, true);
                await EFrameAiLoopDelay.DelayFrames(1, cancellationToken);
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    HeldKeys.Remove(key);
                    ReleaseKey(keyboard, key);
                }
            }

            return Ok($"Key '{key}' held down.", key.ToString());
        }

        private static async Task<EFrameKeyboardResult> KeyUpAsync(
            Keyboard keyboard,
            Key key,
            CancellationToken cancellationToken)
        {
            if (!HeldKeys.Contains(key))
            {
                return Fail($"Key '{key}' is not currently held.", key.ToString());
            }

            HeldKeys.Remove(key);
            SetKeyState(keyboard, key, false);
            await EFrameAiLoopDelay.DelayFrames(1, cancellationToken);
            return Ok($"Key '{key}' released.", key.ToString());
        }

        internal static void ReleaseAllKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                HashSet<Key> keysToRelease = new(HeldKeys);
                if (s_pressKey.HasValue)
                {
                    keysToRelease.Add(s_pressKey.Value);
                }

                if (keysToRelease.Count > 0)
                {
                    InputUpdateType updateType = EFrameAiLoopInputUpdateTypeResolver.Resolve();
                    using (StateEvent.From(keyboard, out InputEventPtr eventPtr))
                    {
                        foreach (Key key in keysToRelease)
                        {
                            keyboard[key].WriteValueIntoEvent(0f, eventPtr);
                        }

                        InputState.Change(keyboard, eventPtr, updateType);
                    }
                }
            }

            HeldKeys.Clear();
            s_pressKey = null;
        }

        private static void ReleaseTransientPress(Keyboard keyboard, Key key)
        {
            if (s_pressKey == key)
            {
                s_pressKey = null;
            }

            ReleaseKey(keyboard, key);
        }

        private static void ReleaseKey(Keyboard keyboard, Key key)
        {
            if (keyboard == null)
            {
                return;
            }

            SetKeyState(keyboard, key, false);
        }

        private static void SetKeyState(Keyboard keyboard, Key key, bool pressed)
        {
            InputUpdateType updateType = EFrameAiLoopInputUpdateTypeResolver.Resolve();
            using (StateEvent.From(keyboard, out InputEventPtr eventPtr))
            {
                foreach (Key heldKey in HeldKeys)
                {
                    keyboard[heldKey].WriteValueIntoEvent(1f, eventPtr);
                }

                keyboard[key].WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                InputState.Change(keyboard, eventPtr, updateType);
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ReleaseAllKeys();
            }
        }

        private static async Task WaitForPressLifetime(float duration, CancellationToken cancellationToken)
        {
            int startFrame = Time.frameCount;
            float startedAt = Time.realtimeSinceStartup;
            do
            {
                await EFrameAiLoopDelay.DelayFrames(1, cancellationToken);
            } while (Time.frameCount - startFrame < 1 ||
                     Time.realtimeSinceStartup - startedAt < duration);
        }

        private static string NormalizeKeyName(string key)
        {
            return string.Equals(key, "Return", StringComparison.OrdinalIgnoreCase)
                ? Key.Enter.ToString()
                : key;
        }

        private static EFrameKeyboardResult Ok(string message, string key)
        {
            return new EFrameKeyboardResult
            {
                Success = true,
                Message = message,
                Key = key
            };
        }

        private static EFrameKeyboardResult Fail(string message, string key)
        {
            return new EFrameKeyboardResult
            {
                Success = false,
                Message = message,
                Key = key
            };
        }
    }
}
#else
using System.Threading;
using System.Threading.Tasks;

namespace EFramework.Editor.AILoop
{
    public static class EFrameKeyboardSimulator
    {
        public static Task<EFrameKeyboardResult> ExecuteAsync(
            EFrameKeyboardRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EFrameKeyboardResult
            {
                Success = false,
            Message = "Unity Input System package is not available.",
                Key = request?.Key ?? ""
            });
        }
    }
}
#endif
