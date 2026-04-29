using Lofelt.NiceVibrations;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EFrameWork.Runtime.Vibration
{
    /// <summary>
    /// 震动管理器，提供多种内置震动方案用于不同游戏场景
    /// </summary>
    public class QVibration : IVibrationService
    {
        public bool IsOn { get; set; }

        public QVibration(bool isOn = true)
        {
            IsOn = isOn;
        }

        public bool IsDeviceSupport()
        {
            return DeviceCapabilities.isVersionSupported;
        }

        #region 基础API

        /// <summary>
        /// 通用震动 - 轻微选择反馈
        /// </summary>
        public void CommonVibration()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            PlayPreset(HapticPatterns.PresetType.Selection);
        }

        public void PlayPreset(HapticPatterns.PresetType hapticType)
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(hapticType);
        }

        public void PlayConstant(float amplitude, float frequency, float duration)
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayConstant(amplitude, frequency, duration);
        }

        public void PlayEmphasis(float amplitude, float frequency)
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayEmphasis(amplitude, frequency);
        }

        #endregion

        #region UI交互震动

        /// <summary>
        /// 按钮点击 - 轻微、清脆的触感反馈
        /// </summary>
        public void ButtonClick()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
        }

        /// <summary>
        /// 开关切换 - 明确的状态切换反馈
        /// </summary>
        public void ToggleSwitch()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
        }

        /// <summary>
        /// 滑动选择 - 轻微滑动反馈，适用于滑动选择器
        /// </summary>
        public void SliderTick()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayEmphasis(0.2f, 0.3f);
        }

        /// <summary>
        /// 弹窗出现 - 提示用户注意
        /// </summary>
        public void PopupAppear()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
        }

        #endregion

        #region 游戏操作震动

        /// <summary>
        /// 出牌 - 清脆的卡牌放下感觉
        /// </summary>
        public void PlayCard()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayEmphasis(0.5f, 0.6f);
        }

        /// <summary>
        /// 翻牌 - 卡牌翻转的触感
        /// </summary>
        public void FlipCard()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.RigidImpact);
        }

        /// <summary>
        /// 洗牌 - 连续轻微的震动模拟洗牌感觉
        /// </summary>
        public async UniTaskVoid ShuffleCards(CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            for (int i = 0; i < 6; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                HapticPatterns.PlayEmphasis(0.25f + i * 0.05f, 0.4f);
                await UniTask.Delay(60, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// 发牌 - 每张牌发出时的反馈
        /// </summary>
        public void DealCard()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayEmphasis(0.35f, 0.5f);
        }

        /// <summary>
        /// 筹码移动 - 筹码堆叠或滑动的感觉
        /// </summary>
        public void ChipMove()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayEmphasis(0.3f, 0.4f);
        }

        /// <summary>
        /// 下注 - 筹码投入的确认感
        /// </summary>
        public void PlaceBet()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
        }

        /// <summary>
        /// 加注 - 比普通下注更强的反馈
        /// </summary>
        public void RaiseBet()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
        }

        /// <summary>
        /// 弃牌 - 轻柔的放弃感
        /// </summary>
        public void FoldHand()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.SoftImpact);
        }

        #endregion

        #region 游戏结果震动

        /// <summary>
        /// 胜利 - 欢快递进的震动庆祝
        /// </summary>
        public async UniTaskVoid Victory(CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            // 三段递进的欢快震动
            HapticPatterns.PlayEmphasis(0.4f, 0.5f);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayEmphasis(0.6f, 0.6f);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
        }

        /// <summary>
        /// 大胜 - 更强烈的胜利庆祝
        /// </summary>
        public async UniTaskVoid BigWin(CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            // 强烈的递进震动
            for (int i = 0; i < 3; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
                await UniTask.Delay(120, cancellationToken: cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested) return;
            await UniTask.Delay(50, cancellationToken: cancellationToken);
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
        }

        /// <summary>
        /// 失败 - 沉闷的失败反馈
        /// </summary>
        public void Defeat()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Failure);
        }

        /// <summary>
        /// 平局 - 中性的结果反馈
        /// </summary>
        public void Draw()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
        }

        /// <summary>
        /// 获得奖励 - 收到奖励时的愉悦反馈
        /// </summary>
        public async UniTaskVoid RewardReceived(CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            // 两段轻快的反馈
            HapticPatterns.PlayEmphasis(0.5f, 0.7f);
            await UniTask.Delay(80, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
        }

        /// <summary>
        /// 连续收益 - 金币/筹码持续增加的反馈
        /// </summary>
        public async UniTaskVoid ContinuousGain(int count, CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            count = System.Math.Min(count, 10); // 限制最大次数
            for (int i = 0; i < count; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                HapticPatterns.PlayEmphasis(0.3f, 0.5f + i * 0.03f);
                await UniTask.Delay(50, cancellationToken: cancellationToken);
            }
        }

        #endregion

        #region 特殊事件震动

        /// <summary>
        /// 警告提示 - 需要用户注意的警告
        /// </summary>
        public void Warning()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
        }

        /// <summary>
        /// 错误提示 - 操作错误的反馈
        /// </summary>
        public void Error()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Failure);
        }

        /// <summary>
        /// 倒计时提醒 - 时间紧迫的提示
        /// </summary>
        public async UniTaskVoid CountdownAlert(int beats = 3, CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            for (int i = 0; i < beats; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                float intensity = 0.5f + (i * 0.15f); // 递增强度
                HapticPatterns.PlayEmphasis(intensity, 0.5f);
                await UniTask.Delay(300, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// 紧急提醒 - 快速连续的警告震动
        /// </summary>
        public async UniTaskVoid UrgentAlert(CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            for (int i = 0; i < 4; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
                await UniTask.Delay(150, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// 解锁成就 - 成就解锁的特殊反馈
        /// </summary>
        public async UniTaskVoid AchievementUnlock(CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            // 独特的解锁节奏
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.RigidImpact);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayEmphasis(0.7f, 0.8f);
            await UniTask.Delay(150, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
        }

        /// <summary>
        /// 升级 - 等级提升的庆祝震动
        /// </summary>
        public async UniTaskVoid LevelUp(CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            // 三段递进震动
            HapticPatterns.PlayEmphasis(0.4f, 0.4f);
            await UniTask.Delay(80, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayEmphasis(0.6f, 0.6f);
            await UniTask.Delay(80, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayEmphasis(0.8f, 0.8f);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
        }

        /// <summary>
        /// 特殊牌型 - 如同花顺、四条等特殊牌型出现
        /// </summary>
        public async UniTaskVoid SpecialHand(CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            // 强调性的特殊反馈
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayEmphasis(0.8f, 0.9f);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
        }

        #endregion

        #region 社交互动震动

        /// <summary>
        /// 收到消息 - 新消息提醒
        /// </summary>
        public void MessageReceived()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayEmphasis(0.4f, 0.6f);
        }

        /// <summary>
        /// 好友上线 - 好友状态变化提醒
        /// </summary>
        public void FriendOnline()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
        }

        /// <summary>
        /// 被邀请 - 收到游戏邀请
        /// </summary>
        public async UniTaskVoid InviteReceived(CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            HapticPatterns.PlayEmphasis(0.5f, 0.5f);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
        }

        /// <summary>
        /// 表情发送 - 发送表情/礼物的反馈
        /// </summary>
        public void SendEmoji()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayEmphasis(0.35f, 0.5f);
        }

        #endregion

        #region 持续震动

        /// <summary>
        /// 持续轻震 - 用于长按或拖拽操作
        /// </summary>
        public void LightContinuous(float duration)
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayConstant(0.3f, 0.3f, duration);
        }

        /// <summary>
        /// 持续中震 - 中等强度的持续震动
        /// </summary>
        public void MediumContinuous(float duration)
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayConstant(0.5f, 0.5f, duration);
        }

        /// <summary>
        /// 持续强震 - 强烈的持续震动
        /// </summary>
        public void HeavyContinuous(float duration)
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayConstant(0.8f, 0.7f, duration);
        }

        /// <summary>
        /// 停止持续震动
        /// </summary>
        public void StopContinuous()
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticController.Stop();
        }

        #endregion

        #region 自定义震动

        /// <summary>
        /// 自定义强调震动
        /// </summary>
        /// <param name="amplitude">振幅 0.0-1.0</param>
        /// <param name="frequency">频率 0.0-1.0</param>
        public void CustomEmphasis(float amplitude, float frequency)
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayEmphasis(amplitude, frequency);
        }

        /// <summary>
        /// 自定义持续震动
        /// </summary>
        /// <param name="amplitude">振幅 0.0-1.0</param>
        /// <param name="frequency">频率 0.0-1.0</param>
        /// <param name="duration">持续时间（秒）</param>
        public void CustomConstant(float amplitude, float frequency, float duration)
        {
            if (!IsOn || !IsDeviceSupport()) return;
            HapticPatterns.PlayConstant(amplitude, frequency, duration);
        }

        /// <summary>
        /// 自定义脉冲震动 - 可配置的脉冲序列
        /// </summary>
        /// <param name="pulseCount">脉冲次数</param>
        /// <param name="amplitude">振幅 0.0-1.0</param>
        /// <param name="intervalMs">脉冲间隔（毫秒）</param>
        public async UniTaskVoid CustomPulse(int pulseCount, float amplitude, int intervalMs, CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            pulseCount = System.Math.Min(pulseCount, 15); // 安全限制
            for (int i = 0; i < pulseCount; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                HapticPatterns.PlayEmphasis(amplitude, 0.5f);
                await UniTask.Delay(intervalMs, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// 渐强震动 - 从轻到重递增
        /// </summary>
        public async UniTaskVoid Crescendo(float duration, CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            int steps = (int)(duration * 10);
            steps = System.Math.Min(steps, 20);

            for (int i = 0; i < steps; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                float amplitude = (float)(i + 1) / steps;
                HapticPatterns.PlayEmphasis(amplitude, 0.5f);
                await UniTask.Delay((int)(duration * 1000 / steps), cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// 渐弱震动 - 从重到轻递减
        /// </summary>
        public async UniTaskVoid Decrescendo(float duration, CancellationToken cancellationToken = default)
        {
            if (!IsOn || !IsDeviceSupport()) return;

            int steps = (int)(duration * 10);
            steps = System.Math.Min(steps, 20);

            for (int i = steps; i > 0; i--)
            {
                if (cancellationToken.IsCancellationRequested) break;
                float amplitude = (float)i / steps;
                HapticPatterns.PlayEmphasis(amplitude, 0.5f);
                await UniTask.Delay((int)(duration * 1000 / steps), cancellationToken: cancellationToken);
            }
        }

        #endregion
    }
}
