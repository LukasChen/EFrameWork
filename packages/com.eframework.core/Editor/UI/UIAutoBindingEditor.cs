using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using EFramework.Runtime.UI;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using EFramework.Runtime.UI.Transitions;

namespace EFramework.Editor.UI
{
    [CustomEditor(typeof(QUIBinding))]
    public class UIAutoBindingEditor : UnityEditor.Editor
    {
        private const string AppGeneratedUIPath = "Assets/App/Runtime/Generated/UI";
        private const string ModulesRootPath = "Assets/Modules";
        private static readonly HashSet<string> s_csharpKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is",
            "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte",
            "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
            "ushort", "using", "virtual", "void", "volatile", "while"
        };
        private static readonly HashSet<string> s_bindingNameStopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Root", "Content", "Container", "Group", "Layout", "Node", "Wrapper", "Holder",
            "Area", "Panel", "Window", "View"
        };
        private static readonly Dictionary<string, string> s_bindingNameAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Btn", "Button" },
            { "Txt", "Text" },
            { "Tmp", "Text" },
            { "Img", "Image" },
            { "Bg", "Background" },
            { "Tog", "Toggle" },
            { "ScrollView", "Scroll" },
            { "ScrollRect", "Scroll" }
        };
        private static readonly HashSet<string> s_bindingNameRootSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Panel", "Window", "View", "Page", "Popup"
        };

        private QUIBinding m_target;
        private Vector2 m_scrollPosition;
        private Vector2 m_treeScrollPosition;
        private Vector2 m_componentScrollPosition;

        // 树状节点相关
        private List<RectTransform> m_rectTransformNodes = new List<RectTransform>();
        private Dictionary<RectTransform, bool> m_nodeExpandedState = new Dictionary<RectTransform, bool>();
        private RectTransform m_selectedNode = null;
        private List<Component> m_selectedNodeComponents = new List<Component>();

        // ViewConfig 相关
        private bool m_viewConfigFoldout = true;
        private SerializedProperty m_viewConfigProperty;

        private void OnEnable()
        {
            m_target = (QUIBinding)target;
            m_viewConfigProperty = serializedObject.FindProperty("m_viewConfig");
            RefreshRectTransformNodes();
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawViewConfigSection();
            EditorGUILayout.Space(5);
            DrawControlButtons();
            DrawBindingsList();
            DrawAvailableComponents();

            if (GUI.changed)
            {
                MarkBindingDataDirty();
            }
        }

        private void DrawViewConfigSection()
        {
            // 检查目标对象是否可以持久化（避免 DontSaveInEditor 断言错误）
            bool isDontSave = (m_target.hideFlags & HideFlags.DontSaveInEditor) != 0 ||
                              (m_target.gameObject != null && (m_target.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0);

            if (!EditorUtility.IsPersistent(serializedObject.targetObject) && isDontSave)
            {
                EditorGUILayout.HelpBox("View 配置在非持久化对象上不可编辑", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");

            m_viewConfigFoldout = EditorGUILayout.Foldout(m_viewConfigFoldout, "⚙️ View 配置", true, EditorStyles.foldoutHeader);

            if (m_viewConfigFoldout && m_viewConfigProperty != null)
            {
                EditorGUI.indentLevel++;

                // DefaultLayer
                var layerProp = m_viewConfigProperty.FindPropertyRelative("DefaultLayer");
                if (layerProp != null)
                    EditorGUILayout.PropertyField(layerProp, new GUIContent("默认层级", "View 打开时的默认 UI 层级"));

                // IsViewRoot
                var isViewRoot = m_viewConfigProperty.FindPropertyRelative("IsViewRoot");
                if (isViewRoot != null)
                    EditorGUILayout.PropertyField(isViewRoot, new GUIContent("是否界面根节点", "方便该界面上的按钮获取到所属界面"));

                // UsingCache
                var cacheProp = m_viewConfigProperty.FindPropertyRelative("UsingCache");
                if (cacheProp != null)
                    EditorGUILayout.PropertyField(cacheProp, new GUIContent("使用缓存", "开启后关闭时隐藏而非销毁，可重复使用"));

                // AnimationRootName - 使用下拉菜单
                var animRootProp = m_viewConfigProperty.FindPropertyRelative("AnimationRootName");
                if (animRootProp != null)
                    DrawAnimationRootPopup(animRootProp);

                var openTransitionProp = m_viewConfigProperty.FindPropertyRelative("OpenTransition");
                var closeTransitionProp = m_viewConfigProperty.FindPropertyRelative("CloseTransition");
                if (openTransitionProp != null && closeTransitionProp != null)
                {
                    MigrateLegacyTransitionConfigIfNeeded(openTransitionProp, closeTransitionProp);
                    DrawTransitionConfig(openTransitionProp, "打开动画");
                    DrawTransitionConfig(closeTransitionProp, "关闭动画");
                }
                else
                {
                    DrawLegacyTransitionConfig();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void MigrateLegacyTransitionConfigIfNeeded(SerializedProperty openTransitionProp, SerializedProperty closeTransitionProp)
        {
            if (HasSerializedTransitionValue(openTransitionProp) || HasSerializedTransitionValue(closeTransitionProp))
            {
                return;
            }

            var legacyTypeProp = m_viewConfigProperty.FindPropertyRelative("TransitionTypeName");
            var legacyDurationProp = m_viewConfigProperty.FindPropertyRelative("AnimationDuration");
            var legacyTypeName = legacyTypeProp?.stringValue ?? string.Empty;
            var legacyDuration = legacyDurationProp?.floatValue ?? 0f;
            if (string.IsNullOrEmpty(legacyTypeName) && legacyDuration <= 0f)
            {
                return;
            }

            SetTransitionConfig(openTransitionProp, legacyTypeName, legacyDuration);
            SetTransitionConfig(closeTransitionProp, legacyTypeName, legacyDuration);
            MarkBindingDataDirty();
        }

        private static bool HasSerializedTransitionValue(SerializedProperty transitionProp)
        {
            var transitionTypeProp = transitionProp.FindPropertyRelative("TransitionTypeName");
            var durationProp = transitionProp.FindPropertyRelative("Duration");
            return !string.IsNullOrEmpty(transitionTypeProp?.stringValue) || (durationProp?.floatValue ?? 0f) > 0f;
        }

        private static void SetTransitionConfig(SerializedProperty transitionProp, string transitionTypeName, float duration)
        {
            var transitionTypeProp = transitionProp.FindPropertyRelative("TransitionTypeName");
            var durationProp = transitionProp.FindPropertyRelative("Duration");
            if (transitionTypeProp != null)
            {
                transitionTypeProp.stringValue = transitionTypeName;
            }

            if (durationProp != null)
            {
                durationProp.floatValue = duration > 0f ? duration : ViewTransitionConfig.DefaultDuration;
            }
        }

        private void DrawTransitionConfig(SerializedProperty transitionProp, string label)
        {
            var transitionTypeProp = transitionProp.FindPropertyRelative("TransitionTypeName");
            var durationProp = transitionProp.FindPropertyRelative("Duration");
            if (transitionTypeProp == null || durationProp == null)
            {
                return;
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DrawTransitionTypePopup(transitionTypeProp, new GUIContent("类型", "选择实现 IUIViewTransition 的动画类型，可在业务代码中自定义扩展"));
            EditorGUILayout.PropertyField(durationProp, new GUIContent("时长", "动画时长（秒），0 表示使用动画默认时长"));
            ClampTransitionDuration(durationProp);
            EditorGUI.indentLevel--;
        }

        private void DrawLegacyTransitionConfig()
        {
            var transitionTypeProp = m_viewConfigProperty.FindPropertyRelative("TransitionTypeName");
            if (transitionTypeProp != null)
            {
                DrawTransitionTypePopup(transitionTypeProp, new GUIContent("动画类型", "选择实现 IUIViewTransition 的动画类型，可在业务代码中自定义扩展"));
            }

            var durationProp = m_viewConfigProperty.FindPropertyRelative("AnimationDuration");
            if (durationProp != null)
            {
                EditorGUILayout.PropertyField(durationProp, new GUIContent("动画时长", "开关动画的持续时间（秒）"));
                ClampTransitionDuration(durationProp);
            }
        }

        private static void ClampTransitionDuration(SerializedProperty durationProp)
        {
            if (durationProp.floatValue < 0)
            {
                durationProp.floatValue = 0;
            }

            if (durationProp.floatValue > 2f)
            {
                durationProp.floatValue = 2f;
            }
        }

        private void DrawTransitionTypePopup(SerializedProperty transitionTypeProp, GUIContent label)
        {
            var transitionTypes = TypeCache.GetTypesDerivedFrom<IUIViewTransition>()
                .Where(type => !type.IsAbstract && !typeof(MonoBehaviour).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name)
                .ToList();

            var options = new List<string>();
            var values = new List<string>();

            options.Add("Default (ScaleFade)");
            values.Add(string.Empty);

            foreach (var type in transitionTypes)
            {
                options.Add(FormatTransitionName(type));
                values.Add(GetTransitionId(type));
            }

            int currentIndex = 0;
            var currentValue = transitionTypeProp.stringValue;
            if (!string.IsNullOrEmpty(currentValue))
            {
                var foundIndex = values.FindIndex(value => IsTransitionValueMatch(value, currentValue, transitionTypes));
                if (foundIndex >= 0)
                {
                    currentIndex = foundIndex;
                }
                else
                {
                    options.Add($"⚠ {currentValue} (未找到)");
                    values.Add(currentValue);
                    currentIndex = options.Count - 1;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(
                label,
                currentIndex,
                options.ToArray()
            );

            if (EditorGUI.EndChangeCheck())
            {
                transitionTypeProp.stringValue = values[newIndex];
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static string FormatTransitionName(Type type)
        {
            var name = type.Name;
            const string suffix = "ViewTransition";
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - suffix.Length);
            }

            return $"{name} ({type.Namespace})";
        }

        private static string GetTransitionId(Type type)
        {
            if (type == typeof(ScaleFadeViewTransition))
            {
                return ScaleFadeViewTransition.Id;
            }

            if (type == typeof(NoneViewTransition))
            {
                return NoneViewTransition.Id;
            }

            return type.FullName;
        }

        private static bool IsTransitionValueMatch(string optionValue, string currentValue, List<Type> transitionTypes)
        {
            if (optionValue == currentValue)
            {
                return true;
            }

            var optionType = ResolveTransitionType(optionValue, transitionTypes);
            if (optionType == null)
            {
                return false;
            }

            return currentValue == optionType.FullName
                || currentValue == optionType.AssemblyQualifiedName
                || currentValue.StartsWith(optionType.FullName + ",", StringComparison.Ordinal);
        }

        private static Type ResolveTransitionType(string value, List<Type> transitionTypes)
        {
            if (value == ScaleFadeViewTransition.Id)
            {
                return typeof(ScaleFadeViewTransition);
            }

            if (value == NoneViewTransition.Id)
            {
                return typeof(NoneViewTransition);
            }

            return transitionTypes.FirstOrDefault(type => type.FullName == value || type.AssemblyQualifiedName == value);
        }

        private void DrawAnimationRootPopup(SerializedProperty animRootProp)
        {
            // 收集1-2级子节点
            var nodeOptions = new List<string> { "(无动画)" };
            var nodePaths = new List<string> { "" };

            CollectChildNodes(m_target.transform, "", 0, 2, nodeOptions, nodePaths);

            // 查找当前选中的索引
            int currentIndex = 0;
            string currentValue = animRootProp.stringValue;
            if (!string.IsNullOrEmpty(currentValue))
            {
                int foundIndex = nodePaths.IndexOf(currentValue);
                if (foundIndex >= 0)
                {
                    currentIndex = foundIndex;
                }
                else
                {
                    // 当前值不在列表中，添加为无效项
                    nodeOptions.Add($"⚠ {currentValue} (未找到)");
                    nodePaths.Add(currentValue);
                    currentIndex = nodeOptions.Count - 1;
                }
            }

            // 绘制下拉菜单
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(
                new GUIContent("动画根节点", "用于播放开关动画的子节点，留空则不播放动画"),
                currentIndex,
                nodeOptions.ToArray()
            );

            if (EditorGUI.EndChangeCheck())
            {
                animRootProp.stringValue = nodePaths[newIndex];
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void CollectChildNodes(Transform parent, string pathPrefix, int currentDepth, int maxDepth, List<string> options, List<string> paths)
        {
            if (currentDepth >= maxDepth) return;

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string childPath = string.IsNullOrEmpty(pathPrefix) ? child.name : $"{pathPrefix}/{child.name}";

                // 添加节点显示名称（带层级缩进）
                string indent = currentDepth > 0 ? "    " : "";
                string displayName = $"{indent}{child.name}";
                options.Add(displayName);
                paths.Add(childPath);

                // 递归收集下一级
                CollectChildNodes(child, childPath, currentDepth + 1, maxDepth, options, paths);
            }
        }

        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // 刷新按钮（小图标）
            if (GUILayout.Button(new GUIContent("🔄", "刷新节点树"), GUILayout.Width(28), GUILayout.Height(25)))
            {
                RefreshRectTransformNodes();
            }

            // 生成访问类按钮（绿色突出）
            GUI.enabled = m_target.BindingItems.Count > 0;
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Generate Class", GUILayout.Height(25)))
            {
                GenerateAccessClass();
            }
            GUI.backgroundColor = originalColor;
            GUI.enabled = true;

            // 清空绑定按钮
            if (GUILayout.Button("Clear All", GUILayout.Width(70), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("清空确认", "确定要清空所有绑定吗？", "确定", "取消"))
                {
                    Undo.RecordObject(m_target, "Clear UI Bindings");
                    m_target.ClearAllBindings();
                    SaveCurrentData();
                }
            }

            // 验证绑定按钮
            if (GUILayout.Button("Validate", GUILayout.Width(60), GUILayout.Height(25)))
            {
                Undo.RecordObject(m_target, "Validate UI Bindings");
                m_target.ValidateBindings();
                MarkBindingDataDirty();
                Debug.Log("绑定验证完成");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshRectTransformNodes()
        {
            m_rectTransformNodes.Clear();
            m_nodeExpandedState.Clear();

            // 收集所有RectTransform节点
            CollectRectTransformsRecursively(m_target.GetComponent<RectTransform>(), m_rectTransformNodes);

            // 初始化展开状态 - 默认展开前2级节点
            var rootTransform = m_target.GetComponent<RectTransform>();
            foreach (var rect in m_rectTransformNodes)
            {
                // 计算节点深度
                int depth = GetNodeDepthFromRoot(rect, rootTransform);
                // 展开前2级（深度0和1的节点）
                m_nodeExpandedState[rect] = depth <= 1;
            }
        }

        private void CollectRectTransformsRecursively(RectTransform current, List<RectTransform> results)
        {
            if (current == null) return;

            results.Add(current);
            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i) as RectTransform;
                if (child != null)
                {
                    CollectRectTransformsRecursively(child, results);
                }
            }
        }

        private void RefreshSelectedNodeComponents()
        {
            m_selectedNodeComponents.Clear();

            if (m_selectedNode == null) return;

            // 获取所有组件，包括RectTransform
            var allComponents = m_selectedNode.GetComponents<Component>();

            foreach (var component in allComponents)
            {
                if (component == null) continue;

                // 排除Transform（因为RectTransform已经包含了Transform功能）
                // 排除当前的QUIBinding组件
                if (component is Transform && !(component is RectTransform)) continue;
                if (component == m_target) continue;

                m_selectedNodeComponents.Add(component);
            }
        }

        private void DrawBindingsList()
        {
            if (m_target.BindingItems.Count == 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("暂无绑定项\n在下方选择节点和组件来添加绑定", MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(10);
                return;
            }

            // 绑定列表标题和操作区
            EditorGUILayout.BeginVertical("box");

            // 标题行 - 显示数量和快速操作
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"🔗 绑定列表 ({m_target.BindingItems.Count})", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // 清空所有绑定按钮
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("🗑️", GUILayout.Width(30), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("清空确认", "确定要清空所有绑定吗？", "确定", "取消"))
                {
                    Undo.RecordObject(m_target, "Clear UI Bindings");
                    m_target.ClearAllBindings();
                    SaveCurrentData();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // 绑定项列表
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.Height(180));

            for (int i = 0; i < m_target.BindingItems.Count; i++)
            {
                var item = m_target.BindingItems[i];
                DrawBindingItem(item, i);

                // 添加分隔线（除了最后一项）
                if (i < m_target.BindingItems.Count - 1)
                {
                    EditorGUILayout.Space(2);
                    var rect = EditorGUILayout.GetControlRect(false, 1);
                    EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawBindingItem(ComponentBindingItem item, int index)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(40));

            // 序号显示
            EditorGUILayout.BeginVertical(GUILayout.Width(25));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{index + 1:00}", EditorStyles.miniLabel, GUILayout.Width(20));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            // 主要内容区域
            EditorGUILayout.BeginVertical();

            // 绑定名称编辑 - 增强样式
            EditorGUI.BeginChangeCheck();
            var newBindingName = EditorGUILayout.TextField(item.BindingName, EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_target, "Rename UI Binding");
                item.BindingName = MakeUniqueBindingName(newBindingName, item);
                MarkBindingDataDirty();
            }

            // 组件信息 - 增加GameObject名称
            var componentInfo = $"{GetComponentIcon(item.Component)} {item.ComponentTypeName}";
            if (item.Component != null && item.Component.gameObject != null)
            {
                componentInfo += $" (来自: {item.Component.gameObject.name})";
            }
            EditorGUILayout.LabelField(componentInfo, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            // 操作按钮组
            EditorGUILayout.BeginHorizontal(GUILayout.Width(60));

            // 跳转到GameObject按钮
            if (GUILayout.Button("📍", GUILayout.Width(25), GUILayout.Height(20)))
            {
                if (item.Component != null && item.Component.gameObject != null)
                {
                    Selection.activeGameObject = item.Component.gameObject;
                    EditorGUIUtility.PingObject(item.Component.gameObject);
                }
            }

            // 删除按钮 - 增大尺寸
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("×", GUILayout.Width(30), GUILayout.Height(20)))
            {
                Undo.RecordObject(m_target, "Remove UI Binding");
                m_target.BindingItems.RemoveAt(index);
                MarkBindingDataDirty();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal(); EditorGUILayout.EndHorizontal();
        }

        private void DrawAvailableComponents()
        {
            // 上半部分：节点树
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🌲 RectTransform Nodes", EditorStyles.boldLabel);

            m_treeScrollPosition = EditorGUILayout.BeginScrollView(m_treeScrollPosition, GUILayout.Height(300));
            DrawNodeTree();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 下半部分：选中节点的组件
            EditorGUILayout.BeginVertical("box");
            if (m_selectedNode != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🔧 Components on: {m_selectedNode.name}", EditorStyles.boldLabel);

                // 添加手动跳转到节点的按钮
                if (GUILayout.Button("📍", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    Selection.activeGameObject = m_selectedNode.gameObject;
                    EditorGUIUtility.PingObject(m_selectedNode.gameObject);
                }



                EditorGUILayout.EndHorizontal();

                m_componentScrollPosition = EditorGUILayout.BeginScrollView(m_componentScrollPosition, GUILayout.Height(100));
                DrawNodeComponents();
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("🔧 Node Components", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("请先选择一个节点来查看其组件", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawNodeTree()
        {
            if (m_rectTransformNodes.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到RectTransform节点", MessageType.Warning);
                return;
            }

            // 只从根节点开始绘制，避免重复显示
            var rootNode = m_target.GetComponent<RectTransform>();
            if (rootNode != null)
            {
                DrawTreeNode(rootNode, 0);
            }
        }

        private void DrawTreeNode(RectTransform node, int depth)
        {
            if (node == null) return;

            EditorGUILayout.BeginHorizontal();

            // 缩进 - 使用更清晰的缩进量
            GUILayout.Space(depth * 15);

            // 展开/折叠按钮（如果有子节点）
            bool hasChildren = HasRectTransformChildren(node);
            if (hasChildren)
            {
                bool isExpanded = m_nodeExpandedState.GetValueOrDefault(node, false);
                string foldoutSymbol = isExpanded ? "▼" : "▶";

                if (GUILayout.Button(foldoutSymbol, GUILayout.Width(18), GUILayout.Height(18)))
                {
                    m_nodeExpandedState[node] = !isExpanded;
                }
            }
            else
            {
                // 叶节点显示不同的图标
                GUILayout.Label("•", GUILayout.Width(18));
            }

            // 层级线条（可选 - 增强视觉效果）
            string levelPrefix = "";
            if (depth > 0)
            {
                levelPrefix = new string('│', depth - 1) + "├─ ";
            }

            // 节点选择按钮
            Color originalColor = GUI.backgroundColor;
            if (m_selectedNode == node)
            {
                GUI.backgroundColor = Color.green;
            }

            string nodeName = string.IsNullOrEmpty(node.name) ? "<unnamed>" : node.name;
            string displayName = $"{levelPrefix}📦 {nodeName}";

            // 创建左对齐的按钮样式
            GUIStyle leftAlignedButton = new GUIStyle(GUI.skin.button);
            leftAlignedButton.alignment = TextAnchor.MiddleLeft;

            if (GUILayout.Button(displayName, leftAlignedButton, GUILayout.ExpandWidth(true)))
            {
                // 如果点击的是已选中的节点，执行展开/折叠操作
                if (m_selectedNode == node && hasChildren)
                {
                    m_nodeExpandedState[node] = !m_nodeExpandedState.GetValueOrDefault(node, false);
                }
                else
                {
                    // 否则选中该节点
                    SelectNode(node);
                }
            }

            GUI.backgroundColor = originalColor;

            EditorGUILayout.EndHorizontal();

            // 递归绘制子节点（只有在展开时才绘制）
            if (hasChildren && m_nodeExpandedState.GetValueOrDefault(node, false))
            {
                for (int i = 0; i < node.childCount; i++)
                {
                    var child = node.GetChild(i) as RectTransform;
                    if (child != null)
                    {
                        DrawTreeNode(child, depth + 1);
                    }
                }
            }
        }

        private void DrawNodeComponents()
        {
            if (m_selectedNodeComponents.Count == 0)
            {
                EditorGUILayout.HelpBox("该节点上没有可绑定的组件", MessageType.Info);
                return;
            }

            // 网格布局显示组件按钮 - 使用自适应宽度避免横向滚动
            int buttonsPerRow = 2; // 减少每行按钮数量，避免宽度问题
            int currentInRow = 0;

            EditorGUILayout.BeginHorizontal();

            foreach (var component in m_selectedNodeComponents)
            {
                if (component == null) continue;

                // 换行处理
                if (currentInRow >= buttonsPerRow)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    currentInRow = 0;
                }

                // 检查是否已经被绑定
                bool isAlreadyBound = IsComponentAlreadyBound(component);

                // 设置按钮颜色
                Color originalColor = GUI.backgroundColor;
                if (isAlreadyBound)
                {
                    GUI.backgroundColor = Color.green; // 绿色表示已绑定
                }
                else
                {
                    GUI.backgroundColor = Color.white; // 白色表示未绑定
                }

                // 组件图标和名称
                string componentIcon = GetComponentIcon(component);
                string buttonText = $"{componentIcon} {component.GetType().Name}";

                // 添加已绑定的标识
                if (isAlreadyBound)
                {
                    buttonText += " ✓";
                }

                // 组件绑定/取消绑定按钮 - 使用ExpandWidth自适应宽度
                if (GUILayout.Button(buttonText, GUILayout.ExpandWidth(true), GUILayout.Height(25)))
                {
                    if (isAlreadyBound)
                    {
                        RemoveBindingForComponent(component); // 取消绑定
                    }
                    else
                    {
                        AddBinding(m_selectedNode.gameObject, component); // 添加绑定
                    }
                }

                GUI.backgroundColor = originalColor;
                currentInRow++;
            }

            // 填充剩余空间
            if (currentInRow > 0)
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private bool IsComponentAlreadyBound(Component component)
        {
            return m_target.BindingItems.Any(item => item.Component == component);
        }

        private void RemoveBindingForComponent(Component component)
        {
            var bindingItem = m_target.BindingItems.FirstOrDefault(item => item.Component == component);
            if (bindingItem != null)
            {
                Undo.RecordObject(m_target, "Remove UI Binding");
                m_target.RemoveBinding(bindingItem.BindingName);
                MarkBindingDataDirty();
                Debug.Log($"取消绑定: {bindingItem.BindingName} -> {component.GetType().Name}");
            }
        }

        private void SelectNode(RectTransform node)
        {
            m_selectedNode = node;
            RefreshSelectedNodeComponents();

            // 不再自动切换Selection，避免编辑器窗口跳转
            // Selection.activeGameObject = node.gameObject;
        }

        private int GetNodeDepth(RectTransform node)
        {
            int depth = 0;
            Transform parent = node.parent;
            Transform rootTransform = m_target.transform;

            while (parent != null && parent != rootTransform)
            {
                depth++;
                parent = parent.parent;
            }

            return depth;
        }

        private int GetNodeDepthFromRoot(RectTransform node, RectTransform rootTransform)
        {
            if (node == rootTransform) return 0;

            int depth = 0;
            Transform parent = node.parent;

            while (parent != null && parent != rootTransform)
            {
                depth++;
                parent = parent.parent;
            }

            // 如果找到了rootTransform，再加1（因为node是rootTransform的子级）
            if (parent == rootTransform)
            {
                depth++;
            }

            return depth;
        }

        private bool HasRectTransformChildren(RectTransform node)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                if (node.GetChild(i) is RectTransform)
                    return true;
            }
            return false;
        }

        private string GetComponentIcon(Component component)
        {
            switch (component)
            {
                case RectTransform _: return "📐";
                case Button _: return "🔘";
                case Image _: return "🖼️";
                case Text _: return "📝";
                case TextMeshProUGUI _: return "✏️";
                case Slider _: return "🎚️";
                case Toggle _: return "☑️";
                case InputField _: return "📝";
                case ScrollRect _: return "📜";
                case Canvas _: return "🖼️";
                case CanvasGroup _: return "👥";
                default: return "⚙️";
            }
        }

        private void AddBinding(GameObject gameObject, Component component)
        {
            Undo.RecordObject(m_target, "Add UI Binding");
            string bindingName = CreateUniqueDefaultBindingName(gameObject, component);
            m_target.AddBinding(bindingName, component, gameObject.name);
            MarkBindingDataDirty();
            Debug.Log($"添加绑定: {bindingName} -> {component.GetType().Name}");
        }

        private string CreateUniqueDefaultBindingName(GameObject gameObject, Component component)
        {
            var usedNames = new HashSet<string>(
                m_target.BindingItems.Select(item => item.BindingName),
                StringComparer.Ordinal);

            foreach (var candidate in CreateDefaultBindingNameCandidates(gameObject, component))
            {
                var bindingName = ToValidIdentifier(candidate, "Binding");
                if (!usedNames.Contains(bindingName))
                {
                    return bindingName;
                }
            }

            return MakeUniqueIdentifier(CreateDefaultBindingName(gameObject, component), usedNames);
        }

        private IEnumerable<string> CreateDefaultBindingNameCandidates(GameObject gameObject, Component component)
        {
            var componentSuffix = GetBindingComponentSuffix(component);
            var pathParts = GetBindingPathNameParts(gameObject.transform, componentSuffix);
            if (pathParts.Count == 0)
            {
                pathParts.Add(CreateBindingNamePart(gameObject.name, true, componentSuffix));
            }

            var emitted = new HashSet<string>(StringComparer.Ordinal);
            int leafIndex = pathParts.Count - 1;
            int maxAncestorCount = Math.Min(3, leafIndex);

            for (int ancestorCount = 0; ancestorCount <= maxAncestorCount; ancestorCount++)
            {
                int startIndex = leafIndex - ancestorCount;
                var selectedParts = TrimRedundantPathParts(pathParts.Skip(startIndex).ToList());
                var candidate = AppendComponentSuffixIfNeeded(JoinIdentifierParts(selectedParts), componentSuffix);
                if (!string.IsNullOrEmpty(candidate) && emitted.Add(candidate))
                {
                    yield return candidate;
                }
            }

            if (pathParts.Count > maxAncestorCount + 1)
            {
                var fullPathCandidate = AppendComponentSuffixIfNeeded(JoinIdentifierParts(TrimRedundantPathParts(pathParts)), componentSuffix);
                if (!string.IsNullOrEmpty(fullPathCandidate) && emitted.Add(fullPathCandidate))
                {
                    yield return fullPathCandidate;
                }
            }
        }

        private List<string> GetBindingPathNameParts(Transform targetTransform, string componentSuffix)
        {
            var stack = new Stack<Transform>();
            var current = targetTransform;
            var root = m_target != null ? m_target.transform : null;

            while (current != null && current != root)
            {
                stack.Push(current);
                current = current.parent;
            }

            var result = new List<string>();
            while (stack.Count > 0)
            {
                var transform = stack.Pop();
                bool isLeaf = stack.Count == 0;
                var part = CreateBindingNamePart(transform.name, isLeaf, componentSuffix);
                if (!string.IsNullOrEmpty(part))
                {
                    result.Add(part);
                }
            }

            return result;
        }

        private static string CreateBindingNamePart(string rawName, bool isLeaf, string componentSuffix)
        {
            var words = SplitNameWords(rawName)
                .Select(NormalizeBindingNameWord)
                .Where(word => !string.IsNullOrEmpty(word))
                .ToList();

            if (words.Count > 1 && s_bindingNameRootSuffixes.Contains(words[words.Count - 1]))
            {
                words.RemoveAt(words.Count - 1);
            }

            if (isLeaf)
            {
                RemoveLeadingComponentAlias(words, componentSuffix);
            }
            else
            {
                words.RemoveAll(word => s_bindingNameStopWords.Contains(word));
            }

            if (words.Count == 0)
            {
                return isLeaf ? ToValidIdentifier(rawName, "Node") : string.Empty;
            }

            return JoinIdentifierParts(words);
        }

        private static IEnumerable<string> SplitNameWords(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                yield break;
            }

            var word = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char current = name[i];
                if (!char.IsLetterOrDigit(current))
                {
                    if (word.Length > 0)
                    {
                        yield return word.ToString();
                        word.Length = 0;
                    }
                    continue;
                }

                if (word.Length > 0)
                {
                    char previous = word[word.Length - 1];
                    bool nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                    if ((char.IsLower(previous) && char.IsUpper(current)) ||
                        (char.IsUpper(previous) && char.IsUpper(current) && nextIsLower) ||
                        (char.IsDigit(previous) != char.IsDigit(current)))
                    {
                        yield return word.ToString();
                        word.Length = 0;
                    }
                }

                word.Append(current);
            }

            if (word.Length > 0)
            {
                yield return word.ToString();
            }
        }

        private static string NormalizeBindingNameWord(string word)
        {
            if (s_bindingNameAliases.TryGetValue(word, out var alias))
            {
                return alias;
            }

            if (word.Length <= 2 && word.All(char.IsUpper))
            {
                return word;
            }

            return char.ToUpperInvariant(word[0]) + word.Substring(1);
        }

        private static void RemoveLeadingComponentAlias(List<string> words, string componentSuffix)
        {
            if (words.Count <= 1 || string.IsNullOrEmpty(componentSuffix))
            {
                return;
            }

            var firstWord = words[0];
            if (NameContainsSemanticSuffix(firstWord, componentSuffix) ||
                string.Equals(firstWord, componentSuffix, StringComparison.OrdinalIgnoreCase))
            {
                words.RemoveAt(0);
            }
        }

        private static List<string> TrimRedundantPathParts(List<string> parts)
        {
            var result = new List<string>();
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                if (result.Count > 0)
                {
                    var previous = result[result.Count - 1];
                    if (part.StartsWith(previous, StringComparison.OrdinalIgnoreCase))
                    {
                        result.RemoveAt(result.Count - 1);
                    }
                }

                result.Add(part);
            }

            return result;
        }

        private static string JoinIdentifierParts(IEnumerable<string> parts)
        {
            return string.Join("_", parts.Where(part => !string.IsNullOrEmpty(part)));
        }

        private static string AppendComponentSuffixIfNeeded(string objectName, string componentSuffix)
        {
            if (string.IsNullOrEmpty(componentSuffix) || NameContainsSemanticSuffix(objectName, componentSuffix))
            {
                return objectName;
            }

            return string.IsNullOrEmpty(objectName) ? componentSuffix : $"{objectName}{componentSuffix}";
        }

        private string CreateDefaultBindingName(GameObject gameObject, Component component)
        {
            return ToValidIdentifier(CreateDefaultBindingNameCandidates(gameObject, component).FirstOrDefault(), "Binding");
        }

        private static string GetBindingComponentSuffix(Component component)
        {
            switch (component)
            {
                case RectTransform _:
                    return string.Empty;
                case TextMeshProUGUI _:
                case Text _:
                case InputField _:
                    return "Text";
                case Button _:
                    return "Button";
                case Image _:
                    return "Image";
                case Slider _:
                    return "Slider";
                case Toggle _:
                    return "Toggle";
                case ScrollRect _:
                    return "Scroll";
                case CanvasGroup _:
                    return "CanvasGroup";
                default:
                    return ToValidIdentifier(component.GetType().Name, "Component");
            }
        }

        private static bool NameContainsSemanticSuffix(string identifier, string suffix)
        {
            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(suffix))
            {
                return false;
            }

            var lowerName = identifier.ToLowerInvariant();
            var lowerSuffix = suffix.ToLowerInvariant();
            if (lowerName.EndsWith(lowerSuffix, StringComparison.Ordinal))
            {
                return true;
            }

            if (suffix == "Text")
            {
                return lowerName.EndsWith("label", StringComparison.Ordinal)
                    || lowerName.EndsWith("txt", StringComparison.Ordinal)
                    || lowerName.EndsWith("title", StringComparison.Ordinal)
                    || lowerName.EndsWith("desc", StringComparison.Ordinal)
                    || lowerName.EndsWith("description", StringComparison.Ordinal);
            }

            if (suffix == "Button")
            {
                return lowerName.EndsWith("btn", StringComparison.Ordinal);
            }

            if (suffix == "Image")
            {
                return lowerName.EndsWith("icon", StringComparison.Ordinal)
                    || lowerName.EndsWith("img", StringComparison.Ordinal)
                    || lowerName.EndsWith("bg", StringComparison.Ordinal)
                    || lowerName.EndsWith("background", StringComparison.Ordinal);
            }

            if (suffix == "Scroll")
            {
                return lowerName.EndsWith("scroll", StringComparison.Ordinal)
                    || lowerName.EndsWith("scrollview", StringComparison.Ordinal)
                    || lowerName.EndsWith("list", StringComparison.Ordinal);
            }

            if (suffix == "Toggle")
            {
                return lowerName.EndsWith("tog", StringComparison.Ordinal);
            }

            return false;
        }

        /// <summary>
        /// 保存当前编辑器数据
        /// </summary>
        private void SaveCurrentData()
        {
            MarkBindingDataDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"已自动保存 QUIBinding 数据: {m_target.gameObject.name} ({m_target.BindingItems.Count} 个绑定项)");
        }

        private void MarkBindingDataDirty()
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_target);

            if (PrefabUtility.IsPartOfPrefabInstance(m_target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(m_target);
            }

            var scene = m_target.gameObject.scene;
            if (!Application.isPlaying && scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            }
        }



        private void GenerateAccessClass()
        {
            NormalizeBindingNames();
            var codeItems = BuildGeneratedCodeItems();
            if (codeItems.Count == 0)
            {
                EditorUtility.DisplayDialog("生成失败", "没有绑定项！请先添加组件绑定。", "确定");
                return;
            }

            // 自动保存当前数据
            SaveCurrentData();

            string code = GenerateAccessClassCode();

            var classPath = ResolveGeneratedClassPath();
            var className = ResolveGeneratedClassName();
            if (!Directory.Exists(classPath))
            {
                Directory.CreateDirectory(classPath);
            }

            string filePath = Path.Combine(classPath, $"{className}.Generated.cs");

            // 保存文件（不刷新AssetDatabase避免焦点跳转）
            File.WriteAllText(filePath, code, Encoding.UTF8);

            // 使用异步导入，避免立即刷新导致的焦点跳转
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.DontDownloadFromCacheServer);

            Debug.Log($"访问类已生成: {filePath}");
            EditorUtility.DisplayDialog("生成成功", $"独立访问类已生成到: {filePath}\n\n包含 {codeItems.Count} 个组件的直接引用。", "确定");
        }

        private string ResolveGeneratedClassPath()
        {
            var assetPath = AssetDatabase.GetAssetPath(m_target.gameObject).Replace('\\', '/');
            if (TryResolveModuleName(assetPath, out var moduleName))
            {
                return $"{ModulesRootPath}/{moduleName}/Runtime/Generated/UI";
            }

            return AppGeneratedUIPath;
        }

        private string ResolveGeneratedClassName()
        {
            var className = SanitizePropertyName(m_target.AccessClassName);
            var assetPath = AssetDatabase.GetAssetPath(m_target.gameObject).Replace('\\', '/');
            if (TryResolveModuleName(assetPath, out var moduleName))
            {
                var modulePrefix = SanitizePropertyName(moduleName);
                if (!className.StartsWith(modulePrefix, StringComparison.OrdinalIgnoreCase) &&
                    !className.StartsWith($"v_{modulePrefix}", StringComparison.OrdinalIgnoreCase))
                {
                    className = className.StartsWith("v_", StringComparison.OrdinalIgnoreCase)
                        ? $"v_{modulePrefix}_{className.Substring(2)}"
                        : $"{modulePrefix}_{className}";
                }
            }

            return className;
        }

        private static bool TryResolveModuleName(string assetPath, out string moduleName)
        {
            moduleName = string.Empty;
            if (!assetPath.StartsWith(ModulesRootPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var segments = assetPath.Split('/');
            if (segments.Length < 3 || string.IsNullOrWhiteSpace(segments[2]))
            {
                return false;
            }

            moduleName = segments[2];
            return true;
        }

        private string GenerateAccessClassCode()
        {
            var codeItems = BuildGeneratedCodeItems();
            var className = ResolveGeneratedClassName();
            var sb = new StringBuilder();

            // 添加文件头注释
            sb.AppendLine("// 自动生成的独立访问类，请勿手动修改");
            sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"// 组件数量: {codeItems.Count}");
            sb.AppendLine($"// 源QUIBinding: {m_target.gameObject.name}");
            sb.AppendLine();

            // 添加using语句
            var usings = new HashSet<string>
            {
                "UnityEngine",
                "UnityEngine.UI",
                "TMPro",
                "System.Linq",
                "EFramework.Runtime.UI"
            };

            // 根据组件类型添加额外的using
            foreach (var codeItem in codeItems)
            {
                if (codeItem.Item.Component != null)
                {
                    var type = codeItem.Item.Component.GetType();
                    if (!string.IsNullOrEmpty(type.Namespace))
                    {
                        usings.Add(type.Namespace);
                    }
                }
            }

            foreach (var usingNamespace in usings.OrderBy(x => x))
            {
                sb.AppendLine($"using {usingNamespace};");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {m_target.AccessClassNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {m_target.gameObject.name} 的独立组件访问类");
            sb.AppendLine($"    /// 继承自 BindingViewBase，提供标准的UI绑定功能");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className} : BindingViewBase");
            sb.AppendLine("    {");



            // 生成构造函数和静态工厂方法
            sb.AppendLine("        #region Constructors & Factory Methods");
            sb.AppendLine();

            // 1. 无参构造函数（支持 QUI/IUIService 延迟绑定）
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 无参构造函数，运行时由 QUI 创建后注入 Binding");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public " + className + "() : base()");
            sb.AppendLine("        {");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 2. 带参构造函数 (QUIBinding)，保留给测试或手动包装已存在对象使用
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 使用已有 QUIBinding 构造访问类");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public " + className + "(QUIBinding binding) : base(binding)");
            sb.AppendLine("        {");
            sb.AppendLine("            InitializeFromBinding();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 3. 重写OnBindingSet方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 当Binding被设置时自动调用（支持延迟初始化）");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        protected override void OnBindingSet()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnBindingSet();");
            sb.AppendLine("            InitializeFromBinding();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 4. 静态工厂方法 - Create(QUIBinding)
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 从已有 QUIBinding 创建并初始化实例");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static " + className + " Create(QUIBinding binding)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (binding == null)");
            sb.AppendLine("                throw new System.ArgumentNullException(nameof(binding));");
            sb.AppendLine();
            sb.AppendLine("            return new " + className + "(binding);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 5. 静态工厂方法 - CreateFromGameObject
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 从GameObject查找QUIBinding并创建实例");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static " + className + " CreateFromGameObject(GameObject gameObject)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (gameObject == null)");
            sb.AppendLine("                throw new System.ArgumentNullException(nameof(gameObject));");
            sb.AppendLine();
            sb.AppendLine("            var binding = gameObject.GetComponent<QUIBinding>();");
            sb.AppendLine("            if (binding == null)");
            sb.AppendLine("                throw new System.InvalidOperationException($\"GameObject '{gameObject.name}' does not have a QUIBinding component.\");");
            sb.AppendLine();
            sb.AppendLine("            return Create(binding);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        #endregion");
            sb.AppendLine();

            // 生成字段声明
            sb.AppendLine("        #region Component Fields");
            sb.AppendLine();
            foreach (var codeItem in codeItems.OrderBy(i => i.Item.BindingName))
            {
                sb.AppendLine($"        private {codeItem.TypeName} {codeItem.FieldName};");
            }
            sb.AppendLine();
            sb.AppendLine("        #endregion");
            sb.AppendLine();

            // 生成属性
            sb.AppendLine("        #region Component Properties");
            sb.AppendLine();
            var componentGroups = codeItems
                .GroupBy(item => item.Item.ComponentTypeName)
                .OrderBy(g => g.Key);

            foreach (var group in componentGroups)
            {
                sb.AppendLine($"        // {group.Key} Components");
                foreach (var codeItem in group.OrderBy(i => i.Item.BindingName))
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// {codeItem.Item.DisplayName} - {codeItem.Item.ComponentTypeName}");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public {codeItem.TypeName} {codeItem.PropertyName} => {codeItem.FieldName};");
                    sb.AppendLine();
                }
            }
            sb.AppendLine("        #endregion");
            sb.AppendLine();

            // 生成初始化方法
            sb.AppendLine("        #region Initialization");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 从QUIBinding初始化所有组件引用");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        private void InitializeFromBinding()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (Binding == null)");
            sb.AppendLine("            {");
            sb.AppendLine("                UnityEngine.Debug.LogWarning($\"{GetType().Name}: Binding is null, component initialization skipped.\");");
            sb.AppendLine("                return;");
            sb.AppendLine("            }");
            sb.AppendLine();

            foreach (var codeItem in codeItems.OrderBy(i => i.Item.BindingName))
            {
                sb.AppendLine($"            {codeItem.FieldName} = Binding.GetComponent<{codeItem.TypeName}>(\"{codeItem.Item.BindingName}\");");
                sb.AppendLine($"            if ({codeItem.FieldName} == null)");
                sb.AppendLine($"                UnityEngine.Debug.LogWarning($\"Component '{codeItem.Item.BindingName}' of type {codeItem.Item.ComponentTypeName} not found in binding.\");");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // 重写RefreshComponents方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 重新初始化所有组件引用（重写基类方法）");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public override void RefreshComponents()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.RefreshComponents();");
            sb.AppendLine("            InitializeFromBinding();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // 保持现有的ValidateReferences方法...
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 验证所有组件引用是否有效（重写基类方法）");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public override bool ValidateReferences()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (!base.ValidateReferences()) return false;");
            sb.AppendLine();
            sb.AppendLine("            bool allValid = true;");
            foreach (var codeItem in codeItems.OrderBy(i => i.Item.BindingName))
            {
                sb.AppendLine($"            if ({codeItem.FieldName} == null) {{ UnityEngine.Debug.LogError(\"Missing reference: {codeItem.Item.BindingName}\"); allValid = false; }}");
            }
            sb.AppendLine("            return allValid;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        #endregion");
            sb.AppendLine();

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private sealed class GeneratedBindingCodeItem
        {
            public ComponentBindingItem Item;
            public string TypeName;
            public string FieldName;
            public string PropertyName;
        }

        private List<GeneratedBindingCodeItem> BuildGeneratedCodeItems()
        {
            var result = new List<GeneratedBindingCodeItem>();
            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            var fieldNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in m_target.BindingItems.Where(item => item.Component != null).OrderBy(item => item.BindingName))
            {
                var propertyName = MakeUniqueIdentifier(ToValidIdentifier(item.BindingName, "Binding"), propertyNames);
                var fieldName = MakeUniqueIdentifier("m_" + LowerFirst(propertyName), fieldNames);
                result.Add(new GeneratedBindingCodeItem
                {
                    Item = item,
                    TypeName = GetSourceTypeName(item.Component.GetType()),
                    FieldName = fieldName,
                    PropertyName = propertyName
                });
            }

            return result;
        }

        private void NormalizeBindingNames()
        {
            var usedNames = new HashSet<string>(StringComparer.Ordinal);
            var changed = false;

            foreach (var item in m_target.BindingItems)
            {
                var normalized = MakeUniqueIdentifier(ToValidIdentifier(item.BindingName, "Binding"), usedNames);
                if (item.BindingName != normalized)
                {
                    item.BindingName = normalized;
                    changed = true;
                }
            }

            if (changed)
            {
                MarkBindingDataDirty();
            }
        }

        private string MakeUniqueBindingName(string rawName, ComponentBindingItem ignoredItem = null)
        {
            var usedNames = new HashSet<string>(
                m_target.BindingItems
                    .Where(item => item != ignoredItem)
                    .Select(item => item.BindingName),
                StringComparer.Ordinal);

            return MakeUniqueIdentifier(ToValidIdentifier(rawName, "Binding"), usedNames);
        }

        private static string MakeUniqueIdentifier(string baseName, HashSet<string> usedNames)
        {
            var uniqueName = baseName;
            var counter = 1;
            while (usedNames.Contains(uniqueName))
            {
                uniqueName = $"{baseName}_{counter}";
                counter++;
            }

            usedNames.Add(uniqueName);
            return uniqueName;
        }

        private static string GetSourceTypeName(Type type)
        {
            if (type == null)
            {
                return "global::UnityEngine.Component";
            }

            var fullName = type.FullName ?? type.Name;
            if (!type.IsGenericType)
            {
                return "global::" + fullName.Replace('+', '.');
            }

            var tickIndex = fullName.IndexOf('`');
            var genericTypeName = tickIndex >= 0 ? fullName.Substring(0, tickIndex) : fullName;
            var genericArguments = string.Join(", ", type.GetGenericArguments().Select(GetSourceTypeName));
            return $"global::{genericTypeName.Replace('+', '.')}<{genericArguments}>";
        }

        private static string LowerFirst(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        private static string ToValidIdentifier(string name, string fallback)
        {
            if (string.IsNullOrEmpty(name))
                return fallback;

            var sanitized = new StringBuilder();
            bool firstChar = true;

            foreach (char c in name)
            {
                if (char.IsLetter(c) || (!firstChar && char.IsDigit(c)) || c == '_')
                {
                    sanitized.Append(c);
                    firstChar = false;
                }
                else if (!firstChar && (c == ' ' || c == '-'))
                {
                    sanitized.Append('_');
                }
            }

            var result = sanitized.ToString();

            if (string.IsNullOrEmpty(result) || !char.IsLetter(result[0]))
                result = fallback + "_" + result;

            if (s_csharpKeywords.Contains(result))
                result += "_";

            return result;
        }

        private string SanitizePropertyName(string name)
        {
            return ToValidIdentifier(name, "Unknown");
        }
    }
}
