#if EFRAME_AI_LOOP_HAS_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EFramework.Editor.AILoop
{
    internal static class EFrameAiLoopInputUpdateTypeResolver
    {
        public static InputUpdateType Resolve()
        {
            InputSettings settings = InputSystem.settings;
            if (settings == null)
            {
                return InputUpdateType.Dynamic;
            }

            return settings.updateMode switch
            {
                InputSettings.UpdateMode.ProcessEventsInFixedUpdate => InputUpdateType.Fixed,
                InputSettings.UpdateMode.ProcessEventsManually => InputUpdateType.Manual,
                _ => InputUpdateType.Dynamic
            };
        }

        public static bool IsMatch(InputUpdateType current, InputUpdateType target)
        {
            return current == target || (current & target) == target;
        }
    }
}
#endif
