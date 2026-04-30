#if UNITY_EDITOR
using UnityEditor;

namespace EFrameWork.Editor.ProjectBootstrap
{
    internal static class EFrameAddressablesSyncMenu
    {
        private const string MenuPath = "EFrame Tools/Addressables/Sync Groups And Generate ResPath";

        [MenuItem(MenuPath, false, 30)]
        private static void SyncGroupsAndGenerateResPath()
        {
            EFrameAddressablesReportWindow.Open();
        }
    }
}
#endif
