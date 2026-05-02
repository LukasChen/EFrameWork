using EFrame.Runtime.DataStorage;
using EFrame.Runtime.Event;
using EFrame.Runtime.Procedure;
using EFrame.Runtime.UI;
using System;
using UnityEngine;
using System.Collections;

namespace EFrame.Runtime
{
    [RequireComponent(typeof(EFrameProcedureComponent))]
    public class EFrameComponent : EFrameBehaviour
    {
        private bool m_initialized;

        [Header("Procedure")]
        [Tooltip("EFrame procedure component. Keep it on the startup Boot object with EFrameComponent.")]
        [SerializeField] private EFrameProcedureComponent m_procedureComponent;

        [Header("Settings")]
        [SerializeField] private EFrameSettings m_settings;

        [Header("Cameras")]
        [SerializeField] public Camera UICamera;
        [SerializeField] public Camera SceneCamera;

        [Header("Screen Fit")]
        [Tooltip("设计分辨率")]
        [SerializeField] public Vector2Int DesignSize = new(1080, 1920);
        [Tooltip("屏幕适配模式")]
        [SerializeField] public ScreenFitMode FitMode = ScreenFitMode.FitWidth;

        public EFrameSettings Settings => m_settings;
        public Vector2Int ResolvedDesignSize => m_settings != null ? m_settings.DesignSize : DesignSize;
        public ScreenFitMode ResolvedFitMode => m_settings != null ? m_settings.FitMode : FitMode;
        public bool ResolvedEnableScreenFitDebugLog => m_settings != null && m_settings.EnableScreenFitDebugLog;

        private TimeSpan m_accumulatedTime;
        private DateTime m_startTime;
        private float m_lastTimeTick = 0f;

        private void Awake()
        {
            if (!TryEnsureProcedureComponent())
            {
                Debug.LogError("EFrameComponent requires an EFrameProcedureComponent.");
                return;
            }

            StartCoroutine(FrameWorkInitialize());
        }

        private void Reset()
        {
            TryEnsureProcedureComponent();
        }

        private void OnValidate()
        {
            TryEnsureProcedureComponent();
        }

        private IEnumerator FrameWorkInitialize()
        {
            DontDestroyOnLoad(gameObject);
            //先暂停流程组件，等初始化完成后再启用
            m_procedureComponent.enabled = false;

            m_initialized = false;

            yield return null;
            // 初始化EFrame框架
            yield return EFrame.Initialize(this);
            if (!EFrame.LastInitializationResult.Succeeded)
            {
                Debug.LogError($"EFrame initialization failed: {EFrame.LastInitializationResult.Message}");
                yield break;
            }

            // 启动流程组件
            m_procedureComponent.Initialize(EFrame.Current);
            m_procedureComponent.StartProcedure();
            float startupDeadline = Time.realtimeSinceStartup + 5f;
            while (!m_procedureComponent.IsRunning && Time.realtimeSinceStartup < startupDeadline)
            {
                yield return null;
            }

            if (!m_procedureComponent.IsRunning)
            {
                Debug.LogError("EFrame procedure startup failed.");
                yield break;
            }

            m_initialized = true;
            m_procedureComponent.enabled = true;
            m_startTime = DateTime.Now;
            m_accumulatedTime = TimeSpan.Zero;
            EventBus.Dispatch(new AppStartedEvent());
            yield return null;
        }

        private void Update()
        {
            if (!m_initialized) return;
            EFrame.Update(Time.deltaTime, Time.unscaledDeltaTime);
            m_procedureComponent.Tick(Time.deltaTime, Time.unscaledDeltaTime);
            EventBus.Dispatch(new AppUpdateEvent { DeltaTime = Time.deltaTime, UnscaledDeltaTime = Time.unscaledDeltaTime });

            // 每秒触发一次事件            
            if (Time.time - m_lastTimeTick >= 1f)
            {
                m_lastTimeTick = Time.time;
                EventBus.Dispatch(new AppUpdatePerSecondEvent());
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!m_initialized)
            {
                return;
            }

            if (pauseStatus)
            {
                m_accumulatedTime += DateTime.Now - m_startTime;
                SaveTotalPlayTime((float)m_accumulatedTime.TotalSeconds);
                EventBus.Dispatch(new AppPausedEvent { Duration = (int)m_accumulatedTime.TotalSeconds });
            }
            else
            {
                m_startTime = DateTime.Now;
                m_accumulatedTime = TimeSpan.Zero;
                EventBus.Dispatch(new AppResumedEvent());
            }
        }

        private void OnApplicationQuit()
        {
            if (m_initialized)
            {
                m_accumulatedTime += DateTime.Now - m_startTime;
                SaveTotalPlayTime((float)m_accumulatedTime.TotalSeconds);
                EventBus.Dispatch(new AppQuitEvent());
            }

            m_procedureComponent?.Shutdown();
            EFrame.Dispose();
        }

        private void SaveTotalPlayTime(float totalSeconds)
        {
            Debug.Log($"本次总游戏时长: {totalSeconds} 秒");
            if (Context?.Data == null)
            {
                Debug.LogWarning("SaveTotalPlayTime: DataManager is null, skip saving.");
                return;
            }
            DefaultFrameData data = Context.Data.GetTable<DefaultFrameData>();
            if (data == null || !data.IsLoaded)
            {
                Debug.LogWarning("SaveTotalPlayTime: DefaultFrameData is not loaded, skip saving.");
                return;
            }

            data.TotalPlayTime += totalSeconds;
            data.CurPlayTime = totalSeconds;
        }

        private bool TryEnsureProcedureComponent()
        {
            if (m_procedureComponent != null)
            {
                return true;
            }

            m_procedureComponent = GetComponent<EFrameProcedureComponent>();
            return m_procedureComponent != null;
        }
    }
}
