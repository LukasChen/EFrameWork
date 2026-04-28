using System;
using EFrameWork.Runtime.Audio;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EFrameWork.Runtime.Effect.Fly
{
    public enum FlyPathType
    {
        Linear = 0,
        QuadraticBezier = 1,
        CubicBezier = 2,
        /// <summary>
        /// 散开后飞向目标：先快速飞向起始点附近的随机点，停留一段时间，然后飞向目标点。
        /// </summary>
        ScatterThenFly = 3,
    }

    public enum FlyAudioPlayMode
    {
        None = 0,
        OncePerSequence = 1,
        PerItem = 2,
    }

    [CreateAssetMenu(menuName = "EFrameWork/Fly/Fly Animation Config", fileName = "FlyAnimationConfig")]
    public sealed class FlyAnimationConfig : ScriptableObject
    {
        [Header("Asset")]
        [Tooltip("飞行体预制体引用（Addressables AssetReference）")]
        public AssetReferenceGameObject PrefabReference;

        [Header("Motion")]
        public FlyPathType PathType = FlyPathType.QuadraticBezier;

        [Min(0.01f)]
        public float Duration = 0.7f;

        [Tooltip("t(0..1) -> eased(0..1)，用于沿轨迹前进的节奏曲线")]
        public AnimationCurve MoveProgress = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("整体生命周期缩放倍数：t(0..1) -> scaleMul")]
        public AnimationCurve ScaleOverLife = AnimationCurve.Linear(0, 1, 1, 1);

        [Tooltip("整体生命周期透明度：t(0..1) -> alpha(0..1)")]
        public AnimationCurve AlphaOverLife = AnimationCurve.Linear(0, 1, 1, 1);

        [Tooltip("起点随机偏移半径（在 startPos 附近随机散开）")]
        [Min(0f)]
        public float StartRandomRadius = 0f;

        [Tooltip("每秒旋转角速度（度/秒）。0 表示不旋转")]
        public float RotateSpeedDeg = 0f;

        [Header("ScatterThenFly")]
        [Tooltip("【ScatterThenFly】散开阶段的飞行时长（秒）")]
        [Min(0.01f)]
        public float ScatterDuration = 0.15f;

        [Tooltip("【ScatterThenFly】散开阶段的移动曲线：t(0..1) -> eased(0..1)")]
        public AnimationCurve ScatterMoveProgress = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("【ScatterThenFly】散开后停留时长（秒）")]
        [Min(0f)]
        public float ScatterHoldDuration = 0.1f;

        [Tooltip("【ScatterThenFly】飞向目标阶段控制点1偏移（相对于散开点）")]
        public Vector3 ScatterFlyControl1Offset = new Vector3(0, 100f, 0);

        [Tooltip("【ScatterThenFly】飞向目标阶段控制点2偏移（相对于目标点）")]
        public Vector3 ScatterFlyControl2Offset = new Vector3(0, 100f, 0);

        [Header("Bezier")]
        [Tooltip("二次贝塞尔：控制点 = midpoint(start,target) + offset")]
        public Vector3 QuadraticControlOffset = new Vector3(0, -0.5f, 0);

        [Tooltip("三次贝塞尔：控制点1 = start + offset，控制点2 = target + offset")]
        public Vector3 CubicControl1Offset = new Vector3(0, -0.5f, 0);

        public Vector3 CubicControl2Offset = new Vector3(0, -0.5f, 0);

        [Header("Sequence")]
        [Tooltip("序列发射基础间隔（秒）")]
        [Min(0f)]
        public float BaseInterval = 0.05f;

        [Tooltip("多物品队列节奏：i(0..1) -> intervalMul；最终 interval = BaseInterval * intervalMul")]
        public AnimationCurve IntervalMultiplier = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 0.2f),
            new Keyframe(1f, 1f));

        [Tooltip("可视上限：逻辑数量很大时会压缩为最多这么多个飞行体；到达回调按权重累计")]
        [Min(1)]
        public int MaxVisualCount = 200;

        [Header("Audio")]
        public FlyAudioPlayMode FlySoundMode = FlyAudioPlayMode.OncePerSequence;

        [Tooltip("音效资源引用（AudioClipAsset）。如果设置则优先生效")]
        public AssetReferenceT<AudioClipAsset> FlySoundAsset;

        public FlyAudioPlayMode ArriveSoundMode = FlyAudioPlayMode.OncePerSequence;

        [Tooltip("音效资源引用（AudioClipAsset）。如果设置则优先生效")]
        public AssetReferenceT<AudioClipAsset> ArriveSoundAsset;

        [Header("Effect")]
        [Tooltip("开始特效预制体引用（每个飞行体 spawn 时播放一次）。为空则不播放")]
        public AssetReferenceGameObject StartEffect;

        [Min(0f)]
        [Tooltip("开始特效自动销毁时间（秒），0 表示不自动销毁")]
        public float StartEffectDestroyTime = 2f;

        [Tooltip("结束特效预制体引用（每个飞行体到达时播放一次）。为空则不播放")]
        public AssetReferenceGameObject EndEffect;

        [Min(0f)]
        [Tooltip("结束特效自动销毁时间（秒），0 表示不自动销毁")]
        public float EndEffectDestroyTime = 2f;

        public bool HasValidAsset
        {
            get
            {
                return PrefabReference != null && PrefabReference.RuntimeKeyIsValid();
            }
        }

        private void OnValidate()
        {
            Duration = Mathf.Max(0.01f, Duration);
            MaxVisualCount = Mathf.Max(1, MaxVisualCount);
            BaseInterval = Mathf.Max(0f, BaseInterval);

            StartEffectDestroyTime = Mathf.Max(0f, StartEffectDestroyTime);
            EndEffectDestroyTime = Mathf.Max(0f, EndEffectDestroyTime);

            // ScatterThenFly
            ScatterDuration = Mathf.Max(0.01f, ScatterDuration);
            ScatterHoldDuration = Mathf.Max(0f, ScatterHoldDuration);

            MoveProgress ??= AnimationCurve.EaseInOut(0, 0, 1, 1);
            ScaleOverLife ??= AnimationCurve.Linear(0, 1, 1, 1);
            AlphaOverLife ??= AnimationCurve.Linear(0, 1, 1, 1);
            IntervalMultiplier ??= new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
            ScatterMoveProgress ??= AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }
}
