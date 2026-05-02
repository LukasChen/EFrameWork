using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using EFramework.Runtime.Event;
using EFramework.Runtime.Vibration;
using Lofelt.NiceVibrations;
using UnityEngine;

namespace EFramework.Runtime.Audio
{
    /// <summary>
    /// 音效事件管理器
    /// 监听所有 IEvent 事件，根据配置自动播放音效和震动
    /// </summary>
    public class AudioEventManager : IAudioEventService
    {
        #region Constants

        private const string k_configResourcePath = AudioResourcePaths.EventConfigResourcePath;

        #endregion

        #region Fields

        private AudioEventConfigAsset m_configAsset;
        private Dictionary<string, AudioEventConfigItem> m_configDict;
        private Dictionary<string, float> m_lastPlayTimeByEvent;
        private Dictionary<string, int> m_sequentialIndexByEvent;
        private Dictionary<string, FieldPathAccessor> m_fieldAccessorCache;

        private IAudioService m_audioManager;
        private QVibration m_vibration;
        private CancellationTokenSource m_delayCancellationTokenSource;

        private bool m_isInitialized;

        #endregion

        #region Properties

        public bool IsInitialized => m_isInitialized;

        #endregion

        #region Initialization

        public AudioEventManager(EFrameComponent component, IAudioService audioManager, QVibration vibration)
        {
            m_audioManager = audioManager;
            m_vibration = vibration;
            m_configDict = new Dictionary<string, AudioEventConfigItem>();
            m_lastPlayTimeByEvent = new Dictionary<string, float>();
            m_sequentialIndexByEvent = new Dictionary<string, int>();
            m_fieldAccessorCache = new Dictionary<string, FieldPathAccessor>();
            m_delayCancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 异步初始化，加载配置资源
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (m_isInitialized)
            {
                Debug.LogWarning("[AudioEventManager] Already initialized");
                return;
            }

            try
            {
                EnsureDelayCancellationTokenSource();
                await UniTask.CompletedTask;
                m_configAsset = LoadConfigAsset();

                if (m_configAsset != null)
                {
                    BuildConfigDictionary();
                    SubscribeToEvents();
                    m_isInitialized = true;
                    Debug.Log($"[AudioEventManager] Initialized with {m_configDict.Count} event configs");
                }
                else
                {
                    Debug.LogWarning("[AudioEventManager] Config asset not found, running without audio event configs");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioEventManager] Failed to load config: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步初始化 (备用方案，使用 Resources 加载)
        /// </summary>
        public void Initialize()
        {
            if (m_isInitialized)
            {
                Debug.LogWarning("[AudioEventManager] Already initialized");
                return;
            }

            // 尝试同步加载
            EnsureDelayCancellationTokenSource();
            m_configAsset = LoadConfigAsset();

            if (m_configAsset != null)
            {
                BuildConfigDictionary();
                SubscribeToEvents();
                m_isInitialized = true;
                Debug.Log($"[AudioEventManager] Initialized with {m_configDict.Count} event configs");
            }
            else
            {
                Debug.LogWarning("[AudioEventManager] Config asset not found");
            }
        }

        private void BuildConfigDictionary()
        {
            m_configDict.Clear();
            m_fieldAccessorCache.Clear();
            if (m_configAsset == null) return;

            foreach (var item in m_configAsset.ConfigItems)
            {
                if (!string.IsNullOrEmpty(item.EventTypeName) && item.Enabled)
                {
                    m_configDict[item.EventTypeName] = item;
                }
            }
        }

        private void SubscribeToEvents()
        {
            // 使用 EventBus 的全局钩子监听所有事件
            EventBus.OnAnyEvent += OnAnyEventDispatched;
        }

        public void Dispose()
        {
            EventBus.OnAnyEvent -= OnAnyEventDispatched;
            m_delayCancellationTokenSource?.Cancel();
            m_delayCancellationTokenSource?.Dispose();
            m_delayCancellationTokenSource = null;
            m_configDict.Clear();
            m_lastPlayTimeByEvent.Clear();
            m_sequentialIndexByEvent.Clear();
            m_fieldAccessorCache.Clear();
            m_isInitialized = false;
        }

        #endregion

        #region Event Handling

        private void EnsureDelayCancellationTokenSource()
        {
            if (m_delayCancellationTokenSource == null || m_delayCancellationTokenSource.IsCancellationRequested)
            {
                m_delayCancellationTokenSource?.Dispose();
                m_delayCancellationTokenSource = new CancellationTokenSource();
            }
        }

        private void OnAnyEventDispatched(Type eventType, object eventData)
        {
            if (!m_isInitialized) return;

            string eventTypeName = eventType.FullName;
            if (string.IsNullOrEmpty(eventTypeName)) return;

            if (m_configDict.TryGetValue(eventTypeName, out var configItem))
            {
                ProcessEventAudio(configItem, eventData);
            }
        }

        private void ProcessEventAudio(AudioEventConfigItem configItem, object eventData)
        {
            if (!configItem.Enabled)
                return;

            string eventKey = configItem.EventTypeName;

            // 处理所有条件音效组，所有满足条件的组都会播放
            foreach (var group in configItem.ConditionalGroups)
            {
                if (!group.Enabled || group.AudioClips.Count == 0)
                    continue;

                // 检查此组的重复播放间隔
                string groupKey = eventKey + "_" + group.GroupName;
                if (m_lastPlayTimeByEvent.TryGetValue(groupKey, out float lastTime))
                {
                    if (Time.time - lastTime < group.MinInterval)
                        continue;
                }

                // 评估条件 (无条件 = 始终满足)
                if (EvaluateConditions(group, eventData))
                {
                    // 记录此组的播放时间
                    m_lastPlayTimeByEvent[groupKey] = Time.time;

                    // 条件满足，播放此组的音效
                    var clipEntry = SelectAudioClipFromGroup(group, groupKey);
                    if (clipEntry?.Clip != null)
                    {
                        if (group.Delay > 0)
                        {
                            PlayWithDelayAsync(group, clipEntry, m_delayCancellationTokenSource.Token).Forget();
                        }
                        else
                        {
                            PlayAudioClip(group, clipEntry, group.MinInterval);
                            ProcessVibrationFromGroup(group);
                        }
                    }
                }
            }
        }

        #region Condition Evaluation

        private bool EvaluateConditions(ConditionalAudioGroup group, object eventData)
        {
            if (group.Conditions.Count == 0)
                return true; // 无条件则默认满足

            bool result = group.Logic == ConditionLogic.And;

            foreach (var condition in group.Conditions)
            {
                bool conditionMet = EvaluateSingleCondition(condition, eventData);

                if (group.Logic == ConditionLogic.And)
                {
                    result = result && conditionMet;
                    if (!result) break; // 短路评估
                }
                else // Or
                {
                    result = result || conditionMet;
                    if (result) break; // 短路评估
                }
            }

            return result;
        }

        private bool EvaluateSingleCondition(AudioEventCondition condition, object eventData)
        {
            if (string.IsNullOrEmpty(condition.FieldPath) && condition.Operator != ConditionOperator.IsNotNull && condition.Operator != ConditionOperator.IsNull)
                return true;

            try
            {
                // 获取字段值
                object fieldValue = GetFieldValue(eventData, condition.FieldPath);

                switch (condition.Operator)
                {
                    case ConditionOperator.IsNull:
                        return fieldValue == null;

                    case ConditionOperator.IsNotNull:
                        return fieldValue != null;

                    case ConditionOperator.IsType:
                        if (fieldValue == null) return false;
                        return IsTypeMatch(fieldValue, condition.TypeName);

                    case ConditionOperator.Equals:
                        return CompareValues(fieldValue, condition.CompareValue) == 0;

                    case ConditionOperator.NotEquals:
                        return CompareValues(fieldValue, condition.CompareValue) != 0;

                    case ConditionOperator.GreaterThan:
                        return CompareValues(fieldValue, condition.CompareValue) > 0;

                    case ConditionOperator.GreaterThanOrEquals:
                        return CompareValues(fieldValue, condition.CompareValue) >= 0;

                    case ConditionOperator.LessThan:
                        return CompareValues(fieldValue, condition.CompareValue) < 0;

                    case ConditionOperator.LessThanOrEquals:
                        return CompareValues(fieldValue, condition.CompareValue) <= 0;

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AudioEventManager] Condition evaluation failed: {ex.Message}");
                return false;
            }
        }

        private object GetFieldValue(object obj, string fieldPath)
        {
            if (obj == null || string.IsNullOrEmpty(fieldPath))
                return obj;

            Type rootType = obj.GetType();
            string cacheKey = $"{rootType.FullName}|{fieldPath}";
            if (!m_fieldAccessorCache.TryGetValue(cacheKey, out var accessor))
            {
                accessor = BuildFieldPathAccessor(rootType, fieldPath);
                m_fieldAccessorCache[cacheKey] = accessor;
            }

            return accessor.GetValue(obj);
        }

        private FieldPathAccessor BuildFieldPathAccessor(Type rootType, string fieldPath)
        {
            string[] parts = fieldPath.Split('.');
            Type currentType = rootType;
            var members = new List<MemberInfo>(parts.Length);

            foreach (string part in parts)
            {
                var field = currentType.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    members.Add(field);
                    currentType = field.FieldType;
                    continue;
                }

                var property = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    members.Add(property);
                    currentType = property.PropertyType;
                    continue;
                }

                Debug.LogWarning($"[AudioEventManager] Field/Property '{part}' not found on type '{currentType.Name}'");
                return FieldPathAccessor.Invalid;
            }

            return new FieldPathAccessor(members);
        }

        private sealed class FieldPathAccessor
        {
            public static readonly FieldPathAccessor Invalid = new FieldPathAccessor(null);

            private readonly List<MemberInfo> m_members;

            public FieldPathAccessor(List<MemberInfo> members)
            {
                m_members = members;
            }

            public object GetValue(object root)
            {
                if (root == null || m_members == null) return null;

                object current = root;
                foreach (var member in m_members)
                {
                    if (current == null) return null;

                    if (member is FieldInfo field)
                    {
                        current = field.GetValue(current);
                    }
                    else if (member is PropertyInfo property)
                    {
                        current = property.GetValue(current);
                    }
                }

                return current;
            }
        }

        private bool IsTypeMatch(object value, string typeName)
        {
            if (value == null || string.IsNullOrEmpty(typeName))
                return false;

            Type valueType = value.GetType();

            // 直接类型名匹配
            if (valueType.Name == typeName || valueType.FullName == typeName)
                return true;

            // 检查基类和接口
            Type baseType = valueType.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == typeName || baseType.FullName == typeName)
                    return true;
                baseType = baseType.BaseType;
            }

            foreach (var iface in valueType.GetInterfaces())
            {
                if (iface.Name == typeName || iface.FullName == typeName)
                    return true;
            }

            return false;
        }

        private int CompareValues(object fieldValue, string compareValue)
        {
            if (fieldValue == null)
                return string.IsNullOrEmpty(compareValue) ? 0 : -1;

            // 数值比较
            if (fieldValue is int intVal)
            {
                if (int.TryParse(compareValue, out int compareInt))
                    return intVal.CompareTo(compareInt);
            }
            else if (fieldValue is float floatVal)
            {
                if (float.TryParse(compareValue, out float compareFloat))
                    return floatVal.CompareTo(compareFloat);
            }
            else if (fieldValue is double doubleVal)
            {
                if (double.TryParse(compareValue, out double compareDouble))
                    return doubleVal.CompareTo(compareDouble);
            }
            else if (fieldValue is bool boolVal)
            {
                if (bool.TryParse(compareValue, out bool compareBool))
                    return boolVal.CompareTo(compareBool);
            }
            else if (fieldValue is Enum enumVal)
            {
                // 枚举比较：尝试按名称或值
                string enumString = enumVal.ToString();
                if (enumString.Equals(compareValue, StringComparison.OrdinalIgnoreCase))
                    return 0;
                if (int.TryParse(compareValue, out int enumInt))
                    return Convert.ToInt32(enumVal).CompareTo(enumInt);
                return string.Compare(enumString, compareValue, StringComparison.OrdinalIgnoreCase);
            }

            // 字符串比较
            return string.Compare(fieldValue.ToString(), compareValue, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        private AudioClipEntry SelectAudioClipFromGroup(ConditionalAudioGroup group, string groupKey)
        {
            if (group.AudioClips.Count == 0) return null;
            if (group.AudioClips.Count == 1) return group.AudioClips[0];

            int clipIndex = 0;

            switch (group.PlayMode)
            {
                case AudioPlayMode.Random:
                    clipIndex = UnityEngine.Random.Range(0, group.AudioClips.Count);
                    break;

                case AudioPlayMode.Sequential:
                    if (!m_sequentialIndexByEvent.TryGetValue(groupKey, out clipIndex))
                    {
                        clipIndex = 0;
                    }
                    m_sequentialIndexByEvent[groupKey] = (clipIndex + 1) % group.AudioClips.Count;
                    break;

                case AudioPlayMode.WeightedRandom:
                    clipIndex = GetWeightedClipIndexFromList(group.AudioClips);
                    break;
            }

            return group.AudioClips[clipIndex];
        }

        private int GetWeightedClipIndexFromList(List<AudioClipEntry> clips)
        {
            if (clips.Count == 0) return 0;

            float totalWeight = 0f;
            foreach (var clip in clips)
            {
                totalWeight += clip.Weight;
            }

            if (totalWeight <= 0f)
                return UnityEngine.Random.Range(0, clips.Count);

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < clips.Count; i++)
            {
                cumulative += clips[i].Weight;
                if (random <= cumulative)
                    return i;
            }

            return clips.Count - 1;
        }

        private void ProcessVibrationFromGroup(ConditionalAudioGroup group)
        {
            if (m_vibration == null || !m_vibration.IsOn) return;

            switch (group.VibrationMode)
            {
                case VibrationMode.Preset:
                    m_vibration.PlayPreset(group.VibrationPreset);
                    break;

                case VibrationMode.Custom:
                    m_vibration.PlayEmphasis(group.VibrationAmplitude, group.VibrationFrequency);
                    break;
            }
        }

        private async UniTask PlayWithDelayAsync(ConditionalAudioGroup group, AudioClipEntry clipEntry, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(group.Delay), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested || !m_isInitialized)
                return;

            PlayAudioClip(group, clipEntry, group.MinInterval);
            ProcessVibrationFromGroup(group);
        }

        private void PlayAudioClip(ConditionalAudioGroup group, AudioClipEntry clipEntry, float minInterval)
        {
            try
            {
                var audioClip = clipEntry.Clip;
                if (audioClip == null) return;

                // 获取随机音量和音调
                float volume = group.Volume.RandomValue;
                float pitch = group.Pitch.RandomValue;

                // 根据音轨类型播放
                if (group.TrackType == AudioTrackType.Music)
                {
                    m_audioManager.PlayMusic(audioClip, group.Loop, group.FadeInDuration > 0, group.FadeInDuration);
                }
                else
                {
                    // SFX 播放，支持淡入淡出
                    if (group.FadeInDuration > 0 || group.FadeOutDuration > 0)
                    {
                        m_audioManager.PlaySfxWithFade(
                            audioClip,
                            volume,
                            pitch,
                            group.FadeInDuration,
                            group.FadeInCurve,
                            group.FadeOutDuration,
                            group.FadeOutCurve,
                            group.Loop);
                    }
                    else
                    {
                        m_audioManager.PlaySfx(audioClip, minInterval, volume, pitch);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioEventManager] Failed to play audio: {ex.Message}");
            }
        }


        #endregion

        #region Runtime API

        /// <summary>
        /// 手动触发事件音效（用于不通过 EventBus 的场景）
        /// </summary>
        public void TriggerEventAudio(string eventTypeName)
        {
            if (m_configDict.TryGetValue(eventTypeName, out var configItem))
            {
                ProcessEventAudio(configItem, null);
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public async UniTask ReloadConfigAsync()
        {
            await UniTask.CompletedTask;

            m_configAsset = LoadConfigAsset();
            if (m_configAsset != null)
            {
                BuildConfigDictionary();
                Debug.Log($"[AudioEventManager] Reloaded config with {m_configDict.Count} items");
            }
        }

        /// <summary>
        /// 检查事件是否有音效配置
        /// </summary>
        public bool HasAudioConfig(string eventTypeName)
        {
            return m_configDict.ContainsKey(eventTypeName);
        }

        /// <summary>
        /// 获取事件配置
        /// </summary>
        public AudioEventConfigItem GetConfig(string eventTypeName)
        {
            return m_configDict.TryGetValue(eventTypeName, out var config) ? config : null;
        }

        private static AudioEventConfigAsset LoadConfigAsset()
        {
            return Resources.Load<AudioEventConfigAsset>(k_configResourcePath);
        }

        #endregion
    }
}
