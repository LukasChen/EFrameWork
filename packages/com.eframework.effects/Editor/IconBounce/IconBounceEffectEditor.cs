using UnityEditor;
using UnityEngine;
using EFramework.Extensions.Effects.IconBounce;

namespace EFramework.Extensions.Effects.Editor.IconBounce
{
    [CustomEditor(typeof(IconBounceEffect))]
    public sealed class IconBounceEffectEditor : UnityEditor.Editor
    {
        private IconBounceEffect m_target;
        private bool m_isPreviewing;

        private void OnEnable()
        {
            m_target = (IconBounceEffect)target;
        }

        private void OnDisable()
        {
            if (m_isPreviewing && m_target != null)
            {
                m_target.EditorStopPreview();
                m_isPreviewing = false;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Editor Controls", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !m_isPreviewing;
                if (GUILayout.Button("▶ Preview Bounce", GUILayout.Height(30)))
                {
                    m_target.EditorPreview();
                    m_isPreviewing = true;
                    EditorApplication.update += CheckPreviewEnd;
                }

                GUI.enabled = m_isPreviewing;
                if (GUILayout.Button("■ Stop Preview", GUILayout.Height(30)))
                {
                    m_target.EditorStopPreview();
                    m_isPreviewing = false;
                }
                GUI.enabled = true;
            }

            if (m_isPreviewing)
            {
                EditorGUILayout.HelpBox("Preview is playing...", MessageType.Info);
                Repaint();
            }

            EditorGUILayout.Space(5);

            // 曲线预览提示
            EditorGUILayout.HelpBox(
                "Bounce Curve 说明:\n" +
                "• X轴: 时间 (0-1)\n" +
                "• Y轴: 缩放偏移量乘数\n" +
                "• 正值 = 放大, 负值 = 缩小\n" +
                "• 建议起点和终点都为0，形成阻尼振荡效果",
                MessageType.None);
        }

        private void CheckPreviewEnd()
        {
            if (m_target == null || !m_isPreviewing)
            {
                EditorApplication.update -= CheckPreviewEnd;
                m_isPreviewing = false;
                return;
            }

            // 检查预览是否结束（通过检查缩放是否恢复）
            Repaint();
        }
    }
}
