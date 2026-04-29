using Cysharp.Threading.Tasks;
using EFrameWork.Runtime.Base.Attributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EFrameWork.Runtime.Audio
{
    [CreateAssetMenu(fileName = "AudioClipAsset", menuName = "EFrame/AudioClip Asset")]
    public class AudioClipAsset : ScriptableObject
    {
        public AssetReferenceT<AudioClip>[] AudioClips;

        [MinMaxRange(0f, 1f)] public RangeFloat Volume = new(1f, 1f);
        [MinMaxRange(0.5f, 1.5f)] public RangeFloat Pitch = new(1f, 1f);
        [Range(0.03f, 2f)] public float MinIntervalTime = 0.05f;

        public AudioClip LoadClip()
        {
            var audioClipRef = GetClipReference();
            if (audioClipRef == null) return null;

            if (audioClipRef.OperationHandle.IsValid())
            {
                var existingHandle = audioClipRef.OperationHandle;
                existingHandle.WaitForCompletion();
                return existingHandle.Status == AsyncOperationStatus.Succeeded
                    ? existingHandle.Result as AudioClip
                    : null;
            }

            var audioClip = audioClipRef.LoadAssetAsync<AudioClip>();
            audioClip.WaitForCompletion();
            return audioClip.Status == AsyncOperationStatus.Succeeded ? audioClip.Result : null;
        }

        public async UniTask<AudioClip> LoadClipAsync()
        {
            var audioClipRef = GetClipReference();
            if (audioClipRef == null) return null;

            if (audioClipRef.OperationHandle.IsValid())
            {
                var existingHandle = audioClipRef.OperationHandle;
                while (!existingHandle.IsDone)
                {
                    await UniTask.Yield();
                }

                return existingHandle.Status == AsyncOperationStatus.Succeeded
                    ? existingHandle.Result as AudioClip
                    : null;
            }

            var audioClip = audioClipRef.LoadAssetAsync<AudioClip>();
            while (!audioClip.IsDone)
            {
                await UniTask.Yield();
            }

            return audioClip.Status == AsyncOperationStatus.Succeeded ? audioClip.Result : null;
        }

        public void ReleaseLoadedClips()
        {
            if (AudioClips == null) return;

            foreach (var audioClipRef in AudioClips)
            {
                if (audioClipRef == null || !audioClipRef.OperationHandle.IsValid()) continue;
                audioClipRef.ReleaseAsset();
            }
        }

        public AssetReferenceT<AudioClip> GetClipReference()
        {
            if (AudioClips == null || AudioClips.Length == 0) return null;

            int clipIndex = AudioClips.Length == 1 ? 0 : Random.Range(0, AudioClips.Length);
            return AudioClips[clipIndex];
        }
    }
}
