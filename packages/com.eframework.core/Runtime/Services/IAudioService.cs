using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EFrame.Runtime.Audio
{
    public interface IAudioService : IDisposable
    {
        bool SoundOn { get; set; }
        bool MusicOn { get; set; }
        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        float SfxVolume { get; set; }
        void Initialize(bool musicOn = true, bool soundOn = true);
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
        void PlayAudioAsset(string assetPath);
        UniTask<bool> PreloadAudioClipAssetAsync(AudioClipAsset audioClipAsset);
        void ReleaseAudioClipAsset(AudioClipAsset audioClipAsset);
        void PlayAudioClipAsset(AudioClipAsset audioClipAsset);
        void PlaySfx(string assetPath, float volume = 1f, float pitch = 1f);
        void PlaySfx(AudioClip audioClip, float volume = 1f, float pitch = 1f);
        void PlaySfx(AudioClip audioClip, float minInterval, float volume, float pitch);
        AudioSource PlaySfxWithFade(AudioClip audioClip, float volume, float pitch, float fadeInDuration,
            AnimationCurve fadeInCurve, float fadeOutDuration, AnimationCurve fadeOutCurve, bool loop = false);
        void StopSfxWithFade(AudioSource audioSource, float fadeOutDuration, AnimationCurve fadeOutCurve = null);
        void PlayMusic(string assetPath, bool fade = true, float fadeDuration = AudioManager.K_fadeDuration);
        void PlayMusic(AudioClip audio, bool loop = true, bool fade = true, float fadeDuration = AudioManager.K_fadeDuration);
        void PauseMusic();
        void ResumeMusic();
        void StopMusic(bool fade = true);
        void StopAllSfx();
    }

    public interface IAudioEventService : IDisposable
    {
        bool IsInitialized { get; }
        void Initialize();
    }
}
