using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EFramework.Runtime.UI.Interactions
{
    public sealed class UIDismissDispatcher : MonoBehaviour, IUIDismissDispatcher
    {
        private readonly List<UIDismissScope> m_scopes = new();
        private Camera m_uiCamera;

        public void Initialize(Camera uiCamera)
        {
            m_uiCamera = uiCamera;
        }

        public UIDismissScope Register(IEnumerable<RectTransform> insideAreas, Action<UIDismissTrigger> onDismiss, bool ignoreCurrentFrame = true)
        {
            var scope = new UIDismissScope(this, insideAreas, onDismiss, ignoreCurrentFrame);
            m_scopes.Add(scope);
            return scope;
        }

        public UIDismissScope Register(Action<UIDismissTrigger> onDismiss, bool ignoreCurrentFrame = true, params RectTransform[] insideAreas)
        {
            return Register(insideAreas, onDismiss, ignoreCurrentFrame);
        }

        public void Unregister(UIDismissScope scope)
        {
            if (scope == null)
            {
                return;
            }

            m_scopes.Remove(scope);
        }

        public void Clear()
        {
            for (int i = m_scopes.Count - 1; i >= 0; i--)
            {
                m_scopes[i]?.Dispose();
            }

            m_scopes.Clear();
        }

        private void Update()
        {
            var scope = GetTopScope();
            if (scope == null)
            {
                return;
            }

            if (IsCancelPressed(out var cancelTrigger))
            {
                scope.Dismiss(cancelTrigger);
                return;
            }

            if (!TryGetPointerDown(out var pointerPosition))
            {
                return;
            }

            if (UnityEngine.Time.frameCount <= scope.IgnorePointerUntilFrame)
            {
                return;
            }

            if (!scope.Contains(pointerPosition, m_uiCamera))
            {
                scope.Dismiss(UIDismissTrigger.OutsidePointerDown);
            }
        }

        private UIDismissScope GetTopScope()
        {
            for (int i = m_scopes.Count - 1; i >= 0; i--)
            {
                var scope = m_scopes[i];
                if (scope == null || scope.IsDisposed)
                {
                    m_scopes.RemoveAt(i);
                    continue;
                }

                if (scope.Enabled)
                {
                    return scope;
                }
            }

            return null;
        }

        private static bool TryGetPointerDown(out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null)
            {
                var primaryTouch = Touchscreen.current.primaryTouch;
                if (primaryTouch.press.wasPressedThisFrame)
                {
                    position = primaryTouch.position.ReadValue();
                    return true;
                }
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    position = touch.position;
                    return true;
                }
            }
#endif
            position = default;
            return false;
        }

        private static bool IsCancelPressed(out UIDismissTrigger trigger)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                trigger = UIDismissTrigger.EscapeKey;
                return true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                trigger = UIDismissTrigger.EscapeKey;
                return true;
            }
#endif
            trigger = default;
            return false;
        }
    }
}
