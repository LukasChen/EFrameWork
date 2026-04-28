using EFrameWork.Runtime;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EFramework.Utils
{
    public class DynamicRenderScale : MonoBehaviour
    {
        [Header("Performance Settings")]
        public float TargetFPS = 60f;
        public float CheckInterval = 2f;
        public float MinScale = 0.5f;
        public float MaxScale = 1.0f;
        public float ScaleStep = 0.05f;

        [Header("Frame Time Analysis")]
        public float FrameTimeThreshold = 0.014f; // ~72fps frame time (1/72)
        public int StableFrameCount = 10; // 需要连续稳定的帧数
        public float MaxFrameTimeVariance = 0.002f; // 帧时间方差阈值

        [Header("Adaptive Behavior")]
        public bool EnableAdaptiveAdjustment = true;
        public float AggressiveScaleStep = 0.1f; // 性能严重不足时的大步调整
        public float ConservativeScaleStep = 0.02f; // 保守调整步长
        public int ConsecutiveAdjustmentLimit = 3; // 连续调整次数限制

        [Header("Debug Info")]
        public bool ShowDebugInfo = true;

        private UniversalRenderPipelineAsset m_urpAsset;
        private float[] m_recentFrameTimes;
        private int m_frameTimeIndex = 0;
        private int m_stablePerformanceCounter = 0;
        private int m_consecutiveDownAdjustments = 0;
        private int m_consecutiveUpAdjustments = 0;
        private float m_lastAdjustmentTime = 0f;
        private float m_currentAverageFrameTime = 0f;

        void Start()
        {
            m_urpAsset = (UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (m_urpAsset == null)
            {
                Debug.LogError("DynamicRenderScale: URP asset not found!");
                enabled = false;
                return;
            }

            // 初始化帧时间记录数组
            m_recentFrameTimes = new float[StableFrameCount];
            for (int i = 0; i < StableFrameCount; i++)
            {
                m_recentFrameTimes[i] = 1f / TargetFPS; // 初始化为目标帧时间
            }

            InvokeRepeating("AdjustRenderScale", 5f, CheckInterval);

            if (ShowDebugInfo)
            {
                Debug.Log($"DynamicRenderScale initialized. Target FPS: {TargetFPS}, Initial Scale: {m_urpAsset.renderScale:F2}");
            }
        }

        void Update()
        {
            // 实时记录帧时间用于性能分析
            RecordFrameTime(Time.unscaledDeltaTime);
        }

        private void RecordFrameTime(float frameTime)
        {
            m_recentFrameTimes[m_frameTimeIndex] = frameTime;
            m_frameTimeIndex = (m_frameTimeIndex + 1) % StableFrameCount;

            // 计算平均帧时间
            float sum = 0f;
            for (int i = 0; i < StableFrameCount; i++)
            {
                sum += m_recentFrameTimes[i];
            }
            m_currentAverageFrameTime = sum / StableFrameCount;
        }

        public void SetRenderScale(float scale)
        {
            if (m_urpAsset != null)
            {
                m_urpAsset.renderScale = Mathf.Clamp(scale, MinScale, MaxScale);
                if (ShowDebugInfo)
                {
                    Debug.Log($"Render scale manually set to: {m_urpAsset.renderScale:F2}");
                }
            }
        }

        private void AdjustRenderScale()
        {
            if (m_urpAsset == null) return;

            float currentFPS = EFrame.FPS;
            float currentScale = m_urpAsset.renderScale;
            float currentTime = Time.time;

            // 防止过于频繁的调整
            if (currentTime - m_lastAdjustmentTime < CheckInterval * 0.5f)
                return;

            PerformanceStatus status = AnalyzePerformance();

            if (ShowDebugInfo)
            {
                Debug.Log($"Performance Status: {status}, FPS: {currentFPS:F1}, Avg FrameTime: {m_currentAverageFrameTime * 1000:F2}ms, Scale: {currentScale:F2}");
            }

            bool adjusted = false;

            switch (status)
            {
                case PerformanceStatus.PoorPerformance:
                    adjusted = HandlePoorPerformance(currentScale);
                    break;

                case PerformanceStatus.ExcellentPerformance:
                    adjusted = HandleExcellentPerformance(currentScale);
                    break;

                case PerformanceStatus.GoodPerformance:
                    adjusted = HandleGoodPerformance(currentScale);
                    break;

                case PerformanceStatus.Unstable:
                    // 性能不稳定时暂时不调整
                    ResetConsecutiveCounters();
                    break;
            }

            if (adjusted)
            {
                m_lastAdjustmentTime = currentTime;
            }
        }

        private PerformanceStatus AnalyzePerformance()
        {
            float currentFPS = EFrame.FPS;
            float targetFrameTime = 1f / TargetFPS;

            // 检查性能稳定性
            if (!IsPerformanceStable())
            {
                return PerformanceStatus.Unstable;
            }

            // 基于帧时间的更准确判断（解决锁帧问题）
            if (m_currentAverageFrameTime > targetFrameTime * 1.15f) // 超出目标15%
            {
                return PerformanceStatus.PoorPerformance;
            }
            else if (m_currentAverageFrameTime < FrameTimeThreshold) // 低于阈值，有提升空间
            {
                return PerformanceStatus.ExcellentPerformance;
            }
            else if (currentFPS >= TargetFPS - 2) // 接近目标帧率
            {
                return PerformanceStatus.GoodPerformance;
            }

            return PerformanceStatus.Stable;
        }

        private bool IsPerformanceStable()
        {
            // 计算帧时间方差
            float variance = 0f;
            for (int i = 0; i < StableFrameCount; i++)
            {
                float diff = m_recentFrameTimes[i] - m_currentAverageFrameTime;
                variance += diff * diff;
            }
            variance /= StableFrameCount;

            return variance < MaxFrameTimeVariance;
        }

        private bool HandlePoorPerformance(float currentScale)
        {
            if (currentScale <= MinScale) return false;

            float adjustmentStep = EnableAdaptiveAdjustment ?
                (m_consecutiveDownAdjustments >= 2 ? AggressiveScaleStep : ScaleStep) :
                ScaleStep;

            m_urpAsset.renderScale = Mathf.Max(MinScale, currentScale - adjustmentStep);
            m_consecutiveDownAdjustments++;
            m_consecutiveUpAdjustments = 0;

            if (ShowDebugInfo)
            {
                Debug.Log($"Poor performance detected. Render scale decreased to: {m_urpAsset.renderScale:F2}");
            }

            return true;
        }

        private bool HandleExcellentPerformance(float currentScale)
        {
            if (currentScale >= MaxScale ||
                m_consecutiveUpAdjustments >= ConsecutiveAdjustmentLimit)
                return false;

            float adjustmentStep = EnableAdaptiveAdjustment ? ConservativeScaleStep : ScaleStep;

            m_urpAsset.renderScale = Mathf.Min(MaxScale, currentScale + adjustmentStep);
            m_consecutiveUpAdjustments++;
            m_consecutiveDownAdjustments = 0;

            if (ShowDebugInfo)
            {
                Debug.Log($"Excellent performance detected. Render scale increased to: {m_urpAsset.renderScale:F2}");
            }

            return true;
        }

        private bool HandleGoodPerformance(float currentScale)
        {
            // 性能良好时，偶尔尝试小幅提升
            if (currentScale < MaxScale &&
                m_consecutiveUpAdjustments == 0 &&
                m_stablePerformanceCounter > StableFrameCount * 2)
            {
                m_urpAsset.renderScale = Mathf.Min(MaxScale, currentScale + ConservativeScaleStep);
                m_consecutiveUpAdjustments++;
                m_stablePerformanceCounter = 0;

                if (ShowDebugInfo)
                {
                    Debug.Log($"Good stable performance. Conservative scale increase to: {m_urpAsset.renderScale:F2}");
                }

                return true;
            }

            m_stablePerformanceCounter++;
            return false;
        }

        private void ResetConsecutiveCounters()
        {
            m_consecutiveDownAdjustments = 0;
            m_consecutiveUpAdjustments = 0;
            m_stablePerformanceCounter = 0;
        }

        private enum PerformanceStatus
        {
            PoorPerformance,    // 性能不足，需要降低质量
            GoodPerformance,    // 性能良好，可以保持
            ExcellentPerformance, // 性能优秀，可以提升质量
            Unstable,           // 性能不稳定
            Stable              // 性能稳定但不需要调整
        }
    }
}