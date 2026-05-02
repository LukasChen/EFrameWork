using EFramework.Extensions.Effects.Fly;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace EFramework.Extensions.Effects.Editor.Fly
{
    [CustomEditor(typeof(FlyAnimationConfig))]
    public sealed class FlyAnimationConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty m_prefabAsset;
        private SerializedProperty m_prefabAssetId;

        private SerializedProperty m_pathType;
        private SerializedProperty m_duration;
        private SerializedProperty m_moveProgress;
        private SerializedProperty m_scaleOverLife;
        private SerializedProperty m_alphaOverLife;
        private SerializedProperty m_startRandomRadius;
        private SerializedProperty m_rotateSpeedDeg;

        // ScatterThenFly
        private SerializedProperty m_scatterDuration;
        private SerializedProperty m_scatterMoveProgress;
        private SerializedProperty m_scatterHoldDuration;
        private SerializedProperty m_scatterFlyControl1;
        private SerializedProperty m_scatterFlyControl2;

        private SerializedProperty m_quadOffset;
        private SerializedProperty m_cubic1;
        private SerializedProperty m_cubic2;

        private SerializedProperty m_baseInterval;
        private SerializedProperty m_intervalMul;
        private SerializedProperty m_maxVisual;

        private SerializedProperty m_flySoundMode;
        private SerializedProperty m_flySoundAsset;
        private SerializedProperty m_flySoundAssetId;
        private SerializedProperty m_arriveSoundMode;
        private SerializedProperty m_arriveSoundAsset;
        private SerializedProperty m_arriveSoundAssetId;

        private SerializedProperty m_startEffect;
        private SerializedProperty m_startEffectAssetId;
        private SerializedProperty m_startEffectDestroyTime;
        private SerializedProperty m_endEffect;
        private SerializedProperty m_endEffectAssetId;
        private SerializedProperty m_endEffectDestroyTime;

        private void OnEnable()
        {
            m_prefabAsset = serializedObject.FindProperty("m_prefabAsset");
            m_prefabAssetId = serializedObject.FindProperty("m_prefabAssetId");

            m_pathType = serializedObject.FindProperty("PathType");
            m_duration = serializedObject.FindProperty("Duration");
            m_moveProgress = serializedObject.FindProperty("MoveProgress");
            m_scaleOverLife = serializedObject.FindProperty("ScaleOverLife");
            m_alphaOverLife = serializedObject.FindProperty("AlphaOverLife");
            m_startRandomRadius = serializedObject.FindProperty("StartRandomRadius");
            m_rotateSpeedDeg = serializedObject.FindProperty("RotateSpeedDeg");

            // ScatterThenFly
            m_scatterDuration = serializedObject.FindProperty("ScatterDuration");
            m_scatterMoveProgress = serializedObject.FindProperty("ScatterMoveProgress");
            m_scatterHoldDuration = serializedObject.FindProperty("ScatterHoldDuration");
            m_scatterFlyControl1 = serializedObject.FindProperty("ScatterFlyControl1Offset");
            m_scatterFlyControl2 = serializedObject.FindProperty("ScatterFlyControl2Offset");

            m_quadOffset = serializedObject.FindProperty("QuadraticControlOffset");
            m_cubic1 = serializedObject.FindProperty("CubicControl1Offset");
            m_cubic2 = serializedObject.FindProperty("CubicControl2Offset");

            m_baseInterval = serializedObject.FindProperty("BaseInterval");
            m_intervalMul = serializedObject.FindProperty("IntervalMultiplier");
            m_maxVisual = serializedObject.FindProperty("MaxVisualCount");

            m_flySoundMode = serializedObject.FindProperty("FlySoundMode");
            m_flySoundAsset = serializedObject.FindProperty("m_flySoundAsset");
            m_flySoundAssetId = serializedObject.FindProperty("m_flySoundAssetId");
            m_arriveSoundMode = serializedObject.FindProperty("ArriveSoundMode");
            m_arriveSoundAsset = serializedObject.FindProperty("m_arriveSoundAsset");
            m_arriveSoundAssetId = serializedObject.FindProperty("m_arriveSoundAssetId");

            m_startEffect = serializedObject.FindProperty("m_startEffectAsset");
            m_startEffectAssetId = serializedObject.FindProperty("m_startEffectAssetId");
            m_startEffectDestroyTime = serializedObject.FindProperty("StartEffectDestroyTime");
            m_endEffect = serializedObject.FindProperty("m_endEffectAsset");
            m_endEffectAssetId = serializedObject.FindProperty("m_endEffectAssetId");
            m_endEffectDestroyTime = serializedObject.FindProperty("EndEffectDestroyTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawAssetBlock();
            EditorGUILayout.Space(6);

            DrawMotionBlock();
            EditorGUILayout.Space(6);

            DrawSequenceBlock();
            EditorGUILayout.Space(6);

            DrawAudioBlock();

            serializedObject.ApplyModifiedProperties();

            DrawWarnings();
        }

        private void DrawAssetBlock()
        {
            EditorGUILayout.LabelField("Asset", EditorStyles.boldLabel);
            DrawAuthoringAssetField(m_prefabAsset, m_prefabAssetId, "Prefab");
        }

        private void DrawMotionBlock()
        {
            EditorGUILayout.LabelField("Motion", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_pathType);
            EditorGUILayout.PropertyField(m_duration);
            EditorGUILayout.PropertyField(m_moveProgress);
            EditorGUILayout.PropertyField(m_scaleOverLife);
            EditorGUILayout.PropertyField(m_alphaOverLife);
            EditorGUILayout.PropertyField(m_startRandomRadius);
            EditorGUILayout.PropertyField(m_rotateSpeedDeg);

            var pathType = (FlyPathType)m_pathType.enumValueIndex;
            if (pathType == FlyPathType.QuadraticBezier)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(m_quadOffset);
            }
            else if (pathType == FlyPathType.CubicBezier)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(m_cubic1);
                EditorGUILayout.PropertyField(m_cubic2);
            }
            else if (pathType == FlyPathType.ScatterThenFly)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("ScatterThenFly Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(m_scatterDuration, new GUIContent("Scatter Duration", "散开阶段飞行时长（秒）"));
                EditorGUILayout.PropertyField(m_scatterMoveProgress, new GUIContent("Scatter Move Curve", "散开阶段移动曲线"));
                EditorGUILayout.PropertyField(m_scatterHoldDuration, new GUIContent("Hold Duration", "散开后停留时长（秒）"));
                
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Fly Path (CubicBezier)", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(m_scatterFlyControl1, new GUIContent("Control 1 Offset", "控制点1偏移（相对于散开点）"));
                EditorGUILayout.PropertyField(m_scatterFlyControl2, new GUIContent("Control 2 Offset", "控制点2偏移（相对于目标点）"));
            }
        }

        private void DrawSequenceBlock()
        {
            EditorGUILayout.LabelField("Sequence", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_baseInterval);
            EditorGUILayout.PropertyField(m_intervalMul);
            EditorGUILayout.PropertyField(m_maxVisual);
        }

        private void DrawAudioBlock()
        {
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_flySoundMode);
            if ((FlyAudioPlayMode)m_flySoundMode.enumValueIndex != FlyAudioPlayMode.None)
            {
                DrawAuthoringAssetField(m_flySoundAsset, m_flySoundAssetId, "Fly Sound");
            }

            EditorGUILayout.PropertyField(m_arriveSoundMode);
            if ((FlyAudioPlayMode)m_arriveSoundMode.enumValueIndex != FlyAudioPlayMode.None)
            {
                DrawAuthoringAssetField(m_arriveSoundAsset, m_arriveSoundAssetId, "Arrive Sound");
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Effect", EditorStyles.boldLabel);
            DrawAuthoringAssetField(m_startEffect, m_startEffectAssetId, "Start Effect");
            EditorGUILayout.PropertyField(m_startEffectDestroyTime);
            DrawAuthoringAssetField(m_endEffect, m_endEffectAssetId, "End Effect");
            EditorGUILayout.PropertyField(m_endEffectDestroyTime);
        }

        private static void DrawAuthoringAssetField(SerializedProperty assetProperty, SerializedProperty assetIdProperty, string label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(assetProperty, new GUIContent(label));
            if (EditorGUI.EndChangeCheck())
            {
                assetIdProperty.stringValue = ResolveAssetId(assetProperty.objectReferenceValue);
            }

            EditorGUILayout.PropertyField(assetIdProperty, new GUIContent($"{label} Asset Id"));

            if (assetProperty.objectReferenceValue != null && string.IsNullOrEmpty(assetIdProperty.stringValue))
            {
                EditorGUILayout.HelpBox($"{label} is not an EFrame managed Addressables asset. Move it under Assets/App/Res, Assets/Scenes, or Assets/Modules/*/Res and sync Addressables.", MessageType.Warning);
            }
        }

        private static string ResolveAssetId(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
            {
                return string.Empty;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var entry = !string.IsNullOrEmpty(guid) ? settings?.FindAssetEntry(guid) : null;
            if (entry != null && !string.IsNullOrEmpty(entry.address))
            {
                return entry.address;
            }

            return TryBuildManagedAddress(assetPath, out var address) ? address : string.Empty;
        }

        private static bool TryBuildManagedAddress(string assetPath, out string address)
        {
            assetPath = (assetPath ?? string.Empty).Replace('\\', '/');

            if (assetPath.StartsWith("Assets/App/Res/", System.StringComparison.OrdinalIgnoreCase))
            {
                address = BuildRelativeAddress(assetPath, "Assets/App/Res");
                return true;
            }

            if (assetPath.StartsWith("Assets/Scenes/", System.StringComparison.OrdinalIgnoreCase))
            {
                address = BuildRelativeAddress(assetPath, "Assets");
                return true;
            }

            if (assetPath.StartsWith("Assets/Modules/", System.StringComparison.OrdinalIgnoreCase))
            {
                var segments = assetPath.Split('/');
                if (segments.Length >= 4
                    && (string.Equals(segments[3], "Res", System.StringComparison.OrdinalIgnoreCase)
                        || string.Equals(segments[3], "Scenes", System.StringComparison.OrdinalIgnoreCase)))
                {
                    address = BuildRelativeAddress(assetPath, "Assets");
                    return true;
                }
            }

            address = string.Empty;
            return false;
        }

        private static string BuildRelativeAddress(string assetPath, string rootPath)
        {
            string relativePath = assetPath.StartsWith(rootPath + "/", System.StringComparison.OrdinalIgnoreCase)
                ? assetPath.Substring(rootPath.Length + 1)
                : System.IO.Path.GetFileName(assetPath);

            string extension = System.IO.Path.GetExtension(relativePath);
            if (!string.IsNullOrEmpty(extension))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - extension.Length);
            }

            return relativePath.Replace('\\', '/');
        }

        private void DrawWarnings()
        {
            var cfg = (FlyAnimationConfig)target;
            if (cfg == null) return;

            if (!cfg.HasValidAsset)
            {
                EditorGUILayout.HelpBox("Missing fly prefab asset id.", MessageType.Warning);
            }

            if (cfg.Duration < 0.01f)
            {
                EditorGUILayout.HelpBox("Duration 过小，已在 OnValidate 中自动钳制。", MessageType.Info);
            }

            if (cfg.MaxVisualCount < 1)
            {
                EditorGUILayout.HelpBox("MaxVisualCount 必须 >= 1。", MessageType.Info);
            }
        }
    }
}
