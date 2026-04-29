#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EFrameWork.Editor.ProjectBootstrap
{
    internal static class EFrameAddressablesSyncMenu
    {
        private const string MenuPath = "EFrame Tools/Addressables/Sync Groups And Generate ResPath";

        [MenuItem(MenuPath, false, 30)]
        private static void SyncGroupsAndGenerateResPath()
        {
            EditorUtility.DisplayProgressBar("EFrame Addressables", "Syncing managed groups and generating ResPath...", 0.5f);
            try
            {
                if (!EFrameAddressablesBootstrapUtility.SyncProjectAddressablesAndGenerateResPath(out var message))
                {
                    Debug.LogError($"[EFrame Addressables] {message}");
                    EditorUtility.DisplayDialog("EFrame Addressables Sync Failed", message, "OK");
                    return;
                }

                Debug.Log($"[EFrame Addressables] {message}");
                EditorUtility.DisplayDialog("EFrame Addressables Sync Complete", message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
#endif
