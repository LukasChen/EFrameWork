#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace EFrameWork.Editor.ProjectBootstrap
{
    public class EFrameAudioSetup : EditorWindow
    {
        private GUIStyle m_greenTextStyle;
        private GUIStyle m_redTextStyle;

        private void OnEnable()
        {
            m_redTextStyle = new GUIStyle();
            m_redTextStyle.normal.textColor = Color.red;

            m_greenTextStyle = new GUIStyle();
            m_greenTextStyle.normal.textColor = Color.green;
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("在Resources文件夹下创建一个AudioMixer资源,名字为EFrameAudioMixerSettings, 并新建一个 MUSIC 和 SFX ，以及 OVERLAP 三个组",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Check AudioMixer Settings"))
            {
                Repaint();
            }

            if (!EFrameAudioBootstrapUtility.IsAudioSetupReady())
            {
                GUILayout.Toggle(false, "");
                GUILayout.Label("EFrameAudioMixerSettings is not ready in Assets/Resources", m_redTextStyle);

                if (GUILayout.Button("Create EFrameAudioMixerSettings"))
                {
                    if (!EFrameAudioBootstrapUtility.EnsureAudioSetup(out var message))
                    {
                        Debug.LogError(message);
                        EditorUtility.DisplayDialog("错误", message, "确定");
                        return;
                    }
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Toggle(true, "");
                GUILayout.Label("EFrameAudioMixerSettings found in Resources folder", m_greenTextStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Toggle(true, "");
                GUILayout.Label("MUSIC Group ok.", m_greenTextStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Toggle(true, "");
                GUILayout.Label("SFX Group ok.", m_greenTextStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        [MenuItem("EFrame Tools/Audio Setup")]
        public static void ShowWindow()
        {
            GetWindow<EFrameAudioSetup>("Audio Setup");
        }
    }
}
#endif

