using EFrame.Runtime.UI;
using UnityEngine;

namespace EFrame.Runtime
{
    /// <summary>
    /// Marks a camera as an EFrame scene camera that can host the persistent UI camera stack.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class EFrameSceneCamera : MonoBehaviour
    {
        [SerializeField] private int m_priority;
        private Camera m_camera;

        public int Priority => m_priority;

        public Camera Camera
        {
            get
            {
                if (m_camera == null)
                {
                    TryGetComponent(out m_camera);
                }

                return m_camera;
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void Register()
        {
            EFrame.Current?.UI?.RegisterSceneCamera(this);
        }

        private void Unregister()
        {
            EFrame.Current?.UI?.UnregisterSceneCamera(this);
        }
    }
}
