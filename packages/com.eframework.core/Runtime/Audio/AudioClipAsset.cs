using EFrameWork.Runtime.Base.Attributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace EFrameWork.Runtime.Audio
{
    [CreateAssetMenu(fileName = "AudioClipAsset", menuName = "EFrame/AudioClip Asset")]
    public class AudioClipAsset : ScriptableObject
    {
        [FormerlySerializedAs("AudioClipRef")]
        [SerializeField] public AssetReferenceT<AudioClip> AudioClip;

        [MinMaxRange(0f, 1f)] public RangeFloat Volume = new(1f, 1f);
        [MinMaxRange(0.5f, 1.5f)] public RangeFloat Pitch = new(1f, 1f);
        [Range(0.03f, 2f)] public float MinIntervalTime = 0.05f;

        public AudioClip LoadClipAsync()
        {
            if (AudioClip == null) return null;

            // 检查是否已经加载
            if (AudioClip.OperationHandle.IsValid() && AudioClip.OperationHandle.Status == AsyncOperationStatus.Succeeded)
                return AudioClip.OperationHandle.Result as AudioClip;

            // 加载资源
            var audioClip = AudioClip.LoadAssetAsync<AudioClip>();
            audioClip.WaitForCompletion();
            return audioClip.Result;
        }
    }
}
