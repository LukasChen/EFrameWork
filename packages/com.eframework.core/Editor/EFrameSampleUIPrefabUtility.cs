#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace EFrameWork.Editor.ProjectBootstrap
{
    public static class EFrameSampleUIPrefabUtility
    {
        private const string SourceHomeViewPrefabPath = "Packages/com.eframework.core/Editor/Templates/UI/HomeView.prefab";
        private const string SourceSampleModuleViewPrefabPath = "Packages/com.eframework.core/Editor/Templates/UI/SampleModuleMainView.prefab";
        private const string SourceModuleViewTemplatePrefabPath = "Packages/com.eframework.core/Editor/Templates/UI/ModuleMainViewTemplate.prefab";
        private const string HomeViewPrefabPath = "Assets/App/Res/UI/Panels/Home/HomeView.prefab";
        private const string SampleModuleViewPrefabPath = "Assets/Modules/SampleModule/Res/UI/Panels/SampleModuleMain/SampleModuleMainView.prefab";

        public static bool AreBootstrapSampleUIPrefabsReady()
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(HomeViewPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SampleModuleViewPrefabPath) != null;
        }

        public static bool EnsureBootstrapSampleUIPrefabs(out string message)
        {
            EnsureFolderHierarchy("Assets/App/Res/UI/Panels/Home");
            EnsureFolderHierarchy("Assets/Modules/SampleModule/Res/UI/Panels/SampleModuleMain");

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SourceHomeViewPrefabPath) == null)
            {
                message = $"Sample UI template was not found: {SourceHomeViewPrefabPath}";
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SourceSampleModuleViewPrefabPath) == null)
            {
                message = $"Sample UI template was not found: {SourceSampleModuleViewPrefabPath}";
                return false;
            }

            var copiedHome = EnsureTemplateAsset(SourceHomeViewPrefabPath, HomeViewPrefabPath);
            if (!copiedHome.success)
            {
                message = copiedHome.message;
                return false;
            }

            var copiedSampleModule = EnsureTemplateAsset(SourceSampleModuleViewPrefabPath, SampleModuleViewPrefabPath);
            if (!copiedSampleModule.success)
            {
                message = copiedSampleModule.message;
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            message = copiedHome.copied || copiedSampleModule.copied
                ? "Copied sample UI prefab templates into project Assets."
                : "Sample UI prefab assets already exist in the project.";
            return true;
        }

        private static void EnsureFolderHierarchy(string assetPath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(fullPath);
        }

        private static (bool success, bool copied, string message) EnsureTemplateAsset(string sourceAssetPath, string targetAssetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetAssetPath) != null)
            {
                return (true, false, string.Empty);
            }

            if (!AssetDatabase.CopyAsset(sourceAssetPath, targetAssetPath))
            {
                return (false, false, $"Failed to copy sample UI template from {sourceAssetPath} to {targetAssetPath}.");
            }

            return (true, true, string.Empty);
        }

        public static bool EnsureModuleViewTemplate(string moduleName, out string message)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                message = "Module name is required before copying a module UI template.";
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SourceModuleViewTemplatePrefabPath) == null)
            {
                message = $"Module UI template was not found: {SourceModuleViewTemplatePrefabPath}";
                return false;
            }

            var targetFolder = $"Assets/Modules/{moduleName}/Res/UI/Panels/{moduleName}Main";
            var targetPrefabPath = $"{targetFolder}/{moduleName}MainView.prefab";
            EnsureFolderHierarchy(targetFolder);

            var copiedTemplate = EnsureTemplateAsset(SourceModuleViewTemplatePrefabPath, targetPrefabPath);
            if (!copiedTemplate.success)
            {
                message = copiedTemplate.message;
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            message = copiedTemplate.copied
                ? $"Copied module UI template to {targetPrefabPath}."
                : $"Module UI prefab already exists at {targetPrefabPath}.";
            return true;
        }
    }
}
#endif
