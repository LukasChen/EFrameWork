using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EFrameWork.Runtime.Utils
{
    public static class QInput
    {
        public static bool GetPrimaryPointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }
#endif

            return false;
        }

        public static bool TryGetPrimaryPointerPosition(out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null)
            {
                var primaryTouch = Touchscreen.current.primaryTouch;
                if (primaryTouch.press.isPressed || primaryTouch.press.wasPressedThisFrame || primaryTouch.press.wasReleasedThisFrame)
                {
                    position = primaryTouch.position.ReadValue();
                    return true;
                }
            }
#endif

            position = default;
            return false;
        }
    }
}
