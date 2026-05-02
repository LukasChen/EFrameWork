#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using EFrame.Runtime.UI;
using UnityEditor;

namespace EFrame.Editor.ProjectBootstrap
{
    internal static class EFrameUIBootstrapUtility
    {
        private const string TagManagerAssetPath = "ProjectSettings/TagManager.asset";

        [MenuItem("EFrame Tools/自动创建 UI SortingLayer层级", false, 20)]
        private static void EnsureUISortingLayersMenu()
        {
            var success = EnsureUISortingLayers(out var message);
            if (success)
            {
                UnityEngine.Debug.Log(message);
            }
            else
            {
                UnityEngine.Debug.LogError(message);
            }
        }

        internal static bool AreUISortingLayersReady()
        {
            return TryGetMissingSortingLayers(out _);
        }

        internal static bool EnsureUISortingLayers(out string message)
        {
            if (!TryGetSortingLayersProperty(out var tagManager, out var sortingLayers, out message))
            {
                return false;
            }

            if (TryGetMissingSortingLayers(out var missingLayers))
            {
                message = "UI sorting layers are already configured.";
                return true;
            }

            var nextUniqueId = GetNextSortingLayerUniqueId(sortingLayers);
            foreach (var layerName in missingLayers)
            {
                sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
                var newLayer = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
                newLayer.FindPropertyRelative("name").stringValue = layerName;
                newLayer.FindPropertyRelative("uniqueID").intValue = nextUniqueId++;
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            message = $"Created UI sorting layers: {string.Join(", ", missingLayers)}.";
            return true;
        }

        private static bool TryGetMissingSortingLayers(out List<string> missingLayers)
        {
            missingLayers = new List<string>();

            if (!TryGetSortingLayersProperty(out _, out var sortingLayers, out _))
            {
                return false;
            }

            var configuredLayers = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < sortingLayers.arraySize; index++)
            {
                var existingLayer = sortingLayers.GetArrayElementAtIndex(index);
                var name = existingLayer.FindPropertyRelative("name").stringValue;
                if (!string.IsNullOrEmpty(name))
                {
                    configuredLayers.Add(name);
                }
            }

            foreach (var layerName in Enum.GetNames(typeof(UILayer)))
            {
                if (!configuredLayers.Contains(layerName))
                {
                    missingLayers.Add(layerName);
                }
            }

            return missingLayers.Count == 0;
        }

        private static bool TryGetSortingLayersProperty(out SerializedObject tagManager, out SerializedProperty sortingLayers, out string message)
        {
            tagManager = null;
            sortingLayers = null;

            var assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerAssetPath);
            if (assets == null || assets.Length == 0)
            {
                message = $"Could not load {TagManagerAssetPath}.";
                return false;
            }

            tagManager = new SerializedObject(assets[0]);
            sortingLayers = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayers == null)
            {
                message = "Could not find sorting layers property in TagManager.asset.";
                return false;
            }

            message = null;
            return true;
        }

        private static int GetNextSortingLayerUniqueId(SerializedProperty sortingLayers)
        {
            var maxId = 0;
            for (var index = 0; index < sortingLayers.arraySize; index++)
            {
                var layer = sortingLayers.GetArrayElementAtIndex(index);
                var uniqueId = layer.FindPropertyRelative("uniqueID").intValue;
                if (uniqueId > maxId)
                {
                    maxId = uniqueId;
                }
            }

            return maxId + 1;
        }
    }
}
#endif