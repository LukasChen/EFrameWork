using UnityEngine;

namespace EFrameWork.Runtime.Effect.IconBounce
{
    /// <summary>
    /// 图标回弹效果配置（ScriptableObject）
    /// 用于配置金币到达时的回弹动画参数
    /// </summary>
    [CreateAssetMenu(menuName = "EFrameWork/Effect/Icon Bounce Config", fileName = "IconBounceConfig")]
    public sealed class IconBounceConfig : ScriptableObject
    {
        [Header("Bounce Settings")]
        [Tooltip("回弹动画总时长（秒）")]
        [Min(0.01f)]
        public float Duration = 0.5f;

        [Tooltip("回弹幅度（相对于原始缩放的倍数，1.0表示100%）")]
        [Range(0f, 1f)]
        public float BounceStrength = 0.2f;

        [Tooltip("回弹曲线：t(0..1) -> scale offset multiplier\n建议使用阻尼振荡曲线，如从1快速衰减到0")]
        public AnimationCurve BounceCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.15f, 1f, 0f, 0f),
            new Keyframe(0.35f, -0.5f, 0f, 0f),
            new Keyframe(0.55f, 0.25f, 0f, 0f),
            new Keyframe(0.75f, -0.1f, 0f, 0f),
            new Keyframe(1f, 0f, 0f, 0f)
        );

        [Header("Advanced")]
        [Tooltip("是否使用 Unscaled Time（不受 Time.timeScale 影响）")]
        public bool UseUnscaledTime = false;

        [Tooltip("允许叠加播放：如果为 true，新的回弹会叠加到当前状态；否则会重置后播放")]
        public bool AllowStacking = false;

        private void OnValidate()
        {
            Duration = Mathf.Max(0.01f, Duration);
            BounceStrength = Mathf.Clamp01(BounceStrength);
            BounceCurve ??= AnimationCurve.Linear(0, 1, 1, 0);
        }

        /// <summary>
        /// 根据归一化时间 t 获取缩放偏移量
        /// </summary>
        /// <param name="t">归一化时间 [0, 1]</param>
        /// <returns>缩放偏移量（需要乘以 BounceStrength 再加到基础缩放上）</returns>
        public float EvaluateScale(float t)
        {
            return BounceCurve.Evaluate(Mathf.Clamp01(t)) * BounceStrength;
        }
    }
}
