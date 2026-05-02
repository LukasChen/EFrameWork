#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace EFramework.Editor.ProjectBootstrap
{
    internal sealed class EFrameAppResAddressablePostprocessor : AssetPostprocessor
    {
        private static bool s_syncScheduled;
        private static bool s_requiresFullSync;
        private static readonly HashSet<string> s_pendingImportedAssets = new(StringComparer.OrdinalIgnoreCase);

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var hasImportedManagedAssets = CollectManagedAssetChanges(importedAssets, s_pendingImportedAssets);
            var hasStructuralManagedChanges = ContainsManagedAssetChanges(deletedAssets)
                || ContainsManagedAssetChanges(movedAssets)
                || ContainsManagedAssetChanges(movedFromAssetPaths);

            if (!hasImportedManagedAssets && !hasStructuralManagedChanges)
            {
                return;
            }

            s_requiresFullSync |= hasStructuralManagedChanges;

            if (s_syncScheduled)
            {
                return;
            }

            s_syncScheduled = true;
            EditorApplication.delayCall += FlushPendingAssets;
        }

        private static bool ContainsManagedAssetChanges(IEnumerable<string> assetPaths)
        {
            if (assetPaths == null)
            {
                return false;
            }

            foreach (var assetPath in assetPaths)
            {
                if (EFrameAddressablesBootstrapUtility.IsManagedAssetChangePath(assetPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CollectManagedAssetChanges(IEnumerable<string> assetPaths, ISet<string> managedAssetPaths)
        {
            if (assetPaths == null)
            {
                return false;
            }

            var hasManagedAssetChanges = false;
            foreach (var assetPath in assetPaths)
            {
                if (!EFrameAddressablesBootstrapUtility.IsManagedAssetChangePath(assetPath))
                {
                    continue;
                }

                managedAssetPaths.Add(assetPath);
                hasManagedAssetChanges = true;
            }

            return hasManagedAssetChanges;
        }

        private static void FlushPendingAssets()
        {
            s_syncScheduled = false;

            var importedAssets = new List<string>(s_pendingImportedAssets).ToArray();
            var requiresFullSync = s_requiresFullSync;
            s_pendingImportedAssets.Clear();
            s_requiresFullSync = false;

            if (!EFrameAddressablesBootstrapUtility.SyncImportedAssets(requiresFullSync ? null : importedAssets, out var message))
            {
                UnityEngine.Debug.LogError($"[EFrame Addressables] Auto sync failed. {message}");
                return;
            }

            UnityEngine.Debug.Log($"[EFrame Addressables] Auto sync complete. {message}");
        }
    }
}
#endif
