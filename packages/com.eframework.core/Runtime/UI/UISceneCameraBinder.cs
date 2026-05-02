using EFramework.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EFramework.Runtime.UI
{
    internal sealed class UISceneCameraBinder : IDisposable
    {
        private readonly Camera m_uiCamera;
        private readonly Dictionary<EFrameSceneCamera, int> m_sceneCameras = new();
        private int m_nextOrder;
        private Camera m_boundBaseCamera;
        private bool m_disposed;

        public UISceneCameraBinder(Camera uiCamera)
        {
            m_uiCamera = uiCamera;
        }

        public void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Refresh();
        }

        public void Register(EFrameSceneCamera sceneCamera)
        {
            if (m_disposed || sceneCamera == null)
            {
                return;
            }

            RegisterInternal(sceneCamera);
            RebindBestCamera();
        }

        public void Unregister(EFrameSceneCamera sceneCamera)
        {
            if (m_disposed || sceneCamera == null)
            {
                return;
            }

            m_sceneCameras.Remove(sceneCamera);
            RebindBestCamera();
        }

        public void Refresh()
        {
            if (m_disposed)
            {
                return;
            }

            foreach (var sceneCamera in Resources.FindObjectsOfTypeAll<EFrameSceneCamera>())
            {
                if (!IsSceneRuntimeObject(sceneCamera))
                {
                    continue;
                }

                RegisterInternal(sceneCamera);
            }

            RebindBestCamera();
        }

        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            RemoveUICameraFromStack(m_boundBaseCamera);
            m_sceneCameras.Clear();
            m_boundBaseCamera = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Refresh();
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            Refresh();
        }

        private void RegisterInternal(EFrameSceneCamera sceneCamera)
        {
            if (!IsValidCandidate(sceneCamera))
            {
                return;
            }

            if (!m_sceneCameras.ContainsKey(sceneCamera))
            {
                m_sceneCameras.Add(sceneCamera, ++m_nextOrder);
            }
        }

        private void RebindBestCamera()
        {
            if (m_uiCamera == null)
            {
                return;
            }

            var bestCamera = ResolveBestCamera();
            if (bestCamera == m_boundBaseCamera && IsStackValid(bestCamera))
            {
                return;
            }

            var previousBaseCamera = m_boundBaseCamera;
            RemoveUICameraFromStack(previousBaseCamera);
            m_boundBaseCamera = bestCamera;

            if (m_boundBaseCamera == null)
            {
                ConfigureStandaloneUICamera();
                if (previousBaseCamera != null)
                {
                    UpdateContextSceneCamera(null);
                }
                return;
            }

            BindToBaseCamera(m_boundBaseCamera);
            UpdateContextSceneCamera(m_boundBaseCamera);
        }

        private Camera ResolveBestCamera()
        {
            EFrameSceneCamera best = null;
            int bestOrder = 0;
            List<EFrameSceneCamera> invalidCameras = null;

            foreach (var pair in m_sceneCameras)
            {
                var sceneCamera = pair.Key;
                if (!IsValidCandidate(sceneCamera))
                {
                    invalidCameras ??= new List<EFrameSceneCamera>();
                    invalidCameras.Add(sceneCamera);
                    continue;
                }

                if (best == null || IsBetter(sceneCamera, pair.Value, best, bestOrder))
                {
                    best = sceneCamera;
                    bestOrder = pair.Value;
                }
            }

            if (invalidCameras != null)
            {
                foreach (var invalid in invalidCameras)
                {
                    m_sceneCameras.Remove(invalid);
                }
            }

            return best != null ? best.Camera : null;
        }

        private bool IsValidCandidate(EFrameSceneCamera sceneCamera)
        {
            if (sceneCamera == null || !sceneCamera.isActiveAndEnabled)
            {
                return false;
            }

            var camera = sceneCamera.Camera;
            return camera != null
                && camera.enabled
                && camera != m_uiCamera
                && camera.gameObject.activeInHierarchy;
        }

            private static bool IsSceneRuntimeObject(Component component)
            {
                return component != null
                && component.gameObject.scene.IsValid()
                && component.gameObject.activeInHierarchy;
            }

        private static bool IsBetter(EFrameSceneCamera candidate, int candidateOrder, EFrameSceneCamera current, int currentOrder)
        {
            if (candidate.Priority != current.Priority)
            {
                return candidate.Priority > current.Priority;
            }

            var candidateCamera = candidate.Camera;
            var currentCamera = current.Camera;
            if (candidateCamera != null && currentCamera != null && !Mathf.Approximately(candidateCamera.depth, currentCamera.depth))
            {
                return candidateCamera.depth > currentCamera.depth;
            }

            return candidateOrder > currentOrder;
        }

        private void BindToBaseCamera(Camera baseCamera)
        {
            if (baseCamera == null || m_uiCamera == null)
            {
                return;
            }

            var uiCameraData = m_uiCamera.GetUniversalAdditionalCameraData();
            uiCameraData.renderType = CameraRenderType.Overlay;

            var baseCameraData = baseCamera.GetUniversalAdditionalCameraData();
            baseCameraData.renderType = CameraRenderType.Base;
            RemoveUICameraFromStack(baseCamera);
            baseCameraData.cameraStack.Add(m_uiCamera);
        }

        private void ConfigureStandaloneUICamera()
        {
            if (m_uiCamera == null)
            {
                return;
            }

            var uiCameraData = m_uiCamera.GetUniversalAdditionalCameraData();
            uiCameraData.renderType = CameraRenderType.Base;
        }

        private bool IsStackValid(Camera baseCamera)
        {
            if (baseCamera == null || m_uiCamera == null)
            {
                return false;
            }

            var baseCameraData = baseCamera.GetUniversalAdditionalCameraData();
            return baseCameraData.renderType == CameraRenderType.Base
                && baseCameraData.cameraStack.Contains(m_uiCamera)
                && m_uiCamera.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Overlay;
        }

        private void RemoveUICameraFromStack(Camera baseCamera)
        {
            if (baseCamera == null || m_uiCamera == null)
            {
                return;
            }

            var baseCameraData = baseCamera.GetUniversalAdditionalCameraData();
            for (int i = baseCameraData.cameraStack.Count - 1; i >= 0; i--)
            {
                var stackedCamera = baseCameraData.cameraStack[i];
                if (stackedCamera == null || stackedCamera == m_uiCamera)
                {
                    baseCameraData.cameraStack.RemoveAt(i);
                }
            }
        }

        private static void UpdateContextSceneCamera(Camera sceneCamera)
        {
            var component = EFrame.Current?.Component;
            if (component != null)
            {
                component.SceneCamera = sceneCamera;
            }
        }
    }
}
