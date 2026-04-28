using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EFrameWork.Runtime.Audio;
using EFrameWork.Runtime.DataStorage;
using EFrameWork.Runtime.UI;
using GameFramework;
using UnityEngine;
using EFrameWork.Runtime.Vibration;
using EFramework.Utils;
using EFrameWork.Runtime.Asset;
using EFrameWork.Runtime.Utils;
using Debug = UnityEngine.Debug;
using EFrameWork.Runtime.GMTools;
using System.Collections;

namespace EFrameWork.Runtime
{
    /// <summary>
    /// EFrame 核心框架类
    /// 作为整个游戏框架的入口点，负责管理和协调各个子系统的初始化、更新和销毁。
    /// 采用单例模式设计，通过静态属性提供对各个管理器的全局访问。
    /// </summary>
    public sealed class EFrame
    {
        #region 私有字段
        private static EFrameComponent m_frameComponent;

        /// <summary>
        /// 帧计数器，用于计算平均 FPS
        /// </summary>
        private static int m_frameCount = 0;

        /// <summary>
        /// 累计经过的时间（秒），用于 FPS 计算
        /// </summary>
        private static float m_timePassed = 0f;

        /// <summary>
        /// 计算得出的平均帧率
        /// </summary>
        private static float m_averageFPS = 0f;

        #endregion

        #region 公开属性 - 子系统管理器

        /// <summary>
        /// 框架是否已完成初始化
        /// 在调用任何框架功能前，应先检查此属性确保框架已就绪
        /// </summary>
        public static bool Initialized { get; private set; }


        /// <summary>
        /// 数据管理器
        /// 负责游戏数据的持久化存储和读取，支持多种存储格式
        /// </summary>
        public static DataManager DataManager { get; private set; }

        /// <summary>
        /// UI 管理器
        /// 负责 UI 界面的加载、显示、隐藏和层级管理
        /// </summary>
        public static QUI UI { get; private set; }

        /// <summary>
        /// 音频管理器
        /// 负责背景音乐和音效的播放控制
        /// </summary>
        public static AudioManager Audio { get; private set; }

        /// <summary>
        /// 音效事件管理器
        /// 负责基于游戏事件自动触发音效和震动反馈
        /// </summary>
        public static AudioEventManager AudioEvent { get; private set; }

        /// <summary>
        /// 协程管理器
        /// 提供统一的协程调度服务，支持延迟调用和下一帧回调
        /// </summary>
        public static CoroutineManager Coroutine { get; private set; }

        /// <summary>
        /// 震动管理器
        /// 负责手机震动反馈的触发和控制
        /// </summary>
        public static QVibration Vibration { get; private set; }

        #endregion

        #region 公开属性 - 场景相关

        /// <summary>
        /// 场景相机引用
        /// </summary>
        public static Camera SceneCamera => m_frameComponent != null ? m_frameComponent.SceneCamera : null;

        /// <summary>
        /// UI 相机引用
        /// </summary>
        public static Camera UICamera => m_frameComponent != null ? m_frameComponent.UICamera : null;

        #endregion

        #region 公开属性 - 性能监控

        /// <summary>
        /// FPS 采样时长（秒）
        /// 每隔此时间间隔计算一次平均帧率
        /// </summary>
        public static float SampleDuration = 1f;

        public static bool IsLtsDev
        {
            get
            {
                var ltsDev = false;
#if LTS_DEV
                ltsDev = true;
#endif
                return ltsDev;
            }
        }

        public static IEnumerator Initialize(EFrameComponent component)
        {
            m_frameComponent = component;
            Debug.Log("EFrame 开始初始化...");

            // 等待一帧
            yield return null;

            // 初始化协程管理器
            Coroutine = new CoroutineManager(m_frameComponent);

            // 初始化 Addressables 系统（等待完成）
            yield return AssetManager.InitializeCoroutine();
            yield return null;

            // 初始化游戏时间服务
            GameTimeService.Init();

            // 初始化数据管理器
            DataManager = new DataManager(new JsonFileStorage());
            DataManager.RegisterTable<DefaultFrameData>();
            DefaultFrameData defaultFrameData = DataManager.GetTable<DefaultFrameData>();

            // 初始化音频管理器和音效事件管理器
            Audio = new AudioManager(m_frameComponent);
            Audio.Initialize(defaultFrameData.MusicOn, defaultFrameData.SoundOn);
            AudioEvent = new AudioEventManager(m_frameComponent, Audio, Vibration);
            AudioEvent.Initialize();

            // 初始化震动管理器
            Vibration = new QVibration(defaultFrameData.VibrationOn);

            // 初始化 UI 管理器
            UI = new QUI();
            UI.Init(m_frameComponent.UICamera, component.DesignSize.x, component.DesignSize.y, component.FitMode);

            // 初始化 DOTween 动画库
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(500, 50);

#if UNITY_EDITOR || LTS_DEV
            // 初始化 GM 工具
//            GMButton.Initialize();

#endif
            Initialized = true;
            Debug.Log("EFrame 初始化完成.");
            yield return null;
        }

        /// <summary>
        /// 框架每帧更新方法
        /// 应在 MonoBehaviour 的 Update 方法中调用
        /// </summary>
        public static void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (!Initialized) return;
            GameFrameworkEntry.Update(deltaTime, unscaledDeltaTime);
            m_frameCount++;
            m_timePassed += Time.unscaledDeltaTime;
            if (m_timePassed >= SampleDuration)
            {
                m_averageFPS = m_frameCount / m_timePassed;
                m_frameCount = 0;
                m_timePassed = 0f;
            }
        }

        /// <summary>
        /// 当前平均帧率（FPS）
        /// 基于 SampleDuration 时间段内的帧数计算
        /// </summary>
        public static float FPS
        {
            get
            {
                return m_averageFPS;
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 延迟调用指定方法
        /// </summary>
        /// <param name="delayInSeconds">延迟时间（秒），负值会被修正为 0</param>
        /// <param name="action">要执行的回调方法</param>
        /// <remarks>
        /// 使用协程实现延迟调用，适用于需要等待一定时间后执行的操作
        /// 如果框架未初始化，将输出错误日志
        /// </remarks>
        public static void DelayCall(float delayInSeconds, System.Action action)
        {
            if (Coroutine != null)
            {
                // 防止负数延迟
                if (delayInSeconds < 0f)
                {
                    delayInSeconds = 0f;
                }
                Coroutine.DelayCall(delayInSeconds, action);
            }
            else
            {
                Debug.LogError("EFrame - DelayCall - EFrame is not initialized. Cannot perform DelayCall.");
            }
        }

        #endregion

        #region 销毁方法

        /// <summary>
        /// 销毁框架并释放所有资源
        /// 应在游戏退出或需要重新初始化框架时调用
        /// </summary>
        /// <remarks>
        /// 销毁顺序与初始化顺序相反，确保依赖关系正确处理：
        /// 1. 关闭数据统计监听器
        /// 2. 关闭 GameFramework 核心
        /// 3. 释放 SDK 管理器
        /// 4. 释放音效事件管理器
        /// 5. 释放音频管理器
        /// 6. 释放数据管理器
        /// 7. 释放协程管理器
        /// </remarks>
        public static void Dispose()
        {

            // 关闭 GameFramework 核心框架
            GameFrameworkEntry.Shutdown();

            // 释放各管理器（使用空值传播运算符，防止空引用）
            AudioEvent?.Dispose();
            Audio?.Dispose();
            DataManager?.Dispose();
            Coroutine?.Dispose();
            AudioEvent = null;
            Audio = null;
            DataManager = null;
            Coroutine = null;
            Vibration = null;
            UI = null;
            m_frameComponent = null;
            Initialized = false;
        }

        #endregion
    }
}