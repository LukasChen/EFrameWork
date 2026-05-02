#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EFrame.Editor.Tools
{
    public static class EFrameDeveloperTools
    {
        private static SceneAsset m_originalStartScene;

        public const string TestScenePath = "Assets/Scenes/BattleTestStartUp.unity";

        [MenuItem("EFrame Tools/从开始场景 启动游戏", false, 0)]
        public static void StartUp()
        {
            // 保存当前的启动场景
            m_originalStartScene = EditorSceneManager.playModeStartScene;

            // 从 buildSettings 中获取第一个场景作为启动场景
            SceneAsset startUpScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
            // 设置启动场景
            EditorSceneManager.playModeStartScene = startUpScene;

            // 运行
            EditorApplication.isPlaying = true;
        }

        [MenuItem("EFrame Tools/清档 启动游戏", false, 1)]
        public static void StartUpNewGame()
        {
            ClearDataPath();
            StartUp();
        }

        [MenuItem("EFrame Tools/Open Persistent Data Path")]
        private static void OpenDataPath()
        {
            // 获取到 Application.persistentDataPath 的路径
            string path = Application.persistentDataPath;
            // 打开这个路径
            System.Diagnostics.Process.Start(path);
        }

        [MenuItem("EFrame Tools/清除存档")]
        private static void ClearDataPath()
        {
            // 获取到 Application.persistentDataPath 的路径
            string path = Application.persistentDataPath;
            // 删除这个路径下的所有文件和文件夹
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(path);
            foreach (System.IO.FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            Debug.Log("已清除存档");
        }

        public static void TestStartUp()
        {
            // 保存当前的启动场景
            m_originalStartScene = EditorSceneManager.playModeStartScene;
            // 从指定路径获取测试场景
            SceneAsset startUpScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath);
            // 设置启动场景
            EditorSceneManager.playModeStartScene = startUpScene;
            // 运行
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void RestoreStartScene()
        {
            // 监听 PlayMode 状态变化
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    // 恢复启动场景
                    EditorSceneManager.playModeStartScene = m_originalStartScene;
                    m_originalStartScene = null;
                }
            };
        }
    }
}

#endif
