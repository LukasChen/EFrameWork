using EFrameWork.Runtime.Audio;
using UnityEditor;
using UnityEngine;

namespace EFrameWork.Editor.Audio
{
    /// <summary>
    /// 音效事件配置资源创建工具
    /// </summary>
    public static class AudioEventConfigCreator
    {
        private const string k_configAssetPath = AudioResourcePaths.EventConfigAssetPath;

        public static void CreateAudioEventConfig()
        {
            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<AudioEventConfigAsset>(k_configAssetPath);
            if (existing != null)
            {
                EditorUtility.DisplayDialog("已存在", $"配置文件已存在于:\n{k_configAssetPath}", "确定");
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            // Create new config
            var config = ScriptableObject.CreateInstance<AudioEventConfigAsset>();

            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(k_configAssetPath);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                string[] folders = directory.Replace("\\", "/").Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            AssetDatabase.CreateAsset(config, k_configAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log($"[AudioEventConfig] Created new config at {k_configAssetPath}");
            EditorUtility.DisplayDialog("创建成功", $"配置文件已创建:\n{k_configAssetPath}\n\n运行时将通过 Resources.Load 加载此资源。", "确定");
        }

        [MenuItem("EFrame Tools/Open Audio Event Editor")]
        public static void OpenAudioEventEditor()
        {
            AudioEventEditorWindow.ShowWindow();
        }
    }
}