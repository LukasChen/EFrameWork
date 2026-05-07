using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EFramework.Editor.AILoop
{
    internal static class EFrameGameViewBridge
    {
        private static Type s_gameViewType;
        private static FieldInfo s_renderTextureField;
        private static bool s_resolved;

        public static RenderTexture GetRenderTexture()
        {
            ResolveMembers();
            if (s_gameViewType == null || s_renderTextureField == null)
            {
                return null;
            }

            EditorWindow gameView = FindGameView();
            return gameView == null ? null : s_renderTextureField.GetValue(gameView) as RenderTexture;
        }

        private static EditorWindow FindGameView()
        {
            UnityEngine.Object[] gameViews = Resources.FindObjectsOfTypeAll(s_gameViewType);
            foreach (UnityEngine.Object candidate in gameViews)
            {
                if (candidate is EditorWindow window && window.hasFocus)
                {
                    return window;
                }
            }

            return gameViews.Length > 0 ? gameViews[0] as EditorWindow : null;
        }

        private static void ResolveMembers()
        {
            if (s_resolved)
            {
                return;
            }

            s_resolved = true;
            s_gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            s_renderTextureField = s_gameViewType?.GetField(
                "m_RenderTexture",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
