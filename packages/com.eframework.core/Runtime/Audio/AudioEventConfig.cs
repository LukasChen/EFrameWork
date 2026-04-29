using System;
using System.Collections.Generic;
using EFrameWork.Runtime.Base.Attributes;
using Lofelt.NiceVibrations;
using UnityEngine;
namespace EFrameWork.Runtime.Audio
{
    /// <summary>
    /// 多音频播放规则
    /// </summary>
    public enum AudioPlayMode
    {
        /// <summary>
        /// 随机播放
        /// </summary>
        Random,

        /// <summary>
        /// 顺序循环播放
        /// </summary>
        Sequential,

        /// <summary>
        /// 加权随机播放
        /// </summary>
        WeightedRandom
    }

    /// <summary>
    /// 音轨类型
    /// </summary>
    public enum AudioTrackType
    {
        SFX,
        Music
    }

    /// <summary>
    /// 震动模式
    /// </summary>
    public enum VibrationMode
    {
        /// <summary>
        /// 不震动
        /// </summary>
        None,

        /// <summary>
        /// 使用预设类型
        /// </summary>
        Preset,

        /// <summary>
        /// 自定义强度
        /// </summary>
        Custom
    }

    #region Condition System

    /// <summary>
    /// 条件比较运算符
    /// </summary>
    public enum ConditionOperator
    {
        /// <summary>
        /// 等于
        /// </summary>
        Equals,

        /// <summary>
        /// 不等于
        /// </summary>
        NotEquals,

        /// <summary>
        /// 大于
        /// </summary>
        GreaterThan,

        /// <summary>
        /// 大于等于
        /// </summary>
        GreaterThanOrEquals,

        /// <summary>
        /// 小于
        /// </summary>
        LessThan,

        /// <summary>
        /// 小于等于
        /// </summary>
        LessThanOrEquals,

        /// <summary>
        /// 类型检查 (is)
        /// </summary>
        IsType,

        /// <summary>
        /// 不为空
        /// </summary>
        IsNotNull,

        /// <summary>
        /// 为空
        /// </summary>
        IsNull
    }

    /// <summary>
    /// 条件逻辑运算符 (多条件组合)
    /// </summary>
    public enum ConditionLogic
    {
        /// <summary>
        /// 所有条件都满足
        /// </summary>
        And,

        /// <summary>
        /// 任意条件满足
        /// </summary>
        Or
    }

    /// <summary>
    /// 单个条件配置
    /// </summary>
    [Serializable]
    public class AudioEventCondition
    {
        /// <summary>
        /// 事件字段名 (支持嵌套，如 "TargetPile.Cards.Count")
        /// </summary>
        public string FieldPath = "";

        /// <summary>
        /// 比较运算符
        /// </summary>
        public ConditionOperator Operator = ConditionOperator.Equals;

        /// <summary>
        /// 比较值 (字符串形式，运行时转换)
        /// </summary>
        public string CompareValue = "";

        /// <summary>
        /// 类型名 (用于 IsType 运算符)
        /// </summary>
        public string TypeName = "";
    }

    /// <summary>
    /// 条件音效组 - 包含条件和对应的完整音效配置
    /// </summary>
    [Serializable]
    public class ConditionalAudioGroup
    {
        #region 基本信息

        /// <summary>
        /// 组名称 (用于编辑器显示)
        /// </summary>
        public string GroupName = "条件组";

        /// <summary>
        /// 是否启用此条件组
        /// </summary>
        public bool Enabled = true;

        #endregion

        #region 条件配置

        /// <summary>
        /// 条件列表 (为空 = 无条件 = 始终播放)
        /// </summary>
        public List<AudioEventCondition> Conditions = new List<AudioEventCondition>();

        /// <summary>
        /// 多条件逻辑 (And/Or)
        /// </summary>
        public ConditionLogic Logic = ConditionLogic.And;

        #endregion

        #region 音频配置

        /// <summary>
        /// 音频剪辑列表
        /// </summary>
        public List<AudioClipEntry> AudioClips = new List<AudioClipEntry>();

        /// <summary>
        /// 多音频播放模式
        /// </summary>
        public AudioPlayMode PlayMode = AudioPlayMode.Random;

        #endregion

        #region 播放参数

        /// <summary>
        /// 音轨类型
        /// </summary>
        public AudioTrackType TrackType = AudioTrackType.SFX;

        /// <summary>
        /// 音量范围
        /// </summary>
        [MinMaxRange(0f, 1f)]
        public RangeFloat Volume = new RangeFloat(1f, 1f);

        /// <summary>
        /// 音调范围
        /// </summary>
        [MinMaxRange(0.5f, 1.5f)]
        public RangeFloat Pitch = new RangeFloat(1f, 1f);

        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool Loop = false;

        /// <summary>
        /// 重复播放的最小间隔 (秒)
        /// </summary>
        [Range(0.03f, 2f)]
        public float MinInterval = 0.05f;

        /// <summary>
        /// 延时播放 (秒)
        /// </summary>
        [Range(0f, 10f)]
        public float Delay = 0f;

        #endregion

        #region 淡入淡出

        /// <summary>
        /// 淡入时长 (秒)
        /// </summary>
        [Range(0f, 5f)]
        public float FadeInDuration = 0f;

        /// <summary>
        /// 淡入曲线
        /// </summary>
        public AnimationCurve FadeInCurve = AnimationCurve.Linear(0, 0, 1, 1);

        /// <summary>
        /// 淡出时长 (秒)
        /// </summary>
        [Range(0f, 5f)]
        public float FadeOutDuration = 0f;

        /// <summary>
        /// 淡出曲线
        /// </summary>
        public AnimationCurve FadeOutCurve = AnimationCurve.Linear(0, 1, 1, 0);

        #endregion

        #region 震动配置

        /// <summary>
        /// 震动模式
        /// </summary>
        public VibrationMode VibrationMode = VibrationMode.None;

        /// <summary>
        /// 预设震动类型
        /// </summary>
        public HapticPatterns.PresetType VibrationPreset = HapticPatterns.PresetType.Selection;

        /// <summary>
        /// 自定义震动强度
        /// </summary>
        [Range(0f, 1f)]
        public float VibrationAmplitude = 0.5f;

        /// <summary>
        /// 自定义震动频率
        /// </summary>
        [Range(0f, 1f)]
        public float VibrationFrequency = 0.5f;

        #endregion
    }

    #endregion

    /// <summary>
    /// 音频剪辑引用项 (支持权重)
    /// </summary>
    [Serializable]
    public class AudioClipEntry
    {
        public AudioClip Clip;

        /// <summary>
        /// 权重 (用于 WeightedRandom 模式)
        /// </summary>
        [Range(0f, 1f)]
        public float Weight = 1f;
    }

    /// <summary>
    /// 事件音效配置项
    /// </summary>
    [Serializable]
    public class AudioEventConfigItem
    {
        /// <summary>
        /// 事件类型全名 (命名空间.类型名)
        /// </summary>
        public string EventTypeName;

        /// <summary>
        /// 是否启用此配置
        /// </summary>
        public bool Enabled = true;

        /// <summary>
        /// 音效组列表 (所有满足条件的组都会播放)
        /// 无条件的组 = 默认音效，始终播放
        /// </summary>
        public List<ConditionalAudioGroup> ConditionalGroups = new List<ConditionalAudioGroup>();
    }
}
