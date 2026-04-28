using UnityEngine;

namespace EFrameWork.Runtime.Effect.IconBounce
{
    /// <summary>
    /// 编辑器场景内快速调试用：挂到任意 GameObject，配置后一键预览回弹效果。
    /// 适用于在编辑器中调整回弹曲线参数。
    /// </summary>
    public sealed class IconBounceDebugRunner : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("要应用回弹效果的目标 Transform（如果为空则使用自身）")]
        public Transform Target;

        [Header("Configuration")]
        [Tooltip("回弹效果配置（ScriptableObject）。如果为空，将使用下方的内联配置")]
        public IconBounceConfig Config;

        [Header("Inline Configuration (used when Config is null)")]
        [Min(0.01f)]
        public float Duration = 0.5f;

        [Range(0f, 1f)]
        public float BounceStrength = 0.2f;

        [Tooltip("回弹曲线：t(0..1) -> scale offset multiplier")]
        public AnimationCurve BounceCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.15f, 1f, 0f, 0f),
            new Keyframe(0.35f, -0.5f, 0f, 0f),
            new Keyframe(0.55f, 0.25f, 0f, 0f),
            new Keyframe(0.75f, -0.1f, 0f, 0f),
            new Keyframe(1f, 0f, 0f, 0f)
        );

        public bool UseUnscaledTime = false;

        private IconBounceEffect m_effect;

        private void Awake()
        {
            SetupEffect();
        }

        private void SetupEffect()
        {
            Transform target = Target != null ? Target : transform;
            
            m_effect = target.GetComponent<IconBounceEffect>();
            if (m_effect == null)
            {
                m_effect = target.gameObject.AddComponent<IconBounceEffect>();
            }

            ApplyConfig();
        }

        private void ApplyConfig()
        {
            if (m_effect == null) return;

            if (Config != null)
            {
                m_effect.Config = Config;
            }
            else
            {
                m_effect.Config = null;
                m_effect.Duration = Duration;
                m_effect.BounceStrength = BounceStrength;
                m_effect.BounceCurve = BounceCurve;
                m_effect.UseUnscaledTime = UseUnscaledTime;
            }
        }

        /// <summary>
        /// 播放回弹效果（由 Inspector 按钮调用或外部调用）
        /// </summary>
        public void Play()
        {
            if (m_effect == null)
            {
                SetupEffect();
            }

            if (m_effect != null)
            {
                ApplyConfig();
                m_effect.Play();
                Debug.Log($"[IconBounceDebugRunner] Playing bounce effect on {m_effect.transform.name}", this);
            }
            else
            {
                Debug.LogError($"[IconBounceDebugRunner] Cannot play: effect component not found.", this);
            }
        }

        /// <summary>
        /// 停止回弹效果
        /// </summary>
        public void Stop()
        {
            if (m_effect != null)
            {
                m_effect.Stop();
            }
        }

        private void OnValidate()
        {
            Duration = Mathf.Max(0.01f, Duration);
            BounceStrength = Mathf.Clamp01(BounceStrength);
            BounceCurve ??= AnimationCurve.Linear(0, 1, 1, 0);
        }
    }
}
