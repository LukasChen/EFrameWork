using UnityEngine;

namespace EFramework.Runtime.Effect.Fly
{
    /// <summary>
    /// 场景内快速调试用：挂到任意 GameObject，配置起点/终点/父节点后，一键播放。
    /// </summary>
    public sealed class FlyAnimationDebugRunner : MonoBehaviour
    {
        public FlyAnimationConfig Config;

        [Header("Anchors")]
        public Transform Start;
        public Transform Target;

        [Tooltip("用于 UI 方案时指定父节点（例如某个 UILayer 下）")]
        public Transform Parent;

        [Header("Debug")]
        [Min(1)]
        public int LogicalCount = 50;

        [Min(0.1f)]
        public float Scale = 1f;

        public bool UseUnscaledTime = false;

        public void Play()
        {
            if (Config == null)
            {
                Debug.LogError($"[FlyAnimationDebugRunner] Play failed: Config is null. ({name})", this);
                return;
            }

            if (Start == null || Target == null)
            {
                Debug.LogError($"[FlyAnimationDebugRunner] Play failed: Start/Target is null. Start:{Start} Target:{Target} ({name})", this);
                return;
            }

            if (!Config.HasValidAsset)
            {
                Debug.LogError($"[FlyAnimationDebugRunner] Play failed: Config has no valid asset. ({Config.name})", Config);
                return;
            }

            Debug.Log(
                $"[FlyAnimationDebugRunner] Play: cfg={Config.name}, logical={LogicalCount}, scale={Scale}, unscaled={UseUnscaledTime}, start={Start.position}, target={Target.position}, parent={(Parent != null ? Parent.name : "<null>")}",
                this);

            FlyAnimationSystem.Play(new FlySequenceRequest
            {
                Config = Config,
                LogicalCount = LogicalCount,
                Scale = Scale,
                UseUnscaledTime = UseUnscaledTime,
                StartPosition = Start.position,
                TargetPosition = Target.position,
                Parent = Parent,
            });
        }
    }
}
