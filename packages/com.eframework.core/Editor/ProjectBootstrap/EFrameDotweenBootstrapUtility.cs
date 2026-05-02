#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EFramework.Editor.ProjectBootstrap
{
    internal static class EFrameDotweenBootstrapUtility
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string DotweenSettingsAssetPath = ResourcesFolderPath + "/DOTweenSettings.asset";
        private const string DotweenRuntimeTypeName = "DG.Tweening.DOTween";
        private const string DotweenSettingsTypeName = "DG.Tweening.Core.DOTweenSettings";
        private const string DotweenAdapterDefine = "EFRAME_USE_DOTWEEN";

        [InitializeOnLoadMethod]
        private static void AutoBootstrapOnLoad()
        {
            EditorApplication.delayCall += RunAutoBootstrap;
        }

        internal static bool IsDotweenInstalled()
        {
            return ResolveRuntimeType() != null || ResolveSettingsType() != null;
        }

        internal static bool IsDotweenAdapterEnabled()
        {
            return HasScriptingDefine(DotweenAdapterDefine);
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

        internal static string GetTweenBackendStatus()
        {
            if (IsDotweenAdapterEnabled())
            {
                return IsDotweenInstalled()
                    ? "DOTween adapter enabled. Runtime tween backend will switch to DOTween after recompilation."
                    : "DOTween adapter define is enabled, but DOTween was not detected. Disable the adapter or install DOTween.";
            }

            return IsDotweenInstalled()
                ? "Fallback tween backend is active. DOTween was detected and can be enabled as an optional adapter."
                : "Fallback tween backend is active. DOTween is optional.";
        }

        internal static bool EnsureDotweenSetup(out string message)
        {
            var settingsType = ResolveSettingsType();
            if (settingsType == null)
            {
                message = "DOTween runtime assembly was not found. EFrame fallback tween backend remains active.";
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

        internal static bool EnsureOptionalDotweenSetup(out string message)
        {
            if (!IsDotweenInstalled())
            {
                message = "DOTween was not detected. EFrame fallback tween backend remains active.";
                return true;
            }

            return EnsureDotweenSetup(out message);
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
            return "EFrame includes a fallback tween backend. Install DOTween only when the project wants DOTween as the runtime tween backend, then enable the EFrame DOTween adapter from this window.";
        }

        internal static bool EnableDotweenAdapter(out string message)
        {
            if (!IsDotweenInstalled())
            {
                message = "DOTween was not detected. Install DOTween in the project Assets before enabling the adapter.";
                return false;
            }

            if (!EnsureDotweenSetup(out var setupMessage))
            {
                message = setupMessage;
                return false;
            }

            if (HasScriptingDefine(DotweenAdapterDefine))
            {
                message = $"DOTween adapter is already enabled. {setupMessage}";
                return true;
            }

            AddScriptingDefine(DotweenAdapterDefine);
            message = $"Enabled DOTween adapter with scripting define {DotweenAdapterDefine}. Unity will recompile. {setupMessage}";
            return true;
        }

        internal static bool DisableDotweenAdapter(out string message)
        {
            if (!HasScriptingDefine(DotweenAdapterDefine))
            {
                message = "DOTween adapter is already disabled. EFrame fallback tween backend will be used.";
                return true;
            }

            RemoveScriptingDefine(DotweenAdapterDefine);
            message = $"Disabled DOTween adapter by removing scripting define {DotweenAdapterDefine}. Unity will recompile and use the fallback tween backend.";
            return true;
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

        private static Type ResolveRuntimeType()
        {
            return ResolveType(DotweenRuntimeTypeName);
        }

        private static Type ResolveSettingsType()
        {
            return ResolveType(DotweenSettingsTypeName);
        }

        private static Type ResolveType(string fullName)
        {
            var directType = Type.GetType(fullName, throwOnError: false);
            if (directType != null)
            {
                return directType;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool HasScriptingDefine(string define)
        {
            var symbols = GetScriptingDefineSymbols();
            return Array.Exists(SplitScriptingDefineSymbols(symbols), symbol => symbol == define);
        }

        private static void AddScriptingDefine(string define)
        {
            var symbols = SplitScriptingDefineSymbols(GetScriptingDefineSymbols());
            if (Array.Exists(symbols, symbol => symbol == define))
            {
                return;
            }

            var newSymbols = string.IsNullOrWhiteSpace(GetScriptingDefineSymbols())
                ? define
                : $"{GetScriptingDefineSymbols()};{define}";
            SetScriptingDefineSymbols(newSymbols);
        }

        private static void RemoveScriptingDefine(string define)
        {
            var symbols = SplitScriptingDefineSymbols(GetScriptingDefineSymbols());
            var keptSymbols = Array.FindAll(symbols, symbol => symbol != define);
            SetScriptingDefineSymbols(string.Join(";", keptSymbols));
        }

        private static string[] SplitScriptingDefineSymbols(string symbols)
        {
            return string.IsNullOrWhiteSpace(symbols)
                ? Array.Empty<string>()
                : symbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetScriptingDefineSymbols()
        {
            return PlayerSettings.GetScriptingDefineSymbols(GetCurrentNamedBuildTarget());
        }

        private static void SetScriptingDefineSymbols(string symbols)
        {
            PlayerSettings.SetScriptingDefineSymbols(GetCurrentNamedBuildTarget(), symbols);
        }

        private static NamedBuildTarget GetCurrentNamedBuildTarget()
        {
            return NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
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
