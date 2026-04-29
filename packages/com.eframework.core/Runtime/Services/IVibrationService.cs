using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lofelt.NiceVibrations;

namespace EFrameWork.Runtime.Vibration
{
    public interface IVibrationService
    {
        bool IsOn { get; set; }
        bool IsDeviceSupport();
        void CommonVibration();
        void PlayPreset(HapticPatterns.PresetType hapticType);
        void PlayConstant(float amplitude, float frequency, float duration);
        void PlayEmphasis(float amplitude, float frequency);
        void ButtonClick();
        void ToggleSwitch();
        void SliderTick();
        void PopupAppear();
        void StopContinuous();
        UniTaskVoid ShuffleCards(CancellationToken cancellationToken = default);
        UniTaskVoid Victory(CancellationToken cancellationToken = default);
        UniTaskVoid BigWin(CancellationToken cancellationToken = default);
    }
}
