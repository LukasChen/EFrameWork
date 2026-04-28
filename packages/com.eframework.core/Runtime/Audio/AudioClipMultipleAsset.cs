using EFrameWork.Runtime.Base.Attributes;
using EFrameWork.Runtime.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EFrameWork.Runtime.Audio
{
    [CreateAssetMenu(fileName = "AudioClipMultipleAsset", menuName = "EFrame/AudioClipMultiple Asset")]
    public class AudioClipMultipleAsset : ScriptableObject
    {
        public AssetReferenceT<AudioClip>[] AudioClipRef;
        [MinMaxRange(0f, 1f)] public RangeFloat Volume = new(1f, 1f);
        [MinMaxRange(0.5f, 1.5f)] public RangeFloat Pitch = new(1f, 1f);
        [Range(0.03f, 1f)] public float MinIntervalTime = 0.05f;

        public AudioClip LoadClipAsync()
        {
            if (AudioClipRef == null) return null;

            var audioClip = AudioClipRef.GetRandomElement();

            // 检查是否已经加载
            if (audioClip.OperationHandle.IsValid() && audioClip.OperationHandle.Status == AsyncOperationStatus.Succeeded)
                return audioClip.OperationHandle.Result as AudioClip;

            // 加载资源
            var audioClipHandle = audioClip.LoadAssetAsync<AudioClip>();
            audioClipHandle.WaitForCompletion();
            return audioClipHandle.Result;
        }
    }
}
