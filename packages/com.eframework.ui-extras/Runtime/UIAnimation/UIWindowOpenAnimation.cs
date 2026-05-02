using EFramework.Runtime.Tween;
using UnityEngine;

namespace EFramework.Extensions.UI.Extras.UIAnimation
{
    public class UIWindowOpenAnimation : MonoBehaviour
    {
        [SerializeField] private float m_openDuration = 0.22f;
        [SerializeField] private float m_easeOvershoot = 1.4f;
        [SerializeField] private float m_startScale = 0.85f;

        private void Start()
        {
            if (!this.gameObject.activeSelf) this.gameObject.SetActive(true);
            EFrameTween.Kill(this.transform, true);
            this.transform.localScale = Vector3.one * m_startScale; // 起始稍小
            EFrameTween.Float(0f, 1f, m_openDuration, progress =>
            {
                var easedProgress = EvaluateOutBack(progress, m_easeOvershoot);
                this.transform.localScale = Vector3.one * Mathf.LerpUnclamped(m_startScale, 1f, easedProgress);
            }, new EFrameTweenOptions
            {
                Target = this.transform,
                Ease = EFrameEase.Linear,
                IgnoreTimeScale = true
            });
        }

        private void OnDestroy()
        {
            EFrameTween.Kill(this.transform);
        }

        private static float EvaluateOutBack(float value, float overshoot)
        {
            value = Mathf.Clamp01(value) - 1f;
            var coefficient = overshoot + 1f;
            return 1f + coefficient * value * value * value + overshoot * value * value;
        }
    }
}
