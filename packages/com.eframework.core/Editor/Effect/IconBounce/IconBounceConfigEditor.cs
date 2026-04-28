using UnityEditor;
using UnityEngine;
using EFrameWork.Runtime.Effect.IconBounce;

namespace EFrameWork.Editor.Effect.IconBounce
{
    [CustomEditor(typeof(IconBounceConfig))]
    public sealed class IconBounceConfigEditor : UnityEditor.Editor
    {
        private IconBounceConfig m_target;

        private void OnEnable()
        {
            m_target = (IconBounceConfig)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            
            // 曲线说明
            EditorGUILayout.HelpBox(
                "Bounce Curve 配置说明:\n" +
                "• X轴: 归一化时间 (0-1)\n" +
                "• Y轴: 缩放偏移量乘数 (最终偏移 = Y值 × BounceStrength)\n" +
                "• 正值使图标放大，负值使图标缩小\n" +
                "• 建议起点(0,0)和终点(1,0)，中间形成阻尼振荡\n\n" +
                "预设曲线建议:\n" +
                "• 弹性回弹: 0→1→-0.5→0.25→-0.1→0\n" +
                "• 强调冲击: 0→1.5→-0.3→0\n" +
                "• 平滑回落: 0→1→0 (使用EaseOut)",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // 预览曲线效果的计算示例
            EditorGUILayout.LabelField("Scale Preview at Key Points", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                float[] samplePoints = { 0f, 0.15f, 0.35f, 0.5f, 0.75f, 1f };
                foreach (float t in samplePoints)
                {
                    float scaleOffset = m_target.EvaluateScale(t);
                    float finalScale = 1f + scaleOffset;
                    EditorGUILayout.LabelField($"t={t:F2}", $"Scale: {finalScale:F3} ({scaleOffset:+0.000;-0.000})");
                }
            }
        }
    }
}
