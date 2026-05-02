using Cysharp.Threading.Tasks;
using EFrame.Runtime.Base.Attributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EFrame.Runtime.Audio
{
    [CreateAssetMenu(fileName = "AudioClipAsset", menuName = "EFrame/AudioClip Asset")]
    public class AudioClipAsset : ScriptableObject
    {
        private readonly List<AudioClip> m_preloadedClips = new();

        public AssetReferenceT<AudioClip>[] AudioClips;

        [MinMaxRange(0f, 1f)] public RangeFloat Volume = new(1f, 1f);
        [MinMaxRange(0.5f, 1.5f)] public RangeFloat Pitch = new(1f, 1f);
        [Range(0.03f, 2f)] public float MinIntervalTime = 0.05f;

        public bool IsPreloaded
        {
            get
            {
                RefreshPreloadedClipCache();
                int validClipCount = GetValidClipReferenceCount();
                return validClipCount > 0 && GetLoadedClipReferenceCount() == validClipCount;
            }
        }

        public async UniTask<bool> PreloadAsync()
        {
            m_preloadedClips.Clear();

            if (AudioClips == null || AudioClips.Length == 0)
            {
                return false;
            }

            foreach (var audioClipRef in AudioClips)
            {
                if (!IsValidClipReference(audioClipRef))
                {
                    continue;
                }

                var clip = await LoadClipReferenceAsync(audioClipRef);
                if (clip != null && !m_preloadedClips.Contains(clip))
                {
                    m_preloadedClips.Add(clip);
                }
            }

            return IsPreloaded;
        }

        public bool TryGetPreloadedClip(out AudioClip audioClip)
        {
            RefreshPreloadedClipCache();

            if (m_preloadedClips.Count == 0)
            {
                audioClip = null;
                return false;
            }

            int clipIndex = m_preloadedClips.Count == 1 ? 0 : Random.Range(0, m_preloadedClips.Count);
            audioClip = m_preloadedClips[clipIndex];
            return audioClip != null;
        }

        public AudioClip GetPreloadedClip()
        {
            return TryGetPreloadedClip(out var audioClip) ? audioClip : null;
        }

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

            return await LoadClipReferenceAsync(audioClipRef);
        }

        public void ReleaseLoadedClips()
        {
            if (AudioClips == null) return;

            foreach (var audioClipRef in AudioClips)
            {
                if (!IsValidClipReference(audioClipRef) || !audioClipRef.OperationHandle.IsValid()) continue;
                audioClipRef.ReleaseAsset();
            }

            m_preloadedClips.Clear();
        }

        public AssetReferenceT<AudioClip> GetClipReference()
        {
            if (AudioClips == null || AudioClips.Length == 0) return null;

            int validClipCount = GetValidClipReferenceCount();
            if (validClipCount == 0) return null;

            int targetIndex = validClipCount == 1 ? 0 : Random.Range(0, validClipCount);
            int currentIndex = 0;

            foreach (var audioClipRef in AudioClips)
            {
                if (!IsValidClipReference(audioClipRef))
                {
                    continue;
                }

                if (currentIndex == targetIndex)
                {
                    return audioClipRef;
                }

                currentIndex++;
            }

            return null;
        }

        private async UniTask<AudioClip> LoadClipReferenceAsync(AssetReferenceT<AudioClip> audioClipRef)
        {
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

        private void RefreshPreloadedClipCache()
        {
            m_preloadedClips.RemoveAll(clip => clip == null);

            if (AudioClips == null) return;

            foreach (var audioClipRef in AudioClips)
            {
                if (TryGetLoadedClip(audioClipRef, out var clip) && !m_preloadedClips.Contains(clip))
                {
                    m_preloadedClips.Add(clip);
                }
            }
        }

        private int GetValidClipReferenceCount()
        {
            if (AudioClips == null) return 0;

            int count = 0;
            foreach (var audioClipRef in AudioClips)
            {
                if (IsValidClipReference(audioClipRef))
                {
                    count++;
                }
            }

            return count;
        }

        private int GetLoadedClipReferenceCount()
        {
            if (AudioClips == null) return 0;

            int count = 0;
            foreach (var audioClipRef in AudioClips)
            {
                if (TryGetLoadedClip(audioClipRef, out _))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryGetLoadedClip(AssetReferenceT<AudioClip> audioClipRef, out AudioClip clip)
        {
            clip = null;

            if (audioClipRef == null || !audioClipRef.OperationHandle.IsValid())
            {
                return false;
            }

            var handle = audioClipRef.OperationHandle;
            if (!handle.IsDone || handle.Status != AsyncOperationStatus.Succeeded)
            {
                return false;
            }

            clip = handle.Result as AudioClip;
            return clip != null;
        }

        private static bool IsValidClipReference(AssetReferenceT<AudioClip> audioClipRef)
        {
            return audioClipRef != null && audioClipRef.RuntimeKeyIsValid();
        }
    }
}
