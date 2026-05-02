using UnityEngine;

namespace EFrame.Runtime.Effect.Fly
{
    /// <summary>
    /// 独立驱动 FlyAnimationSystem 更新的常驻组件。
    /// </summary>
    internal sealed class FlyAnimationSystemDriver : MonoBehaviour
    {
        private static FlyAnimationSystemDriver s_instance;

        internal static void EnsureExists()
        {
            if (s_instance != null) return;

            var go = new GameObject("FlyAnimationSystemDriver");
            if (Application.isPlaying)
            {
                // Avoid DontSaveInEditor on persistent objects during PlayMode
                go.hideFlags = HideFlags.HideInHierarchy;
                DontDestroyOnLoad(go);
            }
            else
            {
                // Editor-only fallback (should not persist)
                go.hideFlags = HideFlags.HideAndDontSave;
            }
            s_instance = go.AddComponent<FlyAnimationSystemDriver>();

            //            Debug.Log("[FlyAnimationSystem] Created driver.", go);
        }

        private void Update()
        {
            FlyAnimationSystem.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }
    }
}
