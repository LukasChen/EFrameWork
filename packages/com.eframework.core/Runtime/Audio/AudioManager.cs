using DG.Tweening;
using EFrameWork.Runtime.Asset;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace EFrameWork.Runtime.Audio
{
    /// <summary>
    /// 音频管理器
    /// - SFX: 统一音频池(上限30)，支持防抖避免短音频连续播放爆音
    /// - Music: 独立2个AudioSource用于crossfade
    /// </summary>
    public class AudioManager
    {
        public const string K_musicGroup = "MUSIC";
        public const string K_sfxGroup = "SFX";
        public const float K_fadeDuration = 3f;
        public const int K_maxAudioSourceCount = 30;
        public const int K_initialAudioSourceCount = 8;
        public const float K_defaultMinInterval = 0.05f;

        private AudioMixer m_audioMixer;
        private GameObject m_audioSourceHolder;
        private AudioMixerGroup m_musicGroup;
        private AudioMixerGroup m_sfxGroup;
        private bool m_musicOn = true;

        // Music: 独立的2个AudioSource用于crossfade
        private Queue<AudioSource> m_musicAudioSourceCache;
        private AudioSource m_currentMusicSource;
        private AudioSource m_nextMusicSource;

        // SFX: 统一音频池
        private List<AudioSource> m_audioSourcePool;
        private Dictionary<AudioClip, float> m_lastPlayTimeByClip;

        public AudioManager(EFrameComponent baseComponent)
        {
            m_audioSourceHolder = new GameObject("AudioSourceHolder");
            m_audioSourceHolder.transform.SetParent(baseComponent.transform);
            InitAudioCache();
        }

        public bool SoundOn { get; set; }

        public bool MusicOn
        {
            get => m_musicOn;
            set
            {
                if (m_musicOn == value) return;
                m_musicOn = value;
                if (m_musicOn) ResumeMusic();
                else StopMusic();
            }
        }

        public void Initialize(bool musicOn = true, bool soundOn = true)
        {
            SoundOn = soundOn;
            m_musicOn = musicOn;
            Debug.Log($"Initialize AudioManager musicOn {musicOn}, soundOn {soundOn}");
        }

        public void Dispose()
        {
            // 停止所有音乐动画
            foreach (var audioSource in m_musicAudioSourceCache ?? Enumerable.Empty<AudioSource>())
            {
                audioSource.DOKill();
            }

            // 清理音频池
            foreach (var audioSource in m_audioSourcePool ?? Enumerable.Empty<AudioSource>())
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                    Object.Destroy(audioSource);
                }
            }

            m_musicAudioSourceCache?.Clear();
            m_audioSourcePool?.Clear();
            m_lastPlayTimeByClip?.Clear();
        }

        private void InitAudioCache()
        {
            m_audioMixer = Resources.Load<AudioMixer>("EFrameAudioMixerSettings");
            if (m_audioMixer == null)
            {
                Debug.LogWarning("EFrameAudioMixerSettings was not found under Assets/Resources. Audio will initialize without mixer routing. Run EFrame Tools/项目初始化向导 or EFrame Tools/Audio Setup to create it.");
            }
            else
            {
                var musicGroups = m_audioMixer.FindMatchingGroups(K_musicGroup);
                var sfxGroups = m_audioMixer.FindMatchingGroups(K_sfxGroup);
                m_musicGroup = musicGroups.Length > 0 ? musicGroups[0] : null;
                m_sfxGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;

                if (m_musicGroup == null || m_sfxGroup == null)
                {
                    Debug.LogWarning("EFrameAudioMixerSettings is missing MUSIC or SFX groups. Audio will initialize without complete mixer routing.");
                }
            }

            if (m_audioSourceHolder.GetComponent<AudioListener>() == null)
                m_audioSourceHolder.AddComponent<AudioListener>();

            // 初始化音乐AudioSource (2个用于crossfade)
            m_musicAudioSourceCache = new Queue<AudioSource>();
            for (int i = 0; i < 2; i++)
            {
                AudioSource musicAudioSource = m_audioSourceHolder.AddComponent<AudioSource>();
                if (m_musicGroup != null)
                    musicAudioSource.outputAudioMixerGroup = m_musicGroup;
                m_musicAudioSourceCache.Enqueue(musicAudioSource);
            }
            m_currentMusicSource = m_musicAudioSourceCache.ElementAt(0);
            m_nextMusicSource = m_musicAudioSourceCache.ElementAt(1);

            // 初始化SFX音频池
            m_audioSourcePool = new List<AudioSource>(K_maxAudioSourceCount);
            m_lastPlayTimeByClip = new Dictionary<AudioClip, float>();

            for (int i = 0; i < K_initialAudioSourceCount; i++)
            {
                AudioSource audioSource = m_audioSourceHolder.AddComponent<AudioSource>();
                if (m_sfxGroup != null)
                    audioSource.outputAudioMixerGroup = m_sfxGroup;
                m_audioSourcePool.Add(audioSource);
            }
        }

        #region SFX 播放接口

        /// <summary>
        /// 播放音频资源 (自动识别资源类型: AudioClip / AudioClipAsset / AudioClipMultipleAsset)
        /// </summary>
        public void PlayAudioAsset(string assetPath)
        {
            var audioAsset = AssetManager.LoadAsset(assetPath);
            if (audioAsset == null) return;

            if (audioAsset is AudioClip clip)
                PlaySfx(clip);
            else if (audioAsset is AudioClipAsset audioClipAsset)
                PlayAudioClipAsset(audioClipAsset);
            else if (audioAsset is AudioClipMultipleAsset audioClipMultiple)
                PlayAudioClipMultipleAsset(audioClipMultiple);
        }

        /// <summary>
        /// 播放AudioClipAsset (支持随机音量/音调/防抖间隔)
        /// </summary>
        public void PlayAudioClipAsset(AudioClipAsset audioClipAsset)
        {
            if (audioClipAsset == null) return;
            AudioClip audioClip = audioClipAsset.LoadClipAsync();
            if (audioClip == null) return;

            PlaySfxInternal(audioClip, audioClipAsset.MinIntervalTime,
                audioClipAsset.Volume.RandomValue, audioClipAsset.Pitch.RandomValue);
        }

        /// <summary>
        /// 播放AudioClipMultipleAsset (随机选择一个clip播放)
        /// </summary>
        public void PlayAudioClipMultipleAsset(AudioClipMultipleAsset audioClipAsset)
        {
            if (audioClipAsset == null) return;
            AudioClip audioClip = audioClipAsset.LoadClipAsync();
            if (audioClip == null) return;

            PlaySfxInternal(audioClip, audioClipAsset.MinIntervalTime,
                audioClipAsset.Volume.RandomValue, audioClipAsset.Pitch.RandomValue);
        }

        /// <summary>
        /// 播放SFX (通过资源路径)
        /// </summary>
        public void PlaySfx(string assetPath, float volume = 1f, float pitch = 1f)
        {
            var audioClip = AssetManager.LoadAsset<AudioClip>(assetPath);
            PlaySfx(audioClip, volume, pitch);
        }

        /// <summary>
        /// 播放SFX (通过AudioClip)
        /// </summary>
        public void PlaySfx(AudioClip audioClip, float volume = 1f, float pitch = 1f)
        {
            PlaySfxInternal(audioClip, K_defaultMinInterval, volume, pitch);
        }

        /// <summary>
        /// 播放SFX (通过AudioClip，自定义防抖间隔)
        /// </summary>
        public void PlaySfx(AudioClip audioClip, float minInterval, float volume, float pitch)
        {
            PlaySfxInternal(audioClip, minInterval, volume, pitch);
        }

        /// <summary>
        /// 内部播放逻辑：统一音频池 + 时间戳防抖
        /// </summary>
        private void PlaySfxInternal(AudioClip audioClip, float minInterval, float volume, float pitch)
        {
            if (!SoundOn || audioClip == null) return;

            // 防抖检查：同一clip在minInterval内不重复播放
            if (m_lastPlayTimeByClip.TryGetValue(audioClip, out float lastPlayTime))
            {
                if (Time.time - lastPlayTime < minInterval)
                    return;
            }
            m_lastPlayTimeByClip[audioClip] = Time.time;

            // 获取可用的AudioSource
            AudioSource audioSource = GetAvailableAudioSource();
            if (audioSource == null) return;

            audioSource.clip = audioClip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.loop = false;
            audioSource.Play();
        }

        /// <summary>
        /// 获取可用的AudioSource：优先复用空闲的，否则创建新的，池满则复用播放时间最长的
        /// </summary>
        private AudioSource GetAvailableAudioSource()
        {
            // 1. 优先找空闲的AudioSource
            foreach (var source in m_audioSourcePool)
            {
                if (!source.isPlaying)
                    return source;
            }

            // 2. 池未满，创建新的
            if (m_audioSourcePool.Count < K_maxAudioSourceCount)
            {
                AudioSource newSource = m_audioSourceHolder.AddComponent<AudioSource>();
                if (m_sfxGroup != null)
                    newSource.outputAudioMixerGroup = m_sfxGroup;
                m_audioSourcePool.Add(newSource);
                return newSource;
            }

            // 3. 池已满，复用播放时间最长的（time最大）
            AudioSource oldest = m_audioSourcePool[0];
            float maxTime = oldest.time;
            foreach (var source in m_audioSourcePool)
            {
                if (source.time > maxTime)
                {
                    maxTime = source.time;
                    oldest = source;
                }
            }
            oldest.Stop();
            return oldest;
        }

        #endregion

        #region Music 播放接口

        /// <summary>
        /// 恢复当前音乐播放
        /// </summary>
        private void ResumeMusic()
        {
            if (!m_musicOn) return;
            AudioSource musicAudioSource = m_currentMusicSource;
            if (musicAudioSource != null && musicAudioSource.clip != null)
            {
                musicAudioSource.DOKill();
                musicAudioSource.DOFade(1, K_fadeDuration).From(0);
                musicAudioSource.Play();
            }
        }

        /// <summary>
        /// 播放背景音乐 (通过资源路径)
        /// </summary>
        /// <param name="assetPath">音频资源路径</param>
        /// <param name="fade">是否使用淡入淡出</param>
        /// <param name="fadeDuration">淡入淡出时长，默认使用 K_fadeDuration</param>
        public void PlayMusic(string assetPath, bool fade = true, float fadeDuration = K_fadeDuration)
        {
            var audioClip = AssetManager.LoadAsset<AudioClip>(assetPath);
            PlayMusic(audioClip, true, fade, fadeDuration);
        }

        /// <summary>
        /// 播放背景音乐 (通过AudioClip)
        /// </summary>
        /// <param name="audio">音频Clip</param>
        /// <param name="loop">是否循环播放</param>
        /// <param name="fade">是否使用淡入淡出</param>
        /// <param name="fadeDuration">淡入淡出时长，默认使用 K_fadeDuration</param>
        public void PlayMusic(AudioClip audio, bool loop = true, bool fade = true, float fadeDuration = K_fadeDuration)
        {
            if (audio == null) return;

            if (m_currentMusicSource == null || m_nextMusicSource == null)
            {
                m_currentMusicSource = m_musicAudioSourceCache.ElementAt(0);
                m_nextMusicSource = m_musicAudioSourceCache.ElementAt(1);
            }

            // 停止当前正在播放的音乐
            if (m_currentMusicSource.isPlaying)
            {
                m_currentMusicSource.DOKill();
                if (fade)
                    m_currentMusicSource.DOFade(0, fadeDuration).OnComplete(m_currentMusicSource.Stop);
                else
                    m_currentMusicSource.Stop();
            }
            else
            {
                m_currentMusicSource.DOKill();
            }

            // 准备下一个音乐源
            m_nextMusicSource.DOKill();
            m_nextMusicSource.Stop();
            m_nextMusicSource.clip = audio;
            m_nextMusicSource.loop = loop;

            if (m_musicOn)
            {
                if (fade)
                {
                    m_nextMusicSource.volume = 0;
                    m_nextMusicSource.Play();
                    m_nextMusicSource.DOFade(1, fadeDuration);
                }
                else
                {
                    m_nextMusicSource.volume = 1;
                    m_nextMusicSource.Play();
                }
            }

            // 交换当前和下一个 source
            var temp = m_currentMusicSource;
            m_currentMusicSource = m_nextMusicSource;
            m_nextMusicSource = temp;
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopMusic()
        {
            foreach (AudioSource audioSource in m_musicAudioSourceCache)
            {
                if (audioSource.clip != null && audioSource.isPlaying)
                {
                    audioSource.DOKill();
                    audioSource.DOFade(0, K_fadeDuration).OnComplete(audioSource.Stop);
                }
            }
        }

        #endregion

        #region SFX 淡入淡出播放接口

        /// <summary>
        /// 播放 SFX 带淡入淡出曲线控制
        /// </summary>
        /// <param name="audioClip">音频剪辑</param>
        /// <param name="volume">目标音量</param>
        /// <param name="pitch">音调</param>
        /// <param name="fadeInDuration">淡入时长</param>
        /// <param name="fadeInCurve">淡入曲线</param>
        /// <param name="fadeOutDuration">淡出时长</param>
        /// <param name="fadeOutCurve">淡出曲线</param>
        /// <param name="loop">是否循环</param>
        /// <returns>用于控制的 AudioSource</returns>
        public AudioSource PlaySfxWithFade(
            AudioClip audioClip,
            float volume,
            float pitch,
            float fadeInDuration,
            AnimationCurve fadeInCurve,
            float fadeOutDuration,
            AnimationCurve fadeOutCurve,
            bool loop = false)
        {
            if (!SoundOn || audioClip == null) return null;

            AudioSource audioSource = GetAvailableAudioSource();
            if (audioSource == null) return null;

            audioSource.clip = audioClip;
            audioSource.pitch = pitch;
            audioSource.loop = loop;

            // 淡入
            if (fadeInDuration > 0 && fadeInCurve != null)
            {
                audioSource.volume = 0;
                audioSource.Play();

                // 使用 DOTween 自定义曲线淡入
                DOTween.To(
                    () => 0f,
                    t => audioSource.volume = fadeInCurve.Evaluate(t) * volume,
                    1f,
                    fadeInDuration
                ).SetEase(Ease.Linear);
            }
            else
            {
                audioSource.volume = volume;
                audioSource.Play();
            }

            // 非循环时处理淡出
            if (!loop && fadeOutDuration > 0 && fadeOutCurve != null)
            {
                float fadeOutStartTime = audioClip.length - fadeOutDuration;
                if (fadeOutStartTime > 0)
                {
                    DOVirtual.DelayedCall(fadeOutStartTime, () =>
                    {
                        if (audioSource != null && audioSource.isPlaying)
                        {
                            float startVolume = audioSource.volume;
                            DOTween.To(
                                () => 0f,
                                t => audioSource.volume = fadeOutCurve.Evaluate(t) * startVolume,
                                1f,
                                fadeOutDuration
                            ).SetEase(Ease.Linear)
                             .OnComplete(() => audioSource.Stop());
                        }
                    });
                }
            }

            return audioSource;
        }

        /// <summary>
        /// 停止指定 AudioSource 并淡出
        /// </summary>
        public void StopSfxWithFade(AudioSource audioSource, float fadeOutDuration, AnimationCurve fadeOutCurve = null)
        {
            if (audioSource == null || !audioSource.isPlaying) return;

            if (fadeOutDuration > 0)
            {
                float startVolume = audioSource.volume;
                if (fadeOutCurve != null)
                {
                    DOTween.To(
                        () => 0f,
                        t => audioSource.volume = fadeOutCurve.Evaluate(t) * startVolume,
                        1f,
                        fadeOutDuration
                    ).SetEase(Ease.Linear)
                     .OnComplete(() => audioSource.Stop());
                }
                else
                {
                    audioSource.DOFade(0, fadeOutDuration).OnComplete(() => audioSource.Stop());
                }
            }
            else
            {
                audioSource.Stop();
            }
        }

        #endregion
    }
}
