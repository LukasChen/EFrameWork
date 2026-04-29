#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using EFrameWork.Runtime.Audio;
using EFrameWork.Runtime.Utils;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace EFrameWork.Editor.Audio
{
    [CustomEditor(typeof(AudioClipMultipleAsset))]
    public class AudioClipMultipleAssetEditor : UnityEditor.Editor
    {
        private GameObject audioSourceGameObject;
        private CancellationTokenSource cancellationTokenSource;
        private Queue<AudioSource> m_AudioSourceCache;
        private bool m_MultiPlayPreview;

        private void OnEnable()
        {
            m_MultiPlayPreview = false;

            audioSourceGameObject = new GameObject("EditorAudioSource");
            audioSourceGameObject.hideFlags = HideFlags.HideInHierarchy;

            m_AudioSourceCache = new Queue<AudioSource>();
            AudioSource audioSource = audioSourceGameObject.AddComponent<AudioSource>();
            m_AudioSourceCache.Enqueue(audioSource);
        }

        private void OnDisable()
        {
            if (m_MultiPlayPreview)
            {
                m_MultiPlayPreview = false;
                cancellationTokenSource?.Cancel();
            }

            // Clean up the temporary GameObject
            if (audioSourceGameObject != null) DestroyImmediate(audioSourceGameObject);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AudioClipMultipleAsset audioClipAsset = (AudioClipMultipleAsset)target;

            if (GUILayout.Button("Preview Single Clip", GUILayout.Height(30))) PlayAudioAsset(audioClipAsset);

            if (GUILayout.Button(m_MultiPlayPreview ? "Stop " : "Preview Multiplay Clip", GUILayout.Height(30)))
            {
                m_MultiPlayPreview = !m_MultiPlayPreview;
                if (m_MultiPlayPreview)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    PlayAudioClipLoop(audioClipAsset, cancellationTokenSource.Token).Forget();
                }
                else
                {
                    cancellationTokenSource?.Cancel();
                }
            }
        }

        private async UniTask PlayAudioClipLoop(AudioClipMultipleAsset audioClipAsset, CancellationToken cancellationToken)
        {
            while (m_MultiPlayPreview)
            {
                PlayAudioAsset(audioClipAsset);
                await UniTask.Delay((int)(audioClipAsset.MinIntervalTime * 1000), cancellationToken: cancellationToken);
            }
        }

        private void PlayAudioAsset(AudioClipMultipleAsset audioClipAsset)
        {
            if (m_AudioSourceCache != null)
            {
                AudioSource audioSource = m_AudioSourceCache.Peek();

                if (audioSource.isPlaying && m_AudioSourceCache.Count < AudioManager.K_maxAudioSourceCount)
                {
                    audioSource = audioSourceGameObject.AddComponent<AudioSource>();
                    m_AudioSourceCache.Enqueue(audioSource);
                }
                else
                {
                    audioSource.Stop();
                    m_AudioSourceCache.Enqueue(m_AudioSourceCache.Dequeue());
                }

                audioSource.clip = audioClipAsset.AudioClipRef.GetRandomElement().editorAsset;
                audioSource.volume = audioClipAsset.Volume.RandomValue;
                audioSource.pitch = audioClipAsset.Pitch.RandomValue;
                audioSource.Play();
            }
        }
    }
}
#endif
