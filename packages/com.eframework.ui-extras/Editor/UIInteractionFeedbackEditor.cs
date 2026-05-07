using System;
using EFramework.Extensions.UI.Extras.Interaction;
using EFramework.Runtime.Tween;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EFramework.Editor.UI.Extras.Interaction
{
    [CustomEditor(typeof(UIInteractionFeedback))]
    [CanEditMultipleObjects]
    public sealed class UIInteractionFeedbackEditor : UnityEditor.Editor
    {
        private static readonly string[] StateTabs = { "Enabled", "Hover", "Pressed", "Selected", "Disabled", "Focus" };
        private static readonly UIInteractionFeedbackStateMask[] StateMasks =
        {
            UIInteractionFeedbackStateMask.Normal,
            UIInteractionFeedbackStateMask.Hover,
            UIInteractionFeedbackStateMask.Pressed,
            UIInteractionFeedbackStateMask.Checked,
            UIInteractionFeedbackStateMask.Disabled,
            UIInteractionFeedbackStateMask.Focus
        };

        private SerializedProperty m_selectable;
        private SerializedProperty m_useTargetFeedback;
        private SerializedProperty m_autoSetupTargets;
        private SerializedProperty m_targets;
        private int m_removeIndex = -1;
        private int m_duplicateIndex = -1;

        private void OnEnable()
        {
            m_selectable = serializedObject.FindProperty("m_selectable");
            m_useTargetFeedback = serializedObject.FindProperty("m_useTargetFeedback");
            m_autoSetupTargets = serializedObject.FindProperty("m_autoSetupTargets");
            m_targets = serializedObject.FindProperty("m_targets");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_useTargetFeedback.boolValue = true;
            m_autoSetupTargets.boolValue = false;
            if (m_selectable.objectReferenceValue == null)
            {
                m_selectable.objectReferenceValue = ((UIInteractionFeedback)target).GetComponent<Selectable>();
            }

            EditorGUILayout.LabelField("Feedback Nodes", EditorStyles.boldLabel);
            DrawNodes();
            DrawFooter();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawNodes()
        {
            m_removeIndex = -1;
            m_duplicateIndex = -1;
            for (var i = 0; i < m_targets.arraySize; i++)
            {
                DrawNode(m_targets.GetArrayElementAtIndex(i), i);
                EditorGUILayout.Space(4f);
            }

            if (m_duplicateIndex >= 0 && m_duplicateIndex < m_targets.arraySize)
            {
                m_targets.InsertArrayElementAtIndex(m_duplicateIndex + 1);
            }

            if (m_removeIndex >= 0 && m_removeIndex < m_targets.arraySize)
            {
                m_targets.DeleteArrayElementAtIndex(m_removeIndex);
            }
        }

        private void DrawNode(SerializedProperty node, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var targetProperty = node.FindPropertyRelative("Target");
            var targetPathProperty = node.FindPropertyRelative("TargetPath");
            var foldoutProperty = node.FindPropertyRelative("Foldout");
            var root = ((UIInteractionFeedback)target).transform;
            ResolveTargetReference(targetProperty, targetPathProperty, root);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(foldoutProperty.boolValue ? "-" : "+", GUILayout.Width(24f)))
            {
                foldoutProperty.boolValue = !foldoutProperty.boolValue;
            }

            EditorGUILayout.LabelField($"Node {index + 1}", EditorStyles.boldLabel, GUILayout.Width(72f));
            DrawTargetDropdown(targetProperty, targetPathProperty);
            var previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.08f, 0.32f, 0.12f);
            if (GUILayout.Button("D", GUILayout.Width(26f)))
            {
                m_duplicateIndex = index;
            }

            GUI.backgroundColor = new Color(0.65f, 0.12f, 0.12f);
            if (GUILayout.Button("-", GUILayout.Width(26f)))
            {
                m_removeIndex = index;
            }
            GUI.backgroundColor = previousBackgroundColor;
            EditorGUILayout.EndHorizontal();

            var targetTransform = targetProperty.objectReferenceValue as RectTransform;
            if (foldoutProperty.boolValue && targetTransform != null)
            {
                EditorGUILayout.Space(6f);
                DrawStateEditor(node, targetTransform);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTargetDropdown(SerializedProperty targetProperty, SerializedProperty targetPathProperty)
        {
            var root = ((UIInteractionFeedback)target).transform;
            var current = targetProperty.objectReferenceValue as RectTransform;
            var label = current != null ? GetFullPath(current.transform, root) : "Select Target";
            if (GUILayout.Button(label, EditorStyles.popup))
            {
                var menu = new GenericMenu();
                AddTransformMenuItem(menu, root, root, targetProperty, targetPathProperty);
                for (var i = 0; i < root.childCount; i++)
                {
                    AddTransformTree(menu, root.GetChild(i), root, targetProperty, targetPathProperty);
                }

                menu.ShowAsContext();
            }
        }

        private static void AddTransformTree(GenericMenu menu, Transform transform, Transform root, SerializedProperty targetProperty, SerializedProperty targetPathProperty)
        {
            AddTransformMenuItem(menu, transform, root, targetProperty, targetPathProperty);
            for (var i = 0; i < transform.childCount; i++)
            {
                AddTransformTree(menu, transform.GetChild(i), root, targetProperty, targetPathProperty);
            }
        }

        private static void AddTransformMenuItem(GenericMenu menu, Transform transform, Transform root, SerializedProperty targetProperty, SerializedProperty targetPathProperty)
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            var path = GetMenuPath(transform, root);
            menu.AddItem(new GUIContent(path), targetProperty.objectReferenceValue == rectTransform, () =>
            {
                targetProperty.serializedObject.Update();
                targetProperty.objectReferenceValue = rectTransform;
                targetPathProperty.stringValue = GetRelativePath(transform, root);
                targetProperty.serializedObject.ApplyModifiedProperties();
            });
        }

        private void DrawStateEditor(SerializedProperty node, RectTransform targetTransform)
        {
            var selectedTabProperty = node.FindPropertyRelative("SelectedTab");
            var actions = node.FindPropertyRelative("Actions");
            var tabLabels = BuildStateTabLabels(actions);
            selectedTabProperty.intValue = Mathf.Clamp(selectedTabProperty.intValue, 0, StateTabs.Length - 1);
            selectedTabProperty.intValue = GUILayout.Toolbar(selectedTabProperty.intValue, tabLabels, GUILayout.Height(28f));

            var stateMask = StateMasks[selectedTabProperty.intValue];
            var actionIndex = FindAction(actions, stateMask);
            var action = actionIndex >= 0 ? actions.GetArrayElementAtIndex(actionIndex) : null;
            var currentType = action != null ? (UIInteractionFeedbackActionType)action.FindPropertyRelative("Type").enumValueIndex : UIInteractionFeedbackActionType.None;
            var availableTypes = GetAvailableTypes(targetTransform);
            var selectedTypeIndex = Mathf.Max(0, Array.IndexOf(availableTypes, currentType));

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            var newTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, ToNames(availableTypes), GUILayout.Width(140f));
            var newType = availableTypes[newTypeIndex];
            if (newType != currentType)
            {
                action = SetActionType(actions, stateMask, newType);
                currentType = newType;
            }

            if (currentType != UIInteractionFeedbackActionType.None)
            {
                if (action == null)
                {
                    action = SetActionType(actions, stateMask, currentType);
                }

                DrawActionParameters(action, currentType);
            }
            EditorGUILayout.EndHorizontal();

            if (currentType != UIInteractionFeedbackActionType.None)
            {
                DrawActionTiming(action, currentType);
            }
        }

        private static string[] BuildStateTabLabels(SerializedProperty actions)
        {
            var labels = new string[StateTabs.Length];
            for (var i = 0; i < StateTabs.Length; i++)
            {
                labels[i] = FindAction(actions, StateMasks[i]) >= 0 ? "*" + StateTabs[i] : StateTabs[i];
            }

            return labels;
        }

        private static void DrawActionParameters(SerializedProperty action, UIInteractionFeedbackActionType type)
        {
            switch (type)
            {
                case UIInteractionFeedbackActionType.Scale:
                    EditorGUILayout.PropertyField(action.FindPropertyRelative("ScaleMultiplier"), GUIContent.none);
                    break;
                case UIInteractionFeedbackActionType.Offset:
                    EditorGUILayout.PropertyField(action.FindPropertyRelative("Offset"), GUIContent.none);
                    break;
                case UIInteractionFeedbackActionType.Color:
                    EditorGUILayout.PropertyField(action.FindPropertyRelative("ColorMultiplier"), GUIContent.none);
                    break;
                case UIInteractionFeedbackActionType.Material:
                    EditorGUILayout.PropertyField(action.FindPropertyRelative("Material"), GUIContent.none);
                    break;
            }
        }

        private static void DrawActionTiming(SerializedProperty action, UIInteractionFeedbackActionType type)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Apply", GUILayout.Width(54f));

            var mode = action.FindPropertyRelative("Mode");
            if (type == UIInteractionFeedbackActionType.Material)
            {
                mode.enumValueIndex = (int)UIInteractionFeedbackActionMode.Immediate;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(0, new[] { "Immediate" }, GUILayout.Width(140f));
                }
            }
            else
            {
                EditorGUILayout.PropertyField(mode, GUIContent.none, GUILayout.Width(140f));
            }

            if ((UIInteractionFeedbackActionMode)mode.enumValueIndex == UIInteractionFeedbackActionMode.Tween)
            {
                EditorGUILayout.PropertyField(action.FindPropertyRelative("Ease"), GUIContent.none, GUILayout.Width(130f));
                EditorGUILayout.LabelField("Duration", GUILayout.Width(56f));
                var duration = action.FindPropertyRelative("Duration");
                duration.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(duration.floatValue, GUILayout.Width(54f)));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(36f)))
            {
                AddNode();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void AddNode()
        {
            var index = m_targets.arraySize;
            m_targets.InsertArrayElementAtIndex(index);
            var node = m_targets.GetArrayElementAtIndex(index);
            node.FindPropertyRelative("Enabled").boolValue = true;
            node.FindPropertyRelative("Role").enumValueIndex = (int)UIInteractionFeedbackTargetRole.Custom;
            node.FindPropertyRelative("Target").objectReferenceValue = null;
            node.FindPropertyRelative("TargetPath").stringValue = string.Empty;
            node.FindPropertyRelative("IncludeChildren").boolValue = true;
            node.FindPropertyRelative("SelectedTab").intValue = 0;
            node.FindPropertyRelative("Foldout").boolValue = true;
            node.FindPropertyRelative("Actions").ClearArray();
        }

        private static SerializedProperty SetActionType(SerializedProperty actions, UIInteractionFeedbackStateMask stateMask, UIInteractionFeedbackActionType type)
        {
            var existingIndex = FindAction(actions, stateMask);
            if (type == UIInteractionFeedbackActionType.None)
            {
                if (existingIndex >= 0)
                {
                    actions.DeleteArrayElementAtIndex(existingIndex);
                }

                return null;
            }

            SerializedProperty action;
            if (existingIndex >= 0)
            {
                action = actions.GetArrayElementAtIndex(existingIndex);
            }
            else
            {
                actions.InsertArrayElementAtIndex(actions.arraySize);
                action = actions.GetArrayElementAtIndex(actions.arraySize - 1);
                SetActionDefaults(action, stateMask, type);
            }

            action.FindPropertyRelative("Enabled").boolValue = true;
            action.FindPropertyRelative("States").intValue = (int)stateMask;
            action.FindPropertyRelative("Type").enumValueIndex = (int)type;
            return action;
        }

        private static void SetActionDefaults(SerializedProperty action, UIInteractionFeedbackStateMask stateMask, UIInteractionFeedbackActionType type)
        {
            action.FindPropertyRelative("Enabled").boolValue = true;
            action.FindPropertyRelative("States").intValue = (int)stateMask;
            action.FindPropertyRelative("Type").enumValueIndex = (int)type;
            action.FindPropertyRelative("Mode").enumValueIndex = (int)UIInteractionFeedbackActionMode.Immediate;
            action.FindPropertyRelative("Ease").enumValueIndex = (int)EFrameEase.OutQuad;
            action.FindPropertyRelative("Duration").floatValue = 0.15f;
            action.FindPropertyRelative("Offset").vector2Value = stateMask == UIInteractionFeedbackStateMask.Pressed ? new Vector2(0f, -2f) : Vector2.zero;
            action.FindPropertyRelative("ScaleMultiplier").vector3Value = Vector3.one;
            action.FindPropertyRelative("Alpha").floatValue = 1f;
            action.FindPropertyRelative("ColorMultiplier").colorValue = Color.white;
            action.FindPropertyRelative("Material").objectReferenceValue = null;
            action.FindPropertyRelative("Active").boolValue = true;
        }

        private static int FindAction(SerializedProperty actions, UIInteractionFeedbackStateMask stateMask)
        {
            for (var i = 0; i < actions.arraySize; i++)
            {
                var action = actions.GetArrayElementAtIndex(i);
                if (action.FindPropertyRelative("States").intValue == (int)stateMask)
                {
                    return i;
                }
            }

            return -1;
        }

        private static UIInteractionFeedbackActionType[] GetAvailableTypes(RectTransform targetTransform)
        {
            var hasGraphic = targetTransform.GetComponent<Graphic>() != null || targetTransform.GetComponentInChildren<Graphic>(true) != null;
            var hasTmpText = HasTmpText(targetTransform);
            if (hasGraphic || hasTmpText)
            {
                return new[]
                {
                    UIInteractionFeedbackActionType.None,
                    UIInteractionFeedbackActionType.Scale,
                    UIInteractionFeedbackActionType.Offset,
                    UIInteractionFeedbackActionType.Color,
                    UIInteractionFeedbackActionType.Material
                };
            }

            return new[]
            {
                UIInteractionFeedbackActionType.None,
                UIInteractionFeedbackActionType.Scale,
                UIInteractionFeedbackActionType.Offset
            };
        }

        private static bool HasTmpText(RectTransform targetTransform)
        {
            var components = targetTransform.GetComponentsInChildren<Component>(true);
            for (var i = 0; i < components.Length; i++)
            {
                var type = components[i] != null ? components[i].GetType() : null;
                while (type != null)
                {
                    if (type.FullName == "TMPro.TMP_Text")
                    {
                        return true;
                    }

                    type = type.BaseType;
                }
            }

            return false;
        }

        private static string[] ToNames(UIInteractionFeedbackActionType[] types)
        {
            var names = new string[types.Length];
            for (var i = 0; i < types.Length; i++)
            {
                names[i] = types[i].ToString();
            }

            return names;
        }

        private static string GetFullPath(Transform transform, Transform root)
        {
            if (transform == root)
            {
                return root.name;
            }

            var path = root.name + "/" + transform.name;
            var parent = transform.parent;
            while (parent != null && parent != root)
            {
                path = root.name + "/" + parent.name + "/" + path.Substring(root.name.Length + 1);
                parent = parent.parent;
            }

            return path;
        }

        private static string GetRelativePath(Transform transform, Transform root)
        {
            if (transform == root)
            {
                return ".";
            }

            var path = transform.name;
            var parent = transform.parent;
            while (parent != null && parent != root)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return parent == root ? path : string.Empty;
        }

        private static RectTransform FindRelativeRect(Transform root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            if (relativePath == ".")
            {
                return root as RectTransform;
            }

            return root.Find(relativePath) as RectTransform;
        }

        private static bool IsSameHierarchy(Transform transform, Transform root)
        {
            var current = transform;
            while (current != null)
            {
                if (current == root)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void ResolveTargetReference(SerializedProperty targetProperty, SerializedProperty targetPathProperty, Transform root)
        {
            var path = targetPathProperty.stringValue;
            if (!string.IsNullOrWhiteSpace(path))
            {
                var resolved = FindRelativeRect(root, path);
                if (resolved != null && targetProperty.objectReferenceValue != resolved)
                {
                    targetProperty.objectReferenceValue = resolved;
                }

                return;
            }

            var current = targetProperty.objectReferenceValue as RectTransform;
            if (current != null && IsSameHierarchy(current.transform, root))
            {
                targetPathProperty.stringValue = GetRelativePath(current.transform, root);
            }
        }

        private static string GetMenuPath(Transform transform, Transform root)
        {
            return GetFullPath(transform, root).Replace("/", " > ");
        }
    }
}
