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
                if (EFrameAddressablesBootstrapUtility.IsManagedAssetChangePath(assetPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static void FlushPendingAssets()
        {
            s_syncScheduled = false;

            if (!EFrameAddressablesBootstrapUtility.SyncImportedAssets(null, out var message))
            {
                UnityEngine.Debug.LogError($"[EFrame Addressables] Auto sync failed. {message}");
                return;
            }

            UnityEngine.Debug.Log($"[EFrame Addressables] Auto sync complete. {message}");
        }
    }
}
#endif
