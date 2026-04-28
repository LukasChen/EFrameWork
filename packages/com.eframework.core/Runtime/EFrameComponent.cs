using EFrameWork.Runtime.DataStorage;
using EFrameWork.Runtime.Event;
using EFrameWork.Runtime.UI;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;
using System.Collections;

namespace EFrameWork.Runtime
{
    [RequireComponent(typeof(ProcedureComponent))]
    public class EFrameComponent : MonoBehaviour
    {
        private bool m_initialized;

        [Header("Procedure")]
        [Tooltip("UGF ProcedureComponent。建议在启动场景里拖引用，或作为本对象的子节点存在（可禁用）。")]
        [SerializeField] private ProcedureComponent m_procedureComponent;

        [Header("Cameras")]
        [SerializeField] public Camera UICamera;
        [SerializeField] public Camera SceneCamera;

        [Header("Screen Fit")]
        [Tooltip("设计分辨率")]
        [SerializeField] public Vector2Int DesignSize = new(1080, 1920);
        [Tooltip("屏幕适配模式")]
        [SerializeField] public ScreenFitMode FitMode = ScreenFitMode.FitWidth;

        private TimeSpan m_accumulatedTime;
        private DateTime m_startTime;
        private float m_lastTimeTick = 0f;

        private void Awake()
        {
            if (m_procedureComponent == null)
            {
                m_procedureComponent = GetComponent<ProcedureComponent>();
                if (m_procedureComponent == null)
                {
                    Debug.LogError("EFrameComponent requires a ProcedureComponent.");
                    return;
                }
            }

            StartCoroutine(FrameWorkInitialize());
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

            // 启动流程组件
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
            m_accumulatedTime += DateTime.Now - m_startTime;
            SaveTotalPlayTime((float)m_accumulatedTime.TotalSeconds);
            EventBus.Dispatch(new AppQuitEvent());
            EFrame.Dispose();
        }

        private void SaveTotalPlayTime(float totalSeconds)
        {
            Debug.Log($"本次总游戏时长: {totalSeconds} 秒");
            if (EFrame.DataManager == null)
            {
                Debug.LogWarning("SaveTotalPlayTime: DataManager is null, skip saving.");
                return;
            }
            DefaultFrameData data = EFrame.DataManager.GetTable<DefaultFrameData>();
            if (data == null || data.Data == null)
            {
                Debug.LogWarning("SaveTotalPlayTime: DefaultFrameData or Data is null, skip saving.");
                return;
            }

            data.TotalPlayTime += totalSeconds;
            data.CurPlayTime = totalSeconds;
        }
    }
}
