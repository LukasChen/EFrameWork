using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EFrame.Runtime.Asset;
using EFrame.Runtime.Audio;
using EFrame.Runtime.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace EFrame.Runtime.Effect.Fly
{
    public readonly struct FlySequenceHandle
    {
        public readonly int Id;
        internal FlySequenceHandle(int id) => Id = id;
        public bool IsValid => Id != 0;
    }

    /// <summary>
    /// 飞行体实例的释放策略。
    /// </summary>
    public enum FlyInstanceDisposeMode
    {
        /// <summary>
        /// 默认：回收到对象池（AssetManager.RecycleToPool）。
        /// </summary>
        RecycleToPool = 0,
        /// <summary>
        /// 直接销毁实例（UnityEngine.Object.Destroy），不回收到池。
        /// </summary>
        Destroy = 1,
    }

    /// <summary>
    /// ScatterThenFly 路径类型的阶段状态
    /// </summary>
    internal enum ScatterThenFlyPhase
    {
        /// <summary>
        /// 散开阶段：从起点快速飞向随机散开点
        /// </summary>
        Scattering = 0,
        /// <summary>
        /// 停留阶段：在散开点停留一段时间
        /// </summary>
        Holding = 1,
        /// <summary>
        /// 飞行阶段：从散开点飞向目标点
        /// </summary>
        Flying = 2,
    }

    public sealed class FlySequenceRequest
    {
        public FlyAnimationConfig Config;
        public int LogicalCount = 1;
        public float Scale = 1f;
        public bool UseUnscaledTime = false;

        public Vector3 StartPosition;
        public Vector3 TargetPosition;

        public Transform Parent;

        /// <summary>
        /// 每个“可视飞行体”到达时回调，参数为本次到达代表的逻辑数量（已做压缩权重）。
        /// </summary>
        public Action<int> OnArrive;

        /// <summary>
        /// 所有可视飞行体到达后回调。
        /// </summary>
        public Action OnCompleted;

        /// <summary>
        /// 可选：覆盖 MaxVisualCount（<=0 则使用配置值）。
        /// </summary>
        public int VisualCountOverride = 0;

        /// <summary>
        /// 飞行体结束时实例的处理方式。默认回收对象池；战斗胜利等一次性表现可设为 Destroy。
        /// </summary>
        public FlyInstanceDisposeMode DisposeMode = FlyInstanceDisposeMode.RecycleToPool;
    }

    internal sealed class FlyItem
    {
        public FlySequence Sequence;

        public GameObject Go;
        public Transform Transform;
        public RectTransform RectTransform;
        public SpriteRenderer SpriteRenderer;
        public Graphic Graphic;

        public float Elapsed;
        public float Duration;

        public Vector3 Start;
        public Vector3 Target;
        public Vector3 Control1;
        public Vector3 Control2;

        public float BaseScale;
        public float RotateSpeedDeg;

        public int ArriveLogicalDelta;

        public FlyAnimationConfig Config;
        public bool UseUnscaledTime;

        public FlyInstanceDisposeMode DisposeMode;

        // === ScatterThenFly 专用字段 ===
        /// <summary>
        /// 当前阶段（仅 ScatterThenFly 路径类型使用）
        /// </summary>
        public ScatterThenFlyPhase ScatterPhase;
        /// <summary>
        /// 散开阶段的目标点（随机生成的中间点）
        /// </summary>
        public Vector3 ScatterTarget;
        /// <summary>
        /// 散开阶段飞行时长
        /// </summary>
        public float ScatterDuration;
        /// <summary>
        /// 停留阶段时长
        /// </summary>
        public float ScatterHoldDuration;
        /// <summary>
        /// 当前阶段已用时间
        /// </summary>
        public float PhaseElapsed;

        public void Reset()
        {
            Sequence = null;
            Go = null;
            Transform = null;
            RectTransform = null;
            SpriteRenderer = null;
            Graphic = null;
            Elapsed = 0f;
            Duration = 0f;
            Start = default;
            Target = default;
            Control1 = default;
            Control2 = default;
            BaseScale = 1f;
            RotateSpeedDeg = 0f;
            ArriveLogicalDelta = 0;
            Config = null;
            UseUnscaledTime = false;
            DisposeMode = FlyInstanceDisposeMode.RecycleToPool;

            // ScatterThenFly
            ScatterPhase = ScatterThenFlyPhase.Scattering;
            ScatterTarget = default;
            ScatterDuration = 0f;
            ScatterHoldDuration = 0f;
            PhaseElapsed = 0f;
        }
    }

    internal sealed class FlySequence
    {
        public int Id;
        public FlyAnimationConfig Config;
        public bool UseUnscaledTime;

        public FlyInstanceDisposeMode DisposeMode;

        public int LogicalCount;
        public int VisualCount;
        public int LogicalDeltaPerVisual;
        public int Remainder;

        public int Spawned;
        public int Completed;
        public float NextSpawnAt;
        public float Time;

        public float Scale;
        public Vector3 Start;
        public Vector3 Target;
        public Transform Parent;

        public Action<int> OnArrive;
        public Action OnCompleted;

        public bool FlySoundPlayed;
        public bool ArriveSoundPlayed;

        public void Reset()
        {
            Id = 0;
            Config = null;
            UseUnscaledTime = false;
            DisposeMode = FlyInstanceDisposeMode.RecycleToPool;
            LogicalCount = 0;
            VisualCount = 0;
            LogicalDeltaPerVisual = 0;
            Remainder = 0;
            Spawned = 0;
            Completed = 0;
            NextSpawnAt = 0f;
            Time = 0f;
            Scale = 1f;
            Start = default;
            Target = default;
            Parent = null;
            OnArrive = null;
            OnCompleted = null;
            FlySoundPlayed = false;
            ArriveSoundPlayed = false;
        }
    }

    /// <summary>
    /// 框架层通用飞行动画系统：
    /// - 单 Update 驱动（订阅 AppUpdateEvent）
    /// - 复用 IAssetService 对象池（Addressables assetId）
    /// - 统一支持 SpriteRenderer 与 UI Image（Graphic）
    /// </summary>
    public static class FlyAnimationSystem
    {
        private static bool m_initialized;
        private static readonly List<FlySequence> s_sequences = new(16);
        private static readonly List<FlyItem> s_items = new(256);

        private static readonly Stack<FlySequence> s_sequencePool = new(16);
        private static readonly Stack<FlyItem> s_itemPool = new(256);

        private static int m_nextSequenceId = 1;

        private static readonly Dictionary<string, AudioClipAsset> s_audioClipAssetCache = new(32);

        public static FlySequenceHandle Play(FlySequenceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Config == null) throw new ArgumentNullException(nameof(request.Config));
            if (!request.Config.HasValidAsset) throw new ArgumentException("FlyAnimationConfig has no valid asset.");

            EnsureInitialized();

#if UNITY_EDITOR || LTS_DEV
            Debug.Log($"[FlyAnimationSystem] Play: cfg={request.Config.name}, logical={request.LogicalCount}, scale={request.Scale}, unscaled={request.UseUnscaledTime}");
#endif

            var sequence = GetSequence();
            sequence.Id = m_nextSequenceId++;
            if (m_nextSequenceId == int.MaxValue) m_nextSequenceId = 1;

            sequence.Config = request.Config;
            sequence.UseUnscaledTime = request.UseUnscaledTime;
            sequence.DisposeMode = request.DisposeMode;
            sequence.LogicalCount = Mathf.Max(1, request.LogicalCount);

            int maxVisual = request.VisualCountOverride > 0 ? request.VisualCountOverride : sequence.Config.MaxVisualCount;
            sequence.VisualCount = Mathf.Clamp(sequence.LogicalCount, 1, Mathf.Max(1, maxVisual));

            sequence.LogicalDeltaPerVisual = sequence.LogicalCount / sequence.VisualCount;
            sequence.Remainder = sequence.LogicalCount - (sequence.LogicalDeltaPerVisual * sequence.VisualCount);
            if (sequence.LogicalDeltaPerVisual <= 0) sequence.LogicalDeltaPerVisual = 1;

            sequence.Scale = request.Scale <= 0f ? 1f : request.Scale;
            sequence.Start = request.StartPosition;
            sequence.Target = request.TargetPosition;
            sequence.Parent = request.Parent;
            sequence.OnArrive = request.OnArrive;
            sequence.OnCompleted = request.OnCompleted;

            sequence.Time = 0f;
            sequence.NextSpawnAt = 0f;

            s_sequences.Add(sequence);

            // 可选：序列级音效
            TryPlaySequenceStartSound(sequence);

            return new FlySequenceHandle(sequence.Id);
        }

        private static void EnsureInitialized()
        {
            if (m_initialized) return;
            m_initialized = true;
            EnsureFallbackDriverExists();
        }

        internal static void ManualUpdate(float dt, float unscaledDt)
        {
            UpdateSequences(dt, unscaledDt);
            UpdateItems(dt, unscaledDt);
        }

        private static void EnsureFallbackDriverExists()
        {
            FlyAnimationSystemDriver.EnsureExists();
        }

        private static void UpdateSequences(float dt, float unscaledDt)
        {
            if (s_sequences.Count == 0) return;

            for (int i = s_sequences.Count - 1; i >= 0; i--)
            {
                var seq = s_sequences[i];
                float step = seq.UseUnscaledTime ? unscaledDt : dt;
                seq.Time += step;

                while (seq.Spawned < seq.VisualCount && seq.Time >= seq.NextSpawnAt)
                {
                    SpawnItem(seq);
                    seq.Spawned++;

                    float interval = ComputeInterval(seq, seq.Spawned - 1, seq.VisualCount);
                    seq.NextSpawnAt += interval;
                }

                if (seq.Completed >= seq.VisualCount && seq.VisualCount > 0)
                {
                    s_sequences.RemoveAt(i);
                    seq.OnCompleted?.Invoke();
                    RecycleSequence(seq);
                }
            }
        }

        private static float ComputeInterval(FlySequence seq, int index, int total)
        {
            float baseInterval = Mathf.Max(0f, seq.Config.BaseInterval);
            if (total <= 1) return baseInterval;

            float t = Mathf.Clamp01(index / (float)(total - 1));
            float mul = 1f;
            try
            {
                mul = seq.Config.IntervalMultiplier != null ? seq.Config.IntervalMultiplier.Evaluate(t) : 1f;
            }
            catch
            {
                mul = 1f;
            }
            return Mathf.Max(0f, baseInterval * Mathf.Max(0f, mul));
        }

        private static void SpawnItem(FlySequence seq)
        {
            var item = GetItem();
            item.Sequence = seq;
            item.Config = seq.Config;
            item.UseUnscaledTime = seq.UseUnscaledTime;
            item.DisposeMode = seq.DisposeMode;

            item.Duration = Mathf.Max(0.01f, seq.Config.Duration);
            item.Elapsed = 0f;

            // 逻辑权重：让前 Remainder 个多 +1（保证总和正确）
            int logicalDelta = seq.LogicalDeltaPerVisual;
            if (seq.Remainder > 0)
            {
                logicalDelta += 1;
                seq.Remainder -= 1;
            }
            item.ArriveLogicalDelta = logicalDelta;

            // 起点 - 对于 ScatterThenFly，不使用起点随机散开，而是用散开阶段处理
            Vector3 start = seq.Start;
            if (seq.Config.PathType == FlyPathType.ScatterThenFly)
            {
                // ScatterThenFly: 起点固定，散开目标点随机
                item.Start = start;
                item.Target = seq.Target;

                // 计算散开目标点（以起点为圆心，在半径范围内随机）
                float r = seq.Config.StartRandomRadius > 0f ? seq.Config.StartRandomRadius : 50f;
                var v2 = UnityEngine.Random.insideUnitCircle * r;
                item.ScatterTarget = start + new Vector3(v2.x, v2.y, 0f);

                // 设置阶段参数
                item.ScatterPhase = ScatterThenFlyPhase.Scattering;
                item.ScatterDuration = Mathf.Max(0.01f, seq.Config.ScatterDuration);
                item.ScatterHoldDuration = Mathf.Max(0f, seq.Config.ScatterHoldDuration);
                item.PhaseElapsed = 0f;
            }
            else
            {
                // 起点随机散开（原有逻辑）
                if (seq.Config.StartRandomRadius > 0f)
                {
                    var r = seq.Config.StartRandomRadius;
                    var v2 = UnityEngine.Random.insideUnitCircle * r;
                    start += new Vector3(v2.x, v2.y, 0f);
                }

                item.Start = start;
                item.Target = seq.Target;
            }

            SetupControls(item);

            item.BaseScale = seq.Scale;
            item.RotateSpeedDeg = seq.Config.RotateSpeedDeg;

            // 实例化（池化）
            var go = SpawnGameObject(seq.Config, seq.Parent, start);
            if (go == null)
            {
                Debug.LogError($"[FlyAnimationSystem] Spawn failed: prefab asset id is invalid. Config: {seq.Config.name}");
                seq.Completed++;
                RecycleItem(item);
                return;
            }

            item.Go = go;
            item.Transform = go.transform;
            item.RectTransform = go.GetComponent<RectTransform>();
            go.transform.position = start;
            go.transform.localScale = Vector3.one * item.BaseScale;

            // 开始特效（每个飞行体 spawn 时）
            TrySpawnEffect(seq.Config.StartEffectAssetId, seq.Parent, start, seq.Config.StartEffectDestroyTime);

            // 缓存渲染器引用（SpriteRenderer 或 Graphic）
            go.TryGetComponent(out item.SpriteRenderer);
            if (item.SpriteRenderer == null)
            {
                item.Graphic = go.GetComponentInChildren<Graphic>(true);
            }

            // 每物体音效（可选）
            TryPlayPerItemStartSound(seq.Config);

            s_items.Add(item);
        }

        private static void SetupControls(FlyItem item)
        {
            var cfg = item.Config;
            if (cfg.PathType == FlyPathType.Linear)
            {
                item.Control1 = default;
                item.Control2 = default;
                return;
            }

            if (cfg.PathType == FlyPathType.QuadraticBezier)
            {
                Vector3 mid = (item.Start + item.Target) * 0.5f;
                item.Control1 = mid + cfg.QuadraticControlOffset;
                item.Control2 = default;
                return;
            }

            if (cfg.PathType == FlyPathType.ScatterThenFly)
            {
                // ScatterThenFly 飞行阶段使用 CubicBezier
                item.Control1 = item.Start + cfg.ScatterFlyControl1Offset;
                item.Control2 = item.Target + cfg.ScatterFlyControl2Offset;
                return;
            }

            // CubicBezier
            item.Control1 = item.Start + cfg.CubicControl1Offset;
            item.Control2 = item.Target + cfg.CubicControl2Offset;
        }

        private static void UpdateItems(float dt, float unscaledDt)
        {
            if (s_items.Count == 0) return;

            for (int i = s_items.Count - 1; i >= 0; i--)
            {
                var item = s_items[i];
                float step = item.UseUnscaledTime ? unscaledDt : dt;

                // ScatterThenFly 路径类型使用专门的更新逻辑
                if (item.Config.PathType == FlyPathType.ScatterThenFly)
                {
                    UpdateScatterThenFlyItem(item, step, i);
                    continue;
                }

                // 原有的更新逻辑（Linear / QuadraticBezier / CubicBezier）
                UpdateStandardFlyItem(item, step, i);
            }
        }

        /// <summary>
        /// 更新标准飞行物品（Linear / QuadraticBezier / CubicBezier）
        /// </summary>
        private static void UpdateStandardFlyItem(FlyItem item, float step, int index)
        {
            item.Elapsed += step;
            float t = Mathf.Clamp01(item.Elapsed / item.Duration);

            float moveT = EvaluateSafe(item.Config.MoveProgress, t);
            Vector3 pos = EvaluatePosition(item, moveT);

            item.Transform.position = pos;

            // scale / alpha
            float scaleMul = EvaluateSafe(item.Config.ScaleOverLife, t);
            item.Transform.localScale = Vector3.one * (item.BaseScale * scaleMul);

            float alpha = Mathf.Clamp01(EvaluateSafe(item.Config.AlphaOverLife, t));
            ApplyAlpha(item, alpha);

            if (Mathf.Abs(item.RotateSpeedDeg) > 0.001f)
            {
                item.Transform.Rotate(0f, 0f, item.RotateSpeedDeg * step);
            }

            if (t >= 1f)
            {
                OnItemArrived(item, index);
            }
        }

        /// <summary>
        /// 更新 ScatterThenFly 路径类型的飞行物品
        /// 三阶段：Scattering（散开） -> Holding（停留） -> Flying（飞向目标）
        /// </summary>
        private static void UpdateScatterThenFlyItem(FlyItem item, float step, int index)
        {
            item.PhaseElapsed += step;
            item.Elapsed += step; // 总时长仍累计（用于生命周期曲线）

            // 计算总时长用于生命周期曲线
            float totalDuration = item.ScatterDuration + item.ScatterHoldDuration + item.Duration;
            float totalT = Mathf.Clamp01(item.Elapsed / totalDuration);

            // 旋转（所有阶段都执行）
            if (Mathf.Abs(item.RotateSpeedDeg) > 0.001f)
            {
                item.Transform.Rotate(0f, 0f, item.RotateSpeedDeg * step);
            }

            // 缩放和透明度基于总生命周期
            float scaleMul = EvaluateSafe(item.Config.ScaleOverLife, totalT);
            item.Transform.localScale = Vector3.one * (item.BaseScale * scaleMul);

            float alpha = Mathf.Clamp01(EvaluateSafe(item.Config.AlphaOverLife, totalT));
            ApplyAlpha(item, alpha);

            switch (item.ScatterPhase)
            {
                case ScatterThenFlyPhase.Scattering:
                    {
                        float phaseT = Mathf.Clamp01(item.PhaseElapsed / item.ScatterDuration);
                        float moveT = EvaluateSafe(item.Config.ScatterMoveProgress, phaseT);
                        Vector3 pos = Vector3.LerpUnclamped(item.Start, item.ScatterTarget, moveT);
                        item.Transform.position = pos;

                        if (phaseT >= 1f)
                        {
                            // 进入停留阶段
                            item.ScatterPhase = ScatterThenFlyPhase.Holding;
                            item.PhaseElapsed = 0f;
                        }
                    }
                    break;

                case ScatterThenFlyPhase.Holding:
                    {
                        // 停留阶段，保持在散开位置不动
                        item.Transform.position = item.ScatterTarget;

                        if (item.PhaseElapsed >= item.ScatterHoldDuration)
                        {
                            // 进入飞行阶段，更新起点为当前散开点
                            item.ScatterPhase = ScatterThenFlyPhase.Flying;
                            item.PhaseElapsed = 0f;
                            item.Start = item.ScatterTarget;
                            // 重新设置控制点（使用原有的目标点飞行配置）
                            SetupControls(item);
                        }
                    }
                    break;

                case ScatterThenFlyPhase.Flying:
                    {
                        // 飞向目标阶段，使用配置的曲线和时长
                        float phaseT = Mathf.Clamp01(item.PhaseElapsed / item.Duration);
                        float moveT = EvaluateSafe(item.Config.MoveProgress, phaseT);
                        Vector3 pos = EvaluatePosition(item, moveT);
                        item.Transform.position = pos;

                        if (phaseT >= 1f)
                        {
                            OnItemArrived(item, index);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 飞行物品到达目标时的处理
        /// </summary>
        private static void OnItemArrived(FlyItem item, int index)
        {
            // 到达：更新序列完成计数 + 回调（按权重）
            var seq = item.Sequence;
            if (seq != null)
            {
                seq.Completed++;

                // 序列级到达音效
                TryPlaySequenceArriveSound(seq);

                seq.OnArrive?.Invoke(item.ArriveLogicalDelta);
            }

            // 每物体到达音效（可选）
            TryPlayPerItemArriveSound(item.Config);

            // 结束特效（每个飞行体到达时）
            if (item.Config != null)
            {
                Transform effectParent = item.Transform != null ? item.Transform.parent : null;
                TrySpawnEffect(item.Config.EndEffectAssetId, effectParent, item.Target, item.Config.EndEffectDestroyTime);
            }

            // 回收
            s_items.RemoveAt(index);
            RecycleItem(item);
        }

        private static Vector3 EvaluatePosition(FlyItem item, float t)
        {
            switch (item.Config.PathType)
            {
                case FlyPathType.Linear:
                    return Vector3.LerpUnclamped(item.Start, item.Target, t);
                case FlyPathType.QuadraticBezier:
                    return QuadraticBezier(item.Start, item.Control1, item.Target, t);
                case FlyPathType.CubicBezier:
                    return CubicBezier(item.Start, item.Control1, item.Control2, item.Target, t);
                case FlyPathType.ScatterThenFly:
                    // ScatterThenFly 飞行阶段使用 CubicBezier
                    return CubicBezier(item.Start, item.Control1, item.Control2, item.Target, t);
                default:
                    return Vector3.LerpUnclamped(item.Start, item.Target, t);
            }
        }

        private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
        }

        private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * p0)
                   + (3f * uu * t * p1)
                   + (3f * u * tt * p2)
                   + (ttt * p3);
        }

        private static float EvaluateSafe(AnimationCurve curve, float t)
        {
            if (curve == null) return t;
            try
            {
                return curve.Evaluate(t);
            }
            catch
            {
                return t;
            }
        }

        private static void ApplyAlpha(FlyItem item, float alpha)
        {
            if (item.SpriteRenderer != null)
            {
                var c = item.SpriteRenderer.color;
                c.a = alpha;
                item.SpriteRenderer.color = c;
                return;
            }

            if (item.Graphic != null)
            {
                var c = item.Graphic.color;
                c.a = alpha;
                item.Graphic.color = c;
            }
        }

        private static GameObject SpawnGameObject(FlyAnimationConfig cfg, Transform parent, Vector3 pos)
        {
            var assets = EFrame.Current?.Assets;
            if (assets == null)
            {
                Debug.LogError("[FlyAnimationSystem] Spawn failed: EFrame asset service is not available.");
                return null;
            }

            if (parent != null) return assets.GetFromPool(cfg.PrefabAssetId, parent, pos);
            return assets.GetFromPool(cfg.PrefabAssetId, pos);
        }

        private static void RecycleGameObject(FlyItem item)
        {
            if (item.Go == null) return;

            // Destroy 模式：不回收到池，直接销毁实例。
            if (item.DisposeMode == FlyInstanceDisposeMode.Destroy)
            {
                AssetManager.ReleaseInstance(item.Go);
                return;
            }

            // 重置可见性/透明度，避免池化残留
            ApplyAlpha(item, 1f);

            if (item.Config != null)
            {
                var assets = EFrame.Current?.Assets;
                if (assets != null)
                {
                    assets.RecycleToPool(item.Config.PrefabAssetId, item.Go);
                    return;
                }

                AssetManager.ReleaseInstance(item.Go);
                return;
            }

            AssetManager.ReleaseInstance(item.Go);
        }

        private static FlySequence GetSequence()
        {
            if (s_sequencePool.Count > 0)
            {
                var seq = s_sequencePool.Pop();
                seq.Reset();
                return seq;
            }

            return new FlySequence();
        }

        private static void RecycleSequence(FlySequence seq)
        {
            if (seq == null) return;
            seq.Reset();
            s_sequencePool.Push(seq);
        }

        private static FlyItem GetItem()
        {
            if (s_itemPool.Count > 0)
            {
                var item = s_itemPool.Pop();
                item.Reset();
                return item;
            }

            return new FlyItem();
        }

        private static void RecycleItem(FlyItem item)
        {
            if (item == null) return;
            RecycleGameObject(item);
            item.Reset();
            s_itemPool.Push(item);
        }

        private static void TryPlaySequenceStartSound(FlySequence seq)
        {
            var cfg = seq.Config;
            if (cfg == null) return;
            if (cfg.FlySoundMode != FlyAudioPlayMode.OncePerSequence) return;
            if (seq.FlySoundPlayed) return;

            if (!EFrame.Runtime.EFrame.Initialized || EFrame.Runtime.EFrame.Current?.Audio == null) return;

            seq.FlySoundPlayed = true;

            TryPlayAudio(cfg.FlySoundAssetId);
        }

        private static void TryPlaySequenceArriveSound(FlySequence seq)
        {
            var cfg = seq.Config;
            if (cfg == null) return;
            if (cfg.ArriveSoundMode != FlyAudioPlayMode.OncePerSequence) return;
            if (seq.ArriveSoundPlayed) return;

            if (!EFrame.Runtime.EFrame.Initialized || EFrame.Runtime.EFrame.Current?.Audio == null) return;

            seq.ArriveSoundPlayed = true;

            TryPlayAudio(cfg.ArriveSoundAssetId);
        }

        private static void TryPlayPerItemStartSound(FlyAnimationConfig cfg)
        {
            if (cfg == null) return;
            if (cfg.FlySoundMode != FlyAudioPlayMode.PerItem) return;
            if (!EFrame.Runtime.EFrame.Initialized || EFrame.Runtime.EFrame.Current?.Audio == null) return;

            TryPlayAudio(cfg.FlySoundAssetId);
        }

        private static void TryPlayPerItemArriveSound(FlyAnimationConfig cfg)
        {
            if (cfg == null) return;
            if (cfg.ArriveSoundMode != FlyAudioPlayMode.PerItem) return;
            if (!EFrame.Runtime.EFrame.Initialized || EFrame.Runtime.EFrame.Current?.Audio == null) return;

            TryPlayAudio(cfg.ArriveSoundAssetId);
        }

        private static bool TryPlayAudio(string audioClipAssetId)
        {
            var audio = EFrame.Current?.Audio;
            if (audio == null || !audio.SoundOn)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(audioClipAssetId))
            {
                try
                {
                    var clipAsset = ResolveAudioClipAsset(audioClipAssetId);
                    if (clipAsset != null)
                    {
                        audio.PlayAudioClipAsset(clipAsset);
                        return true;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        private static void TrySpawnEffect(string effectAssetId, Transform parent, Vector3 pos, float destroyTime)
        {
            if (string.IsNullOrEmpty(effectAssetId)) return;

            var assets = EFrame.Current?.Assets;
            if (assets == null)
            {
                Debug.LogWarning($"[FlyAnimationSystem] Effect skipped because EFrame asset service is not available. AssetId: {effectAssetId}");
                return;
            }

            var go = assets.Instantiate(effectAssetId, parent);
            if (go == null) return;

            go.transform.position = pos;
            if (destroyTime > 0f)
            {
                ReleaseEffectAfterDelay(go, destroyTime).Forget();
            }
        }

        private static async UniTaskVoid ReleaseEffectAfterDelay(GameObject go, float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            AssetManager.ReleaseInstance(go);
        }

        private static AudioClipAsset ResolveAudioClipAsset(string audioClipAssetId)
        {
            if (string.IsNullOrEmpty(audioClipAssetId)) return null;

            var assets = EFrame.Current?.Assets;
            if (assets != null && assets.TryGetPreloadedAsset<AudioClipAsset>(audioClipAssetId, out var preloaded))
            {
                return preloaded;
            }

            if (s_audioClipAssetCache.TryGetValue(audioClipAssetId, out var cached))
            {
                return cached;
            }

            var loaded = AssetManager.LoadAsset<AudioClipAsset>(audioClipAssetId);
            if (loaded != null)
            {
                s_audioClipAssetCache[audioClipAssetId] = loaded;
            }
            return loaded;
        }
    }
}
