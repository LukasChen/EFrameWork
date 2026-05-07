using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace EFramework.Editor.AILoop
{
    internal static class EFrameEditorWindowCaptureUtility
    {
        public static EditorWindow FindWindowByName(string windowName)
        {
            if (string.IsNullOrWhiteSpace(windowName))
            {
                return null;
            }

            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (EditorWindow window in windows)
            {
                if (window.titleContent != null &&
                    string.Equals(window.titleContent.text, windowName, StringComparison.OrdinalIgnoreCase))
                {
                    return window;
                }
            }

            return null;
        }

        public static async Task<Texture2D> CaptureWindowAsync(
            EditorWindow window,
            float resolutionScale,
            CancellationToken cancellationToken)
        {
            if (window == null)
            {
                return null;
            }

            window.ShowTab();
            await EFrameAiLoopDelay.DelayFrames(2, cancellationToken);

            float scale = EditorGUIUtility.pixelsPerPoint;
            int width = Mathf.RoundToInt(window.position.width * scale);
            int height = Mathf.RoundToInt(window.position.height * scale);
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            RenderTextureDescriptor descriptor =
                new(width, height, RenderTextureFormat.ARGB32, 24);
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                descriptor.sRGB = false;
            }

            RenderTexture renderTexture = RenderTexture.GetTemporary(descriptor);
            try
            {
                if (!TryGrabEditorWindow(window, renderTexture))
                {
                    return null;
                }

                Texture2D texture = ReadTexture(renderTexture, width, height);
                return ApplyResolutionScaling(texture, resolutionScale);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        public static async Task<(Texture2D texture, int yOffset)> CaptureGameRenderingAsync(
            float resolutionScale,
            CancellationToken cancellationToken)
        {
            await EFrameAiLoopDelay.DelayFrames(2, cancellationToken);

            RenderTexture source = EFrameGameViewBridge.GetRenderTexture();
            if (source == null)
            {
                return (null, 0);
            }

            int yOffset = Mathf.RoundToInt(Handles.GetMainGameViewSize().y) - source.height;

            RenderTextureDescriptor descriptor =
                new(source.width, source.height, source.format, 0);
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                descriptor.sRGB = false;
            }

            RenderTexture flipped = RenderTexture.GetTemporary(descriptor);
            try
            {
                Graphics.Blit(source, flipped, new Vector2(1f, -1f), new Vector2(0f, 1f));
                Texture2D texture = ReadTexture(flipped, source.width, source.height);
                return (ApplyResolutionScaling(texture, resolutionScale), yOffset);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(flipped);
            }
        }

        private static bool TryGrabEditorWindow(EditorWindow window, RenderTexture destination)
        {
            FieldInfo parentField = typeof(EditorWindow).GetField(
                "m_Parent",
                BindingFlags.Instance | BindingFlags.NonPublic);
            object parent = parentField?.GetValue(window);
            if (parent == null)
            {
                return false;
            }

            MethodInfo grabPixels = parent.GetType().GetMethod(
                "GrabPixels",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(RenderTexture), typeof(Rect) },
                null);
            if (grabPixels == null)
            {
                return false;
            }

            float scale = EditorGUIUtility.pixelsPerPoint;
            Rect rect = new(0f, 0f, window.position.width * scale, window.position.height * scale);
            grabPixels.Invoke(parent, new object[] { destination, rect });
            return true;
        }

        private static Texture2D ReadTexture(RenderTexture renderTexture, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            try
            {
                Texture2D texture = new(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static Texture2D ApplyResolutionScaling(Texture2D source, float scale)
        {
            if (source == null || Mathf.Approximately(scale, 1f))
            {
                return source;
            }

            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
            try
            {
                Graphics.Blit(source, renderTexture);
                UnityEngine.Object.DestroyImmediate(source);
                return ReadTexture(renderTexture, width, height);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }
    }
}
