using UnityEngine;

namespace EFramework.Extensions.Effects.IconBounce
{
    /// <summary>
    /// 图标回弹效果组件
    /// 挂载到需要回弹效果的UI元素上，调用 Play() 播放回弹动画
    /// </summary>
    public sealed class IconBounceEffect : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("回弹效果配置（ScriptableObject）。如果为空，将使用内联配置")]
        public IconBounceConfig Config;

        [Header("Inline Configuration (used when Config is null)")]
        [Tooltip("回弹动画总时长（秒）")]
        [Min(0.01f)]
        public float Duration = 0.4f;

        [Tooltip("回弹幅度（相对于原始缩放的倍数）")]
        [Range(0f, 1f)]
        public float BounceStrength = 0.3f;

        [Tooltip("回弹曲线：t(0..1) -> scale offset multiplier，先缩小再弹回原始大小")]
        public AnimationCurve BounceCurve = new AnimationCurve(
            // 快速缩小到最小值
            new Keyframe(0f, -1f, 0f, 3f),
            // 第一次回弹（略微超过原始大小）
            new Keyframe(0.25f, 0.15f, 2f, -1f),
            // 第二次回弹（轻微缩小）
            new Keyframe(0.5f, -0.08f, 0.5f, 0.5f),
            // 第三次回弹（非常轻微超过）
            new Keyframe(0.75f, 0.03f, 0f, -0.2f),
            // 平滑归位到原始大小
            new Keyframe(1f, 0f, -0.1f, 0f)
        );

        [Tooltip("是否使用 Unscaled Time")]
        public bool UseUnscaledTime = false;

        [Tooltip("允许叠加播放")]
        public bool AllowStacking = false;

        private Vector3 m_originalScale;
        private float m_elapsed;
        private bool m_isPlaying;
        private bool m_hasOriginalScale;

        private void Awake()
        {
            CacheOriginalScale();
        }

        private void OnEnable()
        {
            if (!m_hasOriginalScale)
            {
                CacheOriginalScale();
            }
        }

        private void OnDisable()
        {
            // 禁用时立即恢复原始缩放
            if (m_isPlaying && m_hasOriginalScale)
            {
                transform.localScale = m_originalScale;
            }
            m_isPlaying = false;
            m_elapsed = 0f;
        }

        private void Update()
        {
            if (!m_isPlaying) return;

            float dt = GetUseUnscaledTime() ? Time.unscaledDeltaTime : Time.deltaTime;
            m_elapsed += dt;

            float duration = GetDuration();
            float t = Mathf.Clamp01(m_elapsed / duration);

            // 计算当前缩放：1 + offset，使得 offset=0 时保持原始大小
            float scaleOffset = EvaluateScale(t);
            transform.localScale = m_originalScale * scaleOffset;

            // 动画完成
            if (m_elapsed >= duration)
            {
                transform.localScale = m_originalScale;
                m_isPlaying = false;
                m_elapsed = 0f;
            }
        }

        /// <summary>
        /// 播放回弹效果
        /// </summary>
        public void Play()
        {
            if (!m_hasOriginalScale)
            {
                CacheOriginalScale();
            }

            if (!GetAllowStacking() || !m_isPlaying)
            {
                // 重置到原始缩放后开始
                transform.localScale = m_originalScale;
                m_elapsed = 0f;
            }
            // 如果允许叠加且正在播放，继续当前状态，只是重置时间
            else if (GetAllowStacking() && m_isPlaying)
            {
                // 叠加模式：保持当前缩放作为新的基础
                m_originalScale = transform.localScale;
                m_elapsed = 0f;
            }

            m_isPlaying = true;
        }

        /// <summary>
        /// 停止回弹效果并恢复原始缩放
        /// </summary>
        public void Stop()
        {
            if (m_isPlaying && m_hasOriginalScale)
            {
                transform.localScale = m_originalScale;
            }
            m_isPlaying = false;
            m_elapsed = 0f;
        }

        /// <summary>
        /// 重新缓存原始缩放（在外部修改了基础缩放后调用）
        /// </summary>
        public void RefreshOriginalScale()
        {
            if (!m_isPlaying)
            {
                CacheOriginalScale();
            }
        }

        private void CacheOriginalScale()
        {
            m_originalScale = transform.localScale;
            m_hasOriginalScale = true;
        }

        #region Config Accessors

        private float GetDuration()
        {
            return Config != null ? Config.Duration : Duration;
        }

        private float GetBounceStrength()
        {
            return Config != null ? Config.BounceStrength : BounceStrength;
        }

        private AnimationCurve GetBounceCurve()
        {
            return Config != null ? Config.BounceCurve : BounceCurve;
        }

        private bool GetUseUnscaledTime()
        {
            return Config != null ? Config.UseUnscaledTime : UseUnscaledTime;
        }

        private bool GetAllowStacking()
        {
            return Config != null ? Config.AllowStacking : AllowStacking;
        }

        private float EvaluateScale(float t)
        {
            if (Config != null)
            {
                return Config.EvaluateScale(t);
            }
            return GetBounceCurve().Evaluate(Mathf.Clamp01(t)) * GetBounceStrength();
        }

        #endregion

        #region Editor Preview Support

#if UNITY_EDITOR
        [Header("Editor Preview")]
        [Tooltip("在编辑器中预览回弹效果")]
        public bool PreviewInEditor = false;

        private float m_editorPreviewTime;
        private Vector3 m_editorOriginalScale;
        private bool m_editorHasOriginalScale;

        private void OnValidate()
        {
            Duration = Mathf.Max(0.01f, Duration);
            BounceStrength = Mathf.Clamp01(BounceStrength);
            BounceCurve ??= AnimationCurve.Linear(0, 1, 1, 0);
        }

        /// <summary>
        /// 编辑器中预览回弹效果（由 Editor 脚本调用）
        /// </summary>
        public void EditorPreview()
        {
            if (!m_editorHasOriginalScale)
            {
                m_editorOriginalScale = transform.localScale;
                m_editorHasOriginalScale = true;
            }
            m_editorPreviewTime = 0f;
            UnityEditor.EditorApplication.update += EditorPreviewUpdate;
        }

        /// <summary>
        /// 停止编辑器预览
        /// </summary>
        public void EditorStopPreview()
        {
            UnityEditor.EditorApplication.update -= EditorPreviewUpdate;
            if (m_editorHasOriginalScale)
            {
                transform.localScale = m_editorOriginalScale;
            }
            m_editorPreviewTime = 0f;
        }

        private void EditorPreviewUpdate()
        {
            if (this == null)
            {
                UnityEditor.EditorApplication.update -= EditorPreviewUpdate;
                return;
            }

            float duration = GetDuration();
            m_editorPreviewTime += Time.unscaledDeltaTime;

            if (m_editorPreviewTime >= duration)
            {
                EditorStopPreview();
                return;
            }

            float t = Mathf.Clamp01(m_editorPreviewTime / duration);
            float scaleOffset = EvaluateScale(t);
            transform.localScale = m_editorOriginalScale * (1f + scaleOffset);
            
            UnityEditor.SceneView.RepaintAll();
        }
#endif

        #endregion
    }
}
