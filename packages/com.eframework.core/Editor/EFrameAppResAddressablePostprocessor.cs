#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace EFrameWork.Editor.ProjectBootstrap
{
    internal sealed class EFrameAppResAddressablePostprocessor : AssetPostprocessor
    {
        private static readonly HashSet<string> PendingAssetPaths = new();
        private static bool s_syncScheduled;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            QueueAssetPaths(importedAssets);
            QueueAssetPaths(movedAssets);

            if (PendingAssetPaths.Count == 0 || s_syncScheduled)
            {
                return;
            }

            s_syncScheduled = true;
            EditorApplication.delayCall += FlushPendingAssets;
        }

        private static void QueueAssetPaths(IEnumerable<string> assetPaths)
        {
            foreach (var assetPath in assetPaths)
            {
                if (assetPath.StartsWith(EFrameAddressablesBootstrapUtility.AppResRootPath + "/", System.StringComparison.OrdinalIgnoreCase))
                {
                    PendingAssetPaths.Add(assetPath);
                }
            }
        }

        private static void FlushPendingAssets()
        {
            s_syncScheduled = false;

            if (PendingAssetPaths.Count == 0)
            {
                return;
            }

            var assetPaths = new List<string>(PendingAssetPaths);
            PendingAssetPaths.Clear();

            EFrameAddressablesBootstrapUtility.SyncImportedAssets(assetPaths, out _);
        }
    }
}
#endif