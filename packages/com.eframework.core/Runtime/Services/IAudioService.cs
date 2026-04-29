using System;
using UnityEngine;

namespace EFrameWork.Runtime.Audio
{
    public interface IAudioService : IDisposable
    {
        bool SoundOn { get; set; }
        bool MusicOn { get; set; }
        void Initialize(bool musicOn = true, bool soundOn = true);
        void PlayAudioAsset(string assetPath);
        void PlayAudioClipAsset(AudioClipAsset audioClipAsset);
        void PlayAudioClipMultipleAsset(AudioClipMultipleAsset audioClipAsset);
        void PlaySfx(string assetPath, float volume = 1f, float pitch = 1f);
        void PlaySfx(AudioClip audioClip, float volume = 1f, float pitch = 1f);
        void PlaySfx(AudioClip audioClip, float minInterval, float volume, float pitch);
        void PlayMusic(string assetPath, bool fade = true, float fadeDuration = AudioManager.K_fadeDuration);
        void PlayMusic(AudioClip audio, bool loop = true, bool fade = true, float fadeDuration = AudioManager.K_fadeDuration);
        void StopMusic();
    }

    public interface IAudioEventService : IDisposable
    {
        bool IsInitialized { get; }
        void Initialize();
    }
}
