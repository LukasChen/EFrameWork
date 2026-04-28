#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace EFrameWork.Editor.ProjectBootstrap
{
    internal static class EFrameAddressablesBootstrapUtility
    {
        internal const string AppResRootPath = "Assets/App/Res";
        internal const string AppLocalGroupName = "App Local Group";
        private const string AppResLabel = "app-res";

        internal static bool AreAddressablesInitialized()
        {
            return AddressableAssetSettingsDefaultObject.GetSettings(false) != null;
        }

        internal static bool IsAppResAddressablesReady()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            return settings != null && settings.FindGroup(AppLocalGroupName) != null;
        }

        internal static bool EnsureAddressablesInitialized(out string message)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings != null)
            {
                message = "Addressables is already initialized.";
                return true;
            }

            settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                message = "Failed to create Addressables settings.";
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            message = "Initialized Addressables settings under Assets/AddressableAssetsData.";
            return true;
        }

        internal static bool EnsureAppResAddressablesGroup(out string message)
        {
            EnsureFolderExists(AppResRootPath);
            return SyncAppResAssets(null, out message);
        }

        internal static bool SyncImportedAssets(IEnumerable<string> assetPaths, out string message)
        {
            return SyncAppResAssets(assetPaths, out message);
        }

        private static bool SyncAppResAssets(IEnumerable<string> assetPaths, out string message)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
            {
                message = "Initialize Addressables before configuring App/Res groups.";
                return false;
            }

            var group = settings.FindGroup(AppLocalGroupName);
            if (group == null)
            {
                group = settings.CreateGroup(AppLocalGroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            var targetPaths = assetPaths == null
                ? CollectAllAssetPaths()
                : FilterAssetPaths(assetPaths);

            var registeredCount = 0;

            foreach (var assetPath in targetPaths)
            {
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (mainAssetType == typeof(DefaultAsset) || mainAssetType == typeof(MonoScript))
                {
                    continue;
                }

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry == null)
                {
                    continue;
                }

                entry.address = BuildAppResAddress(assetPath);
                entry.SetLabel(AppResLabel, true, true, false);
                registeredCount++;
            }

            AssetDatabase.SaveAssets();
            message = $"App/Res Addressables ready. Group: {AppLocalGroupName}, entries updated: {registeredCount}.";
            return true;
        }

        private static List<string> CollectAllAssetPaths()
        {
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { AppResRootPath });
            var assetPaths = new List<string>(guids.Length);

            foreach (var guid in guids)
            {
                assetPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            return assetPaths;
        }

        private static List<string> FilterAssetPaths(IEnumerable<string> assetPaths)
        {
            var filtered = new List<string>();

            foreach (var assetPath in assetPaths)
            {
                if (assetPath.StartsWith(AppResRootPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add(assetPath);
                }
            }

            return filtered;
        }

        private static void EnsureFolderExists(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var segments = assetPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        private static string BuildAppResAddress(string assetPath)
        {
            var relativePath = assetPath.StartsWith(AppResRootPath + "/", StringComparison.OrdinalIgnoreCase)
                ? assetPath.Substring(AppResRootPath.Length + 1)
                : Path.GetFileName(assetPath);

            var extension = Path.GetExtension(relativePath);
            if (!string.IsNullOrEmpty(extension))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - extension.Length);
            }

            return relativePath.Replace('\\', '/');
        }
    }
}
#endif