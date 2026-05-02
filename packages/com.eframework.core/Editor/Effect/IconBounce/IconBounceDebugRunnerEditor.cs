using UnityEditor;
using UnityEngine;
using EFramework.Runtime.Effect.IconBounce;

namespace EFramework.Editor.Effect.IconBounce
{
    [CustomEditor(typeof(IconBounceDebugRunner))]
    public sealed class IconBounceDebugRunnerEditor : UnityEditor.Editor
    {
        private IconBounceDebugRunner m_target;

        private void OnEnable()
        {
            m_target = (IconBounceDebugRunner)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ Play Bounce", GUILayout.Height(35)))
                {
                    m_target.Play();
                }

                if (GUILayout.Button("■ Stop", GUILayout.Height(35)))
                {
                    m_target.Stop();
                }
            }

            EditorGUILayout.Space(5);

            // 显示目标信息
            Transform target = m_target.Target != null ? m_target.Target : m_target.transform;
            EditorGUILayout.HelpBox($"Target: {target.name}", MessageType.Info);

            // 显示配置信息
            if (m_target.Config != null)
            {
                EditorGUILayout.HelpBox("Using ScriptableObject config.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Using inline configuration.", MessageType.None);
            }
        }
    }
}
