using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFrameWork.Runtime.Audio;
using EFrameWork.Runtime.Event;
using Lofelt.NiceVibrations;
using UnityEditor;
using UnityEngine;

namespace EFrameWork.Editor.Audio
{
    /// <summary>
    /// 音效事件管理编辑器窗口
    /// 列出所有 IEvent 事件，支持为每个事件配置音效和震动
    /// </summary>
    public class AudioEventEditorWindow : EditorWindow
    {
        #region Constants

        private const float LeftPanelWidth = 280f;
        private const float MinWindowWidth = 900f;
        private const float MinWindowHeight = 600f;
        private const string ConfigAssetPath = AudioResourcePaths.EventConfigAssetPath;

        #endregion

        #region Fields

        private AudioEventConfigAsset _configAsset;
        private List<EventTypeInfo> _allEventTypes = new List<EventTypeInfo>();
        private List<EventTypeInfo> _filteredEventTypes = new List<EventTypeInfo>();

        // UI State
        private Vector2 _leftPanelScroll;
        private Vector2 _rightPanelScroll;
        private int _selectedEventIndex = -1;
        private string _searchFilter = "";
        private string _namespaceFilter = "All";
        private List<string> _namespaces = new List<string> { "All" };
        private bool _showOnlyConfigured = false;

        private Dictionary<string, bool> _namespaceFoldouts = new Dictionary<string, bool>();

        // Event field cache for condition dropdown
        private Dictionary<Type, List<EventFieldInfo>> _eventFieldCache = new Dictionary<Type, List<EventFieldInfo>>();

        // Preview
        private AudioSource _previewAudioSource;
        private Dictionary<string, int> _previewSequentialIndexByGroup = new Dictionary<string, int>();

        #endregion

        #region Inner Classes

        private class EventTypeInfo
        {
            public Type EventType;
            public string FullName;
            public string ShortName;
            public string Namespace;
            public bool HasConfig;
        }

        private class EventFieldInfo
        {
            public string Name;
            public string Path;
            public Type FieldType;
            public string DisplayName;
        }

        #endregion

        #region Menu

        public static void ShowWindow()
        {
            var window = GetWindow<AudioEventEditorWindow>();
            window.titleContent = new GUIContent("Audio Event Editor", EditorGUIUtility.IconContent("AudioSource Icon").image);
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        }

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            LoadOrCreateConfigAsset();
            ScanAllEventTypes();
            CreatePreviewAudioSource();
        }

        private void OnDisable()
        {
            DestroyPreviewAudioSource();
            SaveConfigAsset();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // Left Panel - Event List
            DrawLeftPanel();

            // Right Panel - Config Editor
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();

            // Handle keyboard shortcuts
            HandleKeyboardShortcuts();
        }

        #endregion

        #region Config Asset Management

        private void LoadOrCreateConfigAsset()
        {
            _configAsset = AssetDatabase.LoadAssetAtPath<AudioEventConfigAsset>(ConfigAssetPath);
            if (_configAsset == null)
            {
                // Create directory if not exists
                string directory = System.IO.Path.GetDirectoryName(ConfigAssetPath);
                if (!AssetDatabase.IsValidFolder(directory))
                {
                    string[] folders = directory.Split('/');
                    string currentPath = folders[0];
                    for (int i = 1; i < folders.Length; i++)
                    {
                        string newPath = currentPath + "/" + folders[i];
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[i]);
                        }
                        currentPath = newPath;
                    }
                }

                _configAsset = CreateInstance<AudioEventConfigAsset>();
                AssetDatabase.CreateAsset(_configAsset, ConfigAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[AudioEventEditor] Created new config asset at {ConfigAssetPath}");
            }
        }

        private void SaveConfigAsset()
        {
            if (_configAsset != null)
            {
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
            }
        }

        #endregion

        #region Event Type Scanning

        private void ScanAllEventTypes()
        {
            _allEventTypes.Clear();
            _namespaces.Clear();
            _namespaces.Add("All");

            var eventInterface = typeof(IEvent);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsValueType && eventInterface.IsAssignableFrom(t) && t != eventInterface);

                    foreach (var type in types)
                    {
                        var info = new EventTypeInfo
                        {
                            EventType = type,
                            FullName = type.FullName,
                            ShortName = type.Name,
                            Namespace = type.Namespace ?? "Global",
                            HasConfig = _configAsset?.FindByEventType(type.FullName) != null
                        };
                        _allEventTypes.Add(info);

                        if (!_namespaces.Contains(info.Namespace))
                        {
                            _namespaces.Add(info.Namespace);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                }
            }

            _allEventTypes = _allEventTypes.OrderBy(e => e.Namespace).ThenBy(e => e.ShortName).ToList();
            _namespaces.Sort();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            _filteredEventTypes = _allEventTypes.Where(e =>
            {
                // Namespace filter
                if (_namespaceFilter != "All" && e.Namespace != _namespaceFilter)
                    return false;

                // Search filter
                if (!string.IsNullOrEmpty(_searchFilter))
                {
                    if (!e.ShortName.ToLower().Contains(_searchFilter.ToLower()) &&
                        !e.FullName.ToLower().Contains(_searchFilter.ToLower()))
                        return false;
                }

                // Only configured filter
                if (_showOnlyConfigured && !e.HasConfig)
                    return false;

                return true;
            }).ToList();
        }

        private void RefreshEventConfigStatus()
        {
            foreach (var info in _allEventTypes)
            {
                info.HasConfig = _configAsset?.FindByEventType(info.FullName) != null;
            }
            ApplyFilters();
        }

        #endregion

        #region Left Panel

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(LeftPanelWidth), GUILayout.ExpandHeight(true));

            // Header
            EditorGUILayout.LabelField("游戏事件列表", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Search bar
            EditorGUI.BeginChangeCheck();
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyFilters();
            }

            // Filters
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int namespaceIndex = _namespaces.IndexOf(_namespaceFilter);
            if (namespaceIndex < 0) namespaceIndex = 0;
            namespaceIndex = EditorGUILayout.Popup(namespaceIndex, _namespaces.ToArray(), GUILayout.Width(140));
            _namespaceFilter = _namespaces[namespaceIndex];

            _showOnlyConfigured = GUILayout.Toggle(_showOnlyConfigured, "仅显示已配置", GUILayout.Width(90));
            if (EditorGUI.EndChangeCheck())
            {
                ApplyFilters();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Stats
            EditorGUILayout.LabelField($"共 {_filteredEventTypes.Count} / {_allEventTypes.Count} 个事件", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            // Event list
            _leftPanelScroll = EditorGUILayout.BeginScrollView(_leftPanelScroll);

            string currentNamespace = null;
            bool currentNamespaceExpanded = true;

            for (int i = 0; i < _filteredEventTypes.Count; i++)
            {
                var info = _filteredEventTypes[i];

                // Namespace header (collapsible)
                if (info.Namespace != currentNamespace)
                {
                    currentNamespace = info.Namespace;

                    // Initialize foldout state if not exists
                    if (!_namespaceFoldouts.ContainsKey(currentNamespace))
                    {
                        _namespaceFoldouts[currentNamespace] = true;
                    }

                    EditorGUILayout.Space(4);

                    // Count events in this namespace
                    int nsEventCount = _filteredEventTypes.Count(e => e.Namespace == currentNamespace);
                    int nsConfiguredCount = _filteredEventTypes.Count(e => e.Namespace == currentNamespace && e.HasConfig);

                    // Draw foldout header
                    EditorGUILayout.BeginHorizontal();
                    var foldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold
                    };
                    _namespaceFoldouts[currentNamespace] = EditorGUILayout.Foldout(
                        _namespaceFoldouts[currentNamespace],
                        currentNamespace,
                        true,
                        foldoutStyle);

                    // Show count badge
                    var countStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleRight
                    };
                    if (nsConfiguredCount > 0)
                    {
                        GUI.color = new Color(0.5f, 0.8f, 0.5f);
                    }
                    EditorGUILayout.LabelField($"[{nsConfiguredCount}/{nsEventCount}]", countStyle, GUILayout.Width(50));
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();

                    currentNamespaceExpanded = _namespaceFoldouts[currentNamespace];
                }

                // Only draw event item if namespace is expanded
                if (currentNamespaceExpanded)
                {
                    // Event item
                    DrawEventListItem(info, i);
                }
            }

            EditorGUILayout.EndScrollView();

            // Toolbar buttons
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新事件", GUILayout.Height(25)))
            {
                ScanAllEventTypes();
            }
            if (GUILayout.Button("保存配置", GUILayout.Height(25)))
            {
                SaveConfigAsset();
                EditorUtility.DisplayDialog("保存成功", "配置已保存", "确定");
            }
            if (GUILayout.Button("创建配置", GUILayout.Height(25)))
            {
                AudioEventConfigCreator.CreateAudioEventConfig();
                LoadOrCreateConfigAsset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawEventListItem(EventTypeInfo info, int index)
        {
            bool isSelected = _selectedEventIndex == index;
            var oldBgColor = GUI.backgroundColor;

            // Color based on config status
            if (info.HasConfig)
            {
                GUI.backgroundColor = isSelected ?
                    new Color(0.3f, 0.6f, 0.3f, 1f) :
                    new Color(0.2f, 0.4f, 0.2f, 1f);
            }
            else if (isSelected)
            {
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            }

            EditorGUILayout.BeginHorizontal("box");

            // Config indicator
            var iconStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            if (info.HasConfig)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("♪", iconStyle, GUILayout.Width(18));
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("○", iconStyle, GUILayout.Width(18));
            }

            // Event name button
            if (GUILayout.Button(info.ShortName, EditorStyles.label))
            {
                _selectedEventIndex = index;
            }

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = oldBgColor;
        }

        #endregion

        #region Right Panel

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selectedEventIndex < 0 || _selectedEventIndex >= _filteredEventTypes.Count)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox("从左侧选择一个事件来编辑音效配置。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var selectedEvent = _filteredEventTypes[_selectedEventIndex];
            var configItem = _configAsset?.FindByEventType(selectedEvent.FullName);
            bool hasConfig = configItem != null;

            // Header
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"事件: {selectedEvent.ShortName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (!hasConfig)
            {
                if (GUILayout.Button("+ 添加音效配置", GUILayout.Width(120), GUILayout.Height(24)))
                {
                    configItem = new AudioEventConfigItem
                    {
                        EventTypeName = selectedEvent.FullName,
                        Enabled = true,
                        ConditionalGroups = new List<ConditionalAudioGroup>
                        {
                            new ConditionalAudioGroup
                            {
                                GroupName = "默认音效",
                                Enabled = true
                                // Conditions 为空 = 无条件 = 始终播放
                            }
                        }
                    };
                    _configAsset.AddOrUpdateItem(configItem);
                    RefreshEventConfigStatus();
                    SaveConfigAsset();
                }
            }
            else
            {
                if (GUILayout.Button("- 移除配置", GUILayout.Width(100), GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("确认移除", $"确定要移除 {selectedEvent.ShortName} 的音效配置吗？", "确定", "取消"))
                    {
                        _configAsset.RemoveItem(selectedEvent.FullName);
                        RefreshEventConfigStatus();
                        SaveConfigAsset();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"全名: {selectedEvent.FullName}", EditorStyles.miniLabel);
            EditorGUILayout.Space(8);

            if (!hasConfig)
            {
                EditorGUILayout.HelpBox("此事件尚未配置音效。点击上方按钮添加配置。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Config editor
            _rightPanelScroll = EditorGUILayout.BeginScrollView(_rightPanelScroll);

            EditorGUI.BeginChangeCheck();

            // Enable toggle
            configItem.Enabled = EditorGUILayout.Toggle("启用", configItem.Enabled);
            EditorGUILayout.Space(8);

            // Conditional Groups Section (all audio config is here now)
            DrawConditionalGroupsSection(configItem);

            if (EditorGUI.EndChangeCheck())
            {
                _configAsset.AddOrUpdateItem(configItem);
                EditorUtility.SetDirty(_configAsset);
            }

            EditorGUILayout.EndScrollView();

            // Preview buttons
            EditorGUILayout.Space(8);
            DrawPreviewButtons(configItem);

            EditorGUILayout.EndVertical();
        }

        #region Conditional Groups Section

        private Dictionary<int, bool> _fadeFoldouts = new Dictionary<int, bool>();
        private Dictionary<int, bool> _vibrationFoldouts = new Dictionary<int, bool>();

        private void DrawConditionalGroupsSection(AudioEventConfigItem configItem)
        {
            // 直接绘制每个音效组
            for (int i = 0; i < configItem.ConditionalGroups.Count; i++)
            {
                DrawConditionalGroup(configItem.ConditionalGroups[i], i, configItem);
                EditorGUILayout.Space(4);
            }

            // 添加组按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 添加事件音效", GUILayout.Width(120)))
            {
                configItem.ConditionalGroups.Add(new ConditionalAudioGroup
                {
                    GroupName = $"音效组 {configItem.ConditionalGroups.Count + 1}",
                    Enabled = true
                });
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);
        }

        private void DrawConditionalGroup(ConditionalAudioGroup group, int groupIndex, AudioEventConfigItem configItem)
        {
            bool isDefaultGroup = group.Conditions.Count == 0;

            EditorGUILayout.BeginVertical("box");

            // ============ 条件区 - 深蓝色背景 ============
            var conditionBoxStyle = new GUIStyle("box");
            conditionBoxStyle.normal.background = MakeTex(2, 2, new Color(0.16f, 0.20f, 0.26f, 1f));
            EditorGUILayout.BeginVertical(conditionBoxStyle);

            EditorGUILayout.BeginHorizontal();
            string conditionLabel = isDefaultGroup ? "条件 (无条件 = 始终播放)" : "条件";
            EditorGUILayout.LabelField(conditionLabel, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (group.Conditions.Count > 1)
            {
                // 逻辑切换按钮
                string logicText = group.Logic == ConditionLogic.And ? "全部满足 (AND)" : "任一满足 (OR)";
                if (GUILayout.Button(logicText, GUILayout.Width(100)))
                {
                    group.Logic = group.Logic == ConditionLogic.And ? ConditionLogic.Or : ConditionLogic.And;
                }
            }
            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                group.Conditions.Add(new AudioEventCondition());
            }
            // 删除组按钮
            if (configItem.ConditionalGroups.Count > 1)
            {
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    configItem.ConditionalGroups.RemoveAt(groupIndex);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();

            for (int j = 0; j < group.Conditions.Count; j++)
            {
                DrawConditionInline(group.Conditions[j], j, group);

                // 在条件之间显示逻辑分隔符
                if (j < group.Conditions.Count - 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUI.color = group.Logic == ConditionLogic.And ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.6f, 0.2f);
                    GUILayout.Label(group.Logic == ConditionLogic.And ? "── AND ──" : "── OR ──", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            // ============ 音效参数区 - 深紫色背景 ============
            var audioBoxStyle = new GUIStyle("box");
            audioBoxStyle.normal.background = MakeTex(2, 2, new Color(0.22f, 0.18f, 0.26f, 1f));
            EditorGUILayout.BeginVertical(audioBoxStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("播放模式", GUILayout.Width(60));
            group.PlayMode = (AudioPlayMode)EditorGUILayout.EnumPopup(group.PlayMode);
            EditorGUILayout.EndHorizontal();

            // Audio clips
            EditorGUILayout.LabelField($"音频剪辑 ({group.AudioClips.Count})");
            for (int k = 0; k < group.AudioClips.Count; k++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{k}]", GUILayout.Width(25));

                var entry = group.AudioClips[k];
                entry.Clip = (AudioClip)EditorGUILayout.ObjectField(entry.Clip, typeof(AudioClip), false);

                entry.Weight = EditorGUILayout.Slider(entry.Weight, 0f, 1f, GUILayout.Width(100));

                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    group.AudioClips.RemoveAt(k);
                    k--;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 添加音频", GUILayout.Width(100)))
            {
                group.AudioClips.Add(new AudioClipEntry());
            }
            EditorGUILayout.EndHorizontal();

            // Playback parameters section
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("播放参数", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("音轨", GUILayout.Width(60));
            group.TrackType = (AudioTrackType)EditorGUILayout.EnumPopup(group.TrackType);
            EditorGUILayout.EndHorizontal();

            // Volume
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("音量", GUILayout.Width(60));
            group.Volume.Min = EditorGUILayout.FloatField(group.Volume.Min, GUILayout.Width(40));
            EditorGUILayout.MinMaxSlider(ref group.Volume.Min, ref group.Volume.Max, 0f, 1f);
            group.Volume.Max = EditorGUILayout.FloatField(group.Volume.Max, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            // Pitch
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("音调", GUILayout.Width(60));
            group.Pitch.Min = EditorGUILayout.FloatField(group.Pitch.Min, GUILayout.Width(40));
            EditorGUILayout.MinMaxSlider(ref group.Pitch.Min, ref group.Pitch.Max, 0.5f, 1.5f);
            group.Pitch.Max = EditorGUILayout.FloatField(group.Pitch.Max, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            group.Loop = EditorGUILayout.Toggle("循环播放", group.Loop);
            group.MinInterval = EditorGUILayout.Slider("重复播放间隔 (秒)", group.MinInterval, 0.03f, 2f);
            group.Delay = EditorGUILayout.Slider("延时播放 (秒)", group.Delay, 0f, 10f);

            // Fade settings section (collapsible, default collapsed)
            EditorGUILayout.Space(4);
            if (!_fadeFoldouts.ContainsKey(groupIndex)) _fadeFoldouts[groupIndex] = false;
            EditorGUILayout.BeginHorizontal();
            _fadeFoldouts[groupIndex] = EditorGUILayout.Foldout(_fadeFoldouts[groupIndex], "淡入淡出", true);
            if (!_fadeFoldouts[groupIndex] && (group.FadeInDuration > 0 || group.FadeOutDuration > 0))
            {
                GUI.color = Color.gray;
                GUILayout.Label($"(In:{group.FadeInDuration:F1}s Out:{group.FadeOutDuration:F1}s)", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            if (_fadeFoldouts[groupIndex])
            {
                EditorGUI.indentLevel++;
                // Fade in
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("淡入", GUILayout.Width(40));
                group.FadeInDuration = EditorGUILayout.FloatField(group.FadeInDuration, GUILayout.Width(50));
                EditorGUILayout.LabelField("s", GUILayout.Width(15));
                if (group.FadeInDuration > 0)
                {
                    group.FadeInCurve = EditorGUILayout.CurveField(group.FadeInCurve, Color.green, new Rect(0, 0, 1, 1), GUILayout.Height(20), GUILayout.ExpandWidth(true));
                }
                EditorGUILayout.EndHorizontal();
                // Fade out
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("淡出", GUILayout.Width(40));
                group.FadeOutDuration = EditorGUILayout.FloatField(group.FadeOutDuration, GUILayout.Width(50));
                EditorGUILayout.LabelField("s", GUILayout.Width(15));
                if (group.FadeOutDuration > 0)
                {
                    group.FadeOutCurve = EditorGUILayout.CurveField(group.FadeOutCurve, Color.red, new Rect(0, 0, 1, 1), GUILayout.Height(20), GUILayout.ExpandWidth(true));
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            // Vibration (collapsible, default collapsed)
            EditorGUILayout.Space(4);
            if (!_vibrationFoldouts.ContainsKey(groupIndex)) _vibrationFoldouts[groupIndex] = false;
            EditorGUILayout.BeginHorizontal();
            _vibrationFoldouts[groupIndex] = EditorGUILayout.Foldout(_vibrationFoldouts[groupIndex], "震动设置", true);
            if (!_vibrationFoldouts[groupIndex] && group.VibrationMode != VibrationMode.None)
            {
                GUI.color = Color.gray;
                GUILayout.Label($"({group.VibrationMode})", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            if (_vibrationFoldouts[groupIndex])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("模式", GUILayout.Width(40));
                group.VibrationMode = (VibrationMode)EditorGUILayout.EnumPopup(group.VibrationMode);
                if (group.VibrationMode == VibrationMode.Preset)
                {
                    group.VibrationPreset = (HapticPatterns.PresetType)EditorGUILayout.EnumPopup(group.VibrationPreset);
                }
                else if (group.VibrationMode == VibrationMode.Custom)
                {
                    EditorGUILayout.LabelField("强度", GUILayout.Width(40));
                    group.VibrationAmplitude = EditorGUILayout.Slider(group.VibrationAmplitude, 0f, 1f, GUILayout.Width(100));
                    EditorGUILayout.LabelField("频率", GUILayout.Width(40));
                    group.VibrationFrequency = EditorGUILayout.Slider(group.VibrationFrequency, 0f, 1f, GUILayout.Width(100));
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical(); // 结束音效参数区

            EditorGUILayout.EndVertical(); // 结束整个组
        }

        /// <summary>
        /// 紧凑型条件绘制 - 单行显示: [字段] [运算符] [值] [×]
        /// </summary>
        private void DrawConditionInline(AudioEventCondition condition, int conditionIndex, ConditionalAudioGroup group)
        {
            // 获取一整行的 Rect，然后手动分割
            Rect lineRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            float x = lineRect.x;
            float y = lineRect.y;
            float spacing = 2f;

            // Field path - hierarchical dropdowns (compact)
            var selectedEvent = GetSelectedEventType();
            if (selectedEvent != null)
            {
                x = DrawCompactFieldPath(condition, selectedEvent, x, y, spacing);
            }
            else
            {
                Rect fieldRect = new Rect(x, y, 120, 18);
                condition.FieldPath = EditorGUI.TextField(fieldRect, condition.FieldPath);
                x += 120 + spacing;
            }

            // Operator - compact display
            Rect opRect = new Rect(x, y, 110, 18);
            condition.Operator = (ConditionOperator)EditorGUI.EnumPopup(opRect, condition.Operator);
            x += 110 + spacing;

            // Value field based on operator
            if (condition.Operator == ConditionOperator.IsType)
            {
                DrawCompactTypeDropdown(condition, x, y);
                x += 120 + spacing;
            }
            else if (condition.Operator != ConditionOperator.IsNull && condition.Operator != ConditionOperator.IsNotNull)
            {
                DrawCompactValueField(condition, x, y);
                x += 120 + spacing;
            }

            // Delete button - 右侧对齐，红色背景
            float btnWidth = 18;
            Rect btnRect = new Rect(lineRect.xMax - btnWidth, y, btnWidth, 18);
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            if (GUI.Button(btnRect, "×"))
            {
                group.Conditions.RemoveAt(conditionIndex);
                GUI.backgroundColor = oldBgColor;
                return;
            }
            GUI.backgroundColor = oldBgColor;
        }

        /// <summary>
        /// 紧凑型字段路径选择 - 返回下一个控件的 x 位置
        /// </summary>
        private float DrawCompactFieldPath(AudioEventCondition condition, Type eventType, float startX, float y, float spacing)
        {
            string[] pathSegments = string.IsNullOrEmpty(condition.FieldPath)
                ? new string[0]
                : condition.FieldPath.Split('.');

            var currentType = eventType;
            var newPathSegments = new List<string>();
            bool previousLevelSelected = true;
            float x = startX;

            // 最多显示3层下拉
            for (int level = 0; level <= 2; level++)
            {
                if (!previousLevelSelected) break;

                var members = GetTypeMembers(currentType);
                if (members.Count == 0) break;

                string currentSelection = level < pathSegments.Length ? pathSegments[level] : "";

                string[] names = members.Select(m => m.Name).Prepend(level == 0 ? "(字段)" : "·").ToArray();
                int currentIndex = Array.IndexOf(names, currentSelection);
                if (currentIndex < 0) currentIndex = 0;

                float width = level == 0 ? 120 : 100;
                Rect rect = new Rect(x, y, width, 18);
                int newIndex = EditorGUI.Popup(rect, currentIndex, names);
                x += width + spacing;

                if (newIndex > 0)
                {
                    newPathSegments.Add(names[newIndex]);
                    var selectedMember = members.FirstOrDefault(m => m.Name == names[newIndex]);
                    if (selectedMember != null)
                    {
                        currentType = selectedMember.MemberType;
                        previousLevelSelected = true;
                    }
                    else
                    {
                        previousLevelSelected = false;
                    }
                }
                else
                {
                    previousLevelSelected = false;
                }
            }

            condition.FieldPath = string.Join(".", newPathSegments);
            return x;
        }

        /// <summary>
        /// 紧凑型类型下拉
        /// </summary>
        private void DrawCompactTypeDropdown(AudioEventCondition condition, float x, float y)
        {
            var derivedTypes = GetDerivedTypesForField(condition.FieldPath);
            if (derivedTypes.Count > 0)
            {
                string[] typeNames = derivedTypes.Select(t => t.Name).Prepend("(类型)").ToArray();
                string currentTypeName = string.IsNullOrEmpty(condition.TypeName) ? ""
                    : condition.TypeName.Contains(".") ? condition.TypeName.Substring(condition.TypeName.LastIndexOf('.') + 1) : condition.TypeName;
                int currentIndex = Array.IndexOf(typeNames, currentTypeName);
                if (currentIndex < 0) currentIndex = 0;

                Rect rect = new Rect(x, y, 120, 18);
                int newIndex = EditorGUI.Popup(rect, currentIndex, typeNames);
                if (newIndex > 0)
                {
                    condition.TypeName = derivedTypes[newIndex - 1].FullName;
                }
            }
            else
            {
                Rect rect = new Rect(x, y, 120, 18);
                condition.TypeName = EditorGUI.TextField(rect, condition.TypeName);
            }
        }

        /// <summary>
        /// 紧凑型值输入
        /// </summary>
        private void DrawCompactValueField(AudioEventCondition condition, float x, float y)
        {
            var fieldType = GetFieldTypeFromPath(condition.FieldPath);

            if (fieldType != null && fieldType.IsEnum)
            {
                var enumValues = Enum.GetNames(fieldType);
                int currentIndex = Array.IndexOf(enumValues, condition.CompareValue);
                if (currentIndex < 0) currentIndex = 0;
                Rect rect = new Rect(x, y, 120, 18);
                int newIndex = EditorGUI.Popup(rect, currentIndex, enumValues);
                condition.CompareValue = enumValues[newIndex];
            }
            else
            {
                Rect rect = new Rect(x, y, 80, 18);
                condition.CompareValue = EditorGUI.TextField(rect, condition.CompareValue);
            }
        }

        private void DrawCondition(AudioEventCondition condition, int conditionIndex, ConditionalAudioGroup group)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            // Delete button first
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                group.Conditions.RemoveAt(conditionIndex);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField($"条件 {conditionIndex + 1}", EditorStyles.boldLabel, GUILayout.Width(60));

            // Operator
            condition.Operator = (ConditionOperator)EditorGUILayout.EnumPopup(condition.Operator);

            EditorGUILayout.EndHorizontal();

            // Field path - hierarchical dropdowns
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("字段路径", GUILayout.Width(60));

            var selectedEvent = GetSelectedEventType();
            if (selectedEvent != null)
            {
                DrawHierarchicalFieldPath(condition, selectedEvent);
            }
            else
            {
                condition.FieldPath = EditorGUILayout.TextField(condition.FieldPath);
            }

            EditorGUILayout.EndHorizontal();

            // Value row (based on operator)
            if (condition.Operator == ConditionOperator.IsType)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("目标类型", GUILayout.Width(60));
                DrawTypeDropdown(condition);
                EditorGUILayout.EndHorizontal();
            }
            else if (condition.Operator != ConditionOperator.IsNull && condition.Operator != ConditionOperator.IsNotNull)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("比较值", GUILayout.Width(60));
                DrawValueField(condition);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHierarchicalFieldPath(AudioEventCondition condition, Type eventType)
        {
            // Parse current field path into segments
            string[] pathSegments = string.IsNullOrEmpty(condition.FieldPath)
                ? new string[0]
                : condition.FieldPath.Split('.');

            var currentType = eventType;
            var newPathSegments = new List<string>();
            bool continueHierarchy = true;

            for (int level = 0; level <= 3 && continueHierarchy; level++)
            {
                var members = GetTypeMembers(currentType);
                if (members.Count == 0) break;

                // Current selection at this level
                string currentSelection = level < pathSegments.Length ? pathSegments[level] : "";

                // Build dropdown options
                string[] options = members.Select(m => $"{m.Name} ({GetTypeName(m.MemberType)})").Prepend(level == 0 ? "(选择字段)" : "(停止)").ToArray();
                string[] names = members.Select(m => m.Name).Prepend("").ToArray();

                int currentIndex = Array.IndexOf(names, currentSelection);
                if (currentIndex < 0) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup(currentIndex, options, GUILayout.Width(130));

                if (newIndex == 0)
                {
                    // User selected "stop" or nothing - end here
                    continueHierarchy = false;
                }
                else
                {
                    string selectedName = names[newIndex];
                    newPathSegments.Add(selectedName);

                    // Find the selected member's type for next level
                    var selectedMember = members.FirstOrDefault(m => m.Name == selectedName);
                    if (selectedMember != null && ShouldRecurse(selectedMember.MemberType))
                    {
                        currentType = selectedMember.MemberType;
                        // Continue to next level
                    }
                    else
                    {
                        continueHierarchy = false;
                    }
                }
            }

            // Update field path
            condition.FieldPath = string.Join(".", newPathSegments);

            // Show final path as label
            if (!string.IsNullOrEmpty(condition.FieldPath))
            {
                EditorGUILayout.LabelField($"= {condition.FieldPath}", EditorStyles.miniLabel, GUILayout.Width(100));
            }
        }

        private List<TypeMemberInfo> GetTypeMembers(Type type)
        {
            var result = new List<TypeMemberInfo>();
            if (type == null || type == typeof(object)) return result;

            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            // Fields
            foreach (var field in type.GetFields(bindingFlags))
            {
                result.Add(new TypeMemberInfo
                {
                    Name = field.Name,
                    MemberType = field.FieldType
                });
            }

            // Properties
            foreach (var prop in type.GetProperties(bindingFlags))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
                result.Add(new TypeMemberInfo
                {
                    Name = prop.Name,
                    MemberType = prop.PropertyType
                });
            }

            return result;
        }

        private class TypeMemberInfo
        {
            public string Name;
            public Type MemberType;
        }

        private Type GetSelectedEventType()
        {
            if (_selectedEventIndex >= 0 && _selectedEventIndex < _filteredEventTypes.Count)
            {
                return _filteredEventTypes[_selectedEventIndex].EventType;
            }
            return null;
        }

        private List<EventFieldInfo> GetEventFields(Type eventType)
        {
            if (_eventFieldCache.TryGetValue(eventType, out var cached))
            {
                return cached;
            }

            var fields = new List<EventFieldInfo>();
            CollectFields(eventType, "", fields, 0);
            _eventFieldCache[eventType] = fields;
            return fields;
        }

        private void CollectFields(Type type, string prefix, List<EventFieldInfo> result, int depth)
        {
            if (depth > 2 || type == null || type == typeof(object)) return;

            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            // Fields
            foreach (var field in type.GetFields(bindingFlags))
            {
                string path = string.IsNullOrEmpty(prefix) ? field.Name : $"{prefix}.{field.Name}";
                string displayName = depth > 0 ? $"  {path}" : field.Name;

                result.Add(new EventFieldInfo
                {
                    Name = field.Name,
                    Path = path,
                    FieldType = field.FieldType,
                    DisplayName = $"{displayName} ({GetTypeName(field.FieldType)})"
                });

                // Recurse for complex types (but not primitives, strings, or common Unity types)
                if (ShouldRecurse(field.FieldType))
                {
                    CollectFields(field.FieldType, path, result, depth + 1);
                }
            }

            // Properties
            foreach (var prop in type.GetProperties(bindingFlags))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;

                string path = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                string displayName = depth > 0 ? $"  {path}" : prop.Name;

                result.Add(new EventFieldInfo
                {
                    Name = prop.Name,
                    Path = path,
                    FieldType = prop.PropertyType,
                    DisplayName = $"{displayName} ({GetTypeName(prop.PropertyType)})"
                });

                if (ShouldRecurse(prop.PropertyType))
                {
                    CollectFields(prop.PropertyType, path, result, depth + 1);
                }
            }
        }

        private bool ShouldRecurse(Type type)
        {
            if (type.IsPrimitive || type.IsEnum || type == typeof(string)) return false;
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine")) return false;
            if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type)) return false;
            return true;
        }

        private string GetTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            return type.Name;
        }

        private Type GetFieldTypeFromPath(string fieldPath)
        {
            var selectedEvent = GetSelectedEventType();
            if (selectedEvent == null) return null;
            return GetFieldTypeFromPath(selectedEvent, fieldPath);
        }

        private Type GetFieldTypeFromPath(Type rootType, string fieldPath)
        {
            if (string.IsNullOrEmpty(fieldPath) || rootType == null) return null;

            var segments = fieldPath.Split('.');
            var currentType = rootType;

            foreach (var segment in segments)
            {
                var members = GetTypeMembers(currentType);
                var member = members.FirstOrDefault(m => m.Name == segment);
                if (member == null) return null;
                currentType = member.MemberType;
            }

            return currentType;
        }

        private void DrawTypeDropdown(AudioEventCondition condition)
        {
            var selectedEvent = GetSelectedEventType();
            if (selectedEvent != null && !string.IsNullOrEmpty(condition.FieldPath))
            {
                var fieldType = GetFieldTypeFromPath(selectedEvent, condition.FieldPath);

                if (fieldType != null && !fieldType.IsPrimitive && fieldType != typeof(string))
                {
                    // Get derived types for the field type
                    var derivedTypes = GetDerivedTypes(fieldType);
                    if (derivedTypes.Count > 0)
                    {
                        string[] typeNames = derivedTypes.Select(t => t.Name).Prepend("(自定义)").ToArray();
                        int currentIndex = Array.IndexOf(typeNames, condition.TypeName);
                        if (currentIndex < 0) currentIndex = 0;

                        int newIndex = EditorGUILayout.Popup(currentIndex, typeNames, GUILayout.Width(150));
                        if (newIndex == 0)
                        {
                            condition.TypeName = EditorGUILayout.TextField(condition.TypeName);
                        }
                        else
                        {
                            condition.TypeName = typeNames[newIndex];
                        }
                        return;
                    }
                }
            }
            condition.TypeName = EditorGUILayout.TextField(condition.TypeName);
        }

        private void DrawValueField(AudioEventCondition condition)
        {
            var selectedEvent = GetSelectedEventType();
            if (selectedEvent != null && !string.IsNullOrEmpty(condition.FieldPath))
            {
                var fieldType = GetFieldTypeFromPath(selectedEvent, condition.FieldPath);

                if (fieldType != null)
                {
                    // For enum types, show a dropdown
                    if (fieldType.IsEnum)
                    {
                        string[] enumNames = Enum.GetNames(fieldType);
                        int currentIndex = Array.IndexOf(enumNames, condition.CompareValue);
                        if (currentIndex < 0) currentIndex = 0;

                        int newIndex = EditorGUILayout.Popup(currentIndex, enumNames, GUILayout.Width(150));
                        condition.CompareValue = enumNames[newIndex];
                        return;
                    }

                    // For bool types, show a dropdown
                    if (fieldType == typeof(bool))
                    {
                        string[] boolNames = new[] { "True", "False" };
                        int currentIndex = condition.CompareValue?.ToLower() == "false" ? 1 : 0;
                        int newIndex = EditorGUILayout.Popup(currentIndex, boolNames, GUILayout.Width(80));
                        condition.CompareValue = boolNames[newIndex];
                        return;
                    }
                }
            }
            condition.CompareValue = EditorGUILayout.TextField(condition.CompareValue);
        }

        private List<Type> _derivedTypesCache = new List<Type>();
        private Type _lastBaseType = null;

        private List<Type> GetDerivedTypesForField(string fieldPath)
        {
            var fieldType = GetFieldTypeFromPath(fieldPath);
            if (fieldType == null) return new List<Type>();
            return GetDerivedTypes(fieldType);
        }

        private List<Type> GetDerivedTypes(Type baseType)
        {
            if (_lastBaseType == baseType && _derivedTypesCache.Count > 0)
            {
                return _derivedTypesCache;
            }

            _lastBaseType = baseType;
            _derivedTypesCache.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type != baseType && baseType.IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            _derivedTypesCache.Add(type);
                        }
                    }
                }
                catch { }
            }

            return _derivedTypesCache;
        }

        #endregion

        private void DrawPreviewButtons(AudioEventConfigItem configItem)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("▶ 预览音效", GUILayout.Width(100), GUILayout.Height(30)))
            {
                PreviewAudio(configItem);
            }

            if (GUILayout.Button("■ 停止", GUILayout.Width(80), GUILayout.Height(30)))
            {
                StopPreview();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Preview

        private void CreatePreviewAudioSource()
        {
            if (_previewAudioSource == null)
            {
                var go = new GameObject("AudioEventEditor_Preview");
                go.hideFlags = HideFlags.HideInHierarchy;
                _previewAudioSource = go.AddComponent<AudioSource>();
            }
        }

        private void DestroyPreviewAudioSource()
        {
            if (_previewAudioSource != null)
            {
                DestroyImmediate(_previewAudioSource.gameObject);
                _previewAudioSource = null;
            }
        }

        private void PreviewAudio(AudioEventConfigItem configItem)
        {
            // Find first enabled group with audio clips
            ConditionalAudioGroup groupToPreview = null;
            foreach (var group in configItem.ConditionalGroups)
            {
                if (group.Enabled && group.AudioClips.Count > 0)
                {
                    groupToPreview = group;
                    break;
                }
            }

            if (groupToPreview == null)
            {
                Debug.LogWarning("[AudioEventEditor] No audio clips configured in any group");
                return;
            }

            AudioClip clipToPlay = null;
            int clipIndex = 0;

            switch (groupToPreview.PlayMode)
            {
                case AudioPlayMode.Random:
                    clipIndex = UnityEngine.Random.Range(0, groupToPreview.AudioClips.Count);
                    break;

                case AudioPlayMode.Sequential:
                    string groupKey = $"{configItem.EventTypeName}_{groupToPreview.GroupName}";
                    if (!_previewSequentialIndexByGroup.TryGetValue(groupKey, out clipIndex))
                    {
                        clipIndex = 0;
                    }
                    _previewSequentialIndexByGroup[groupKey] = (clipIndex + 1) % groupToPreview.AudioClips.Count;
                    break;

                case AudioPlayMode.WeightedRandom:
                    clipIndex = GetWeightedClipIndexFromGroup(groupToPreview);
                    break;
            }

            var entry = groupToPreview.AudioClips[clipIndex];
            clipToPlay = entry.Clip;

            if (clipToPlay != null && _previewAudioSource != null)
            {
                _previewAudioSource.clip = clipToPlay;
                _previewAudioSource.volume = groupToPreview.Volume.RandomValue;
                _previewAudioSource.pitch = groupToPreview.Pitch.RandomValue;
                _previewAudioSource.loop = groupToPreview.Loop;
                _previewAudioSource.Play();
            }
        }

        private int GetWeightedClipIndexFromGroup(ConditionalAudioGroup group)
        {
            if (group.AudioClips.Count == 0) return 0;

            float totalWeight = 0f;
            foreach (var clip in group.AudioClips)
            {
                totalWeight += clip.Weight;
            }

            if (totalWeight <= 0f)
                return UnityEngine.Random.Range(0, group.AudioClips.Count);

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < group.AudioClips.Count; i++)
            {
                cumulative += group.AudioClips[i].Weight;
                if (random <= cumulative)
                    return i;
            }

            return group.AudioClips.Count - 1;
        }

        private void StopPreview()
        {
            if (_previewAudioSource != null)
            {
                _previewAudioSource.Stop();
            }
        }

        #endregion

        #region Keyboard Shortcuts

        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.UpArrow:
                        if (_selectedEventIndex > 0)
                        {
                            _selectedEventIndex--;
                            Repaint();
                        }
                        e.Use();
                        break;

                    case KeyCode.DownArrow:
                        if (_selectedEventIndex < _filteredEventTypes.Count - 1)
                        {
                            _selectedEventIndex++;
                            Repaint();
                        }
                        e.Use();
                        break;

                    case KeyCode.S:
                        if (e.control || e.command)
                        {
                            SaveConfigAsset();
                            e.Use();
                        }
                        break;
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 创建纯色纹理用于自定义GUIStyle背景
        /// </summary>
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        #endregion
    }
}
