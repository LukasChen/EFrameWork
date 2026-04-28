#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace EFrameWork.Runtime.Utils
{
    public class EFrameAudioSetup : EditorWindow
    {
        private AudioMixer m_audioMixer;
        private GUIStyle m_greenTextStyle;
        private GUIStyle m_redTextStyle;

        private void OnEnable()
        {
            m_audioMixer = Resources.Load<AudioMixer>("EFrameAudioMixerSettings");
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

            if (GUILayout.Button("Check AudioMixer Settings")) m_audioMixer = Resources.Load<AudioMixer>("EFrameAudioMixerSettings");

            if (m_audioMixer == null)
            {
                GUILayout.Toggle(false, "");
                GUILayout.Label("EFrameAudioMixerSettings not found in Resources folder", m_redTextStyle);

                if (GUILayout.Button("Create EFrameAudioMixerSettings"))
                {
                    string sourcePath = "Packages/com.eframework.core/Editor/Settings/EFrameAudioMixerSettings.mixer";
                    string resourcesFolder = "Assets/Resources";
                    string destPath = resourcesFolder + "/EFrameAudioMixerSettings.mixer";

                    // Check if source file exists
                    if (!File.Exists(sourcePath))
                    {
                        Debug.LogError("源文件不存在: " + sourcePath);
                        EditorUtility.DisplayDialog("错误", "AudioMixer 源文件未找到: " + sourcePath, "确定");
                        return;
                    }

                    // Check if Resources folder exists, create if not
                    if (!AssetDatabase.IsValidFolder(resourcesFolder))
                    {
                        Debug.Log("正在创建 Resources 目录");
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }

                    // Copy the asset
                    AssetDatabase.CopyAsset(sourcePath, destPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    // Reload the mixer after copying
                    m_audioMixer = Resources.Load<AudioMixer>("EFrameAudioMixerSettings");
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
                if (m_audioMixer.FindMatchingGroups("MUSIC").Length == 0)
                {
                    GUILayout.Toggle(false, "");
                    GUILayout.Label("MUSIC Group not found!", m_redTextStyle);
                }
                else
                {
                    GUILayout.Toggle(true, "");
                    GUILayout.Label("MUSIC Group ok.", m_greenTextStyle);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (m_audioMixer.FindMatchingGroups("SFX").Length == 0)
                {
                    GUILayout.Toggle(false, "");
                    GUILayout.Label("SFX Group not found!", m_redTextStyle);
                }
                else
                {
                    GUILayout.Toggle(true, "");
                    GUILayout.Label("SFX Group ok.", m_greenTextStyle);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (m_audioMixer.FindMatchingGroups("OVERLAP").Length == 0)
                {
                    GUILayout.Toggle(false, "");
                    GUILayout.Label("OVERLAP Group not found!", m_redTextStyle);
                }
                else
                {
                    GUILayout.Toggle(true, "");
                    GUILayout.Label("OVERLAP Group ok.", m_greenTextStyle);
                }

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

