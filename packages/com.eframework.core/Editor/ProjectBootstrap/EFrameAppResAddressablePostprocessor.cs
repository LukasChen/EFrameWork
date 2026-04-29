#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace EFrameWork.Editor.ProjectBootstrap
{
    internal sealed class EFrameAppResAddressablePostprocessor : AssetPostprocessor
    {
        private static bool s_syncScheduled;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!ContainsManagedAssetChanges(importedAssets)
                && !ContainsManagedAssetChanges(deletedAssets)
                && !ContainsManagedAssetChanges(movedAssets)
                && !ContainsManagedAssetChanges(movedFromAssetPaths))
            {
                return;
            }

            if (s_syncScheduled)
            {
                return;
            }

            s_syncScheduled = true;
            EditorApplication.delayCall += FlushPendingAssets;
        }

        private static bool ContainsManagedAssetChanges(IEnumerable<string> assetPaths)
        {
            foreach (var assetPath in assetPaths)
            {
                if (assetPath.StartsWith(EFrameAddressablesBootstrapUtility.AppResRootPath + "/", System.StringComparison.OrdinalIgnoreCase)
                    || assetPath.StartsWith(EFrameAddressablesBootstrapUtility.ProjectScenesRootPath + "/", System.StringComparison.OrdinalIgnoreCase)
                    || assetPath.StartsWith(EFrameAddressablesBootstrapUtility.ModulesRootPath + "/", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void FlushPendingAssets()
        {
            s_syncScheduled = false;

            EFrameAddressablesBootstrapUtility.SyncImportedAssets(null, out _);
        }
    }
}
#endif