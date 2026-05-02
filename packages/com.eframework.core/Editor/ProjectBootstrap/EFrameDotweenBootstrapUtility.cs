#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace EFramework.Editor.ProjectBootstrap
{
    internal static class EFrameDotweenBootstrapUtility
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string DotweenSettingsAssetPath = ResourcesFolderPath + "/DOTweenSettings.asset";
        private const string DotweenSettingsTypeName = "DG.Tweening.Core.DOTweenSettings, DOTween";

        [InitializeOnLoadMethod]
        private static void AutoBootstrapOnLoad()
        {
            EditorApplication.delayCall += RunAutoBootstrap;
        }

        internal static bool IsDotweenInstalled()
        {
            return ResolveSettingsType() != null;
        }

        internal static bool IsDotweenSetupReady()
        {
            var settingsType = ResolveSettingsType();
            if (settingsType == null)
            {
                return false;
            }

            var settingsAsset = AssetDatabase.LoadAssetAtPath(DotweenSettingsAssetPath, settingsType);
            return settingsAsset != null;
        }

        internal static bool EnsureDotweenSetup(out string message)
        {
            var settingsType = ResolveSettingsType();
            if (settingsType == null)
            {
                message = "DOTween runtime assembly was not found. Install DOTween into the consumer project's Assets before using EFrame tween-enabled components.";
                return false;
            }

            if (!typeof(ScriptableObject).IsAssignableFrom(settingsType))
            {
                message = $"DOTween settings type is not a ScriptableObject: {settingsType.FullName}";
                return false;
            }

            var existingSettings = AssetDatabase.LoadAssetAtPath(DotweenSettingsAssetPath, settingsType);
            if (existingSettings != null)
            {
                message = "DOTween settings are already initialized.";
                return true;
            }

            EnsureFolderExists(ResourcesFolderPath);

            var settingsAsset = ScriptableObject.CreateInstance(settingsType);
            if (settingsAsset == null)
            {
                message = "Failed to create DOTween settings asset instance.";
                return false;
            }

            AssetDatabase.CreateAsset(settingsAsset, DotweenSettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            existingSettings = AssetDatabase.LoadAssetAtPath(DotweenSettingsAssetPath, settingsType);
            if (existingSettings == null)
            {
                message = $"DOTween settings asset was not available after initialization: {DotweenSettingsAssetPath}";
                return false;
            }

            message = "Initialized DOTween settings under Assets/Resources/DOTweenSettings.asset.";
            return true;
        }

        internal static bool OpenDotweenSettings(out string message)
        {
            if (!EnsureDotweenSetup(out message))
            {
                return false;
            }

            var settingsType = ResolveSettingsType();
            if (settingsType == null)
            {
                message = "DOTween settings type could not be resolved after initialization.";
                return false;
            }

            var settingsAsset = AssetDatabase.LoadAssetAtPath(DotweenSettingsAssetPath, settingsType);
            if (settingsAsset == null)
            {
                message = $"DOTween settings asset was not found at {DotweenSettingsAssetPath}.";
                return false;
            }

            Selection.activeObject = settingsAsset;
            EditorGUIUtility.PingObject(settingsAsset);
            message = "Opened DOTween settings asset under Assets/Resources/DOTweenSettings.asset.";
            return true;
        }

        internal static string GetInstallationGuidance()
        {
            return "EFrame uses DOTween directly. Install DOTween in the consumer project's Assets before using tween-enabled framework components. DOTween Utility Panel module setup should target that project-local installation, not a package copy.";
        }

        private static void RunAutoBootstrap()
        {
            EditorApplication.delayCall -= RunAutoBootstrap;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunAutoBootstrap;
                return;
            }

            if (IsDotweenInstalled())
            {
                EnsureDotweenSetup(out _);
            }
        }

        private static Type ResolveSettingsType()
        {
            return Type.GetType(DotweenSettingsTypeName, throwOnError: false);
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
    }
}
#endif