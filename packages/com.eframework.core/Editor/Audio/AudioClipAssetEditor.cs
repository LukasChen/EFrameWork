#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using EFramework.Runtime.Audio;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace EFramework.Editor.Audio
{
    [CustomEditor(typeof(AudioClipAsset))]
    public class AudioClipAssetEditor : UnityEditor.Editor
    {
        private Queue<AudioSource> m_audioSourceCache;
        private GameObject m_audioSourceGameObject;
        private CancellationTokenSource m_cancellationTokenSource;
        private bool m_multiPlayPreview;

        private void OnEnable()
        {
            m_multiPlayPreview = false;

            m_audioSourceGameObject = new GameObject("EditorAudioSource");
            m_audioSourceGameObject.hideFlags = HideFlags.HideInHierarchy;

            m_audioSourceCache = new Queue<AudioSource>();
            AudioSource audioSource = m_audioSourceGameObject.AddComponent<AudioSource>();
            m_audioSourceCache.Enqueue(audioSource);
        }

        private void OnDisable()
        {
            if (m_multiPlayPreview)
            {
                m_multiPlayPreview = false;
                m_cancellationTokenSource?.Cancel();
            }

            // Clean up the temporary GameObject
            if (m_audioSourceGameObject != null) DestroyImmediate(m_audioSourceGameObject);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AudioClipAsset audioClipAsset = (AudioClipAsset)target;

            if (GUILayout.Button("Preview Single Clip", GUILayout.Height(30))) PlayAudioAsset(audioClipAsset);

            if (GUILayout.Button(m_multiPlayPreview ? "Stop " : "Preview Multiplay Clip", GUILayout.Height(30)))
            {
                m_multiPlayPreview = !m_multiPlayPreview;
                if (m_multiPlayPreview)
                {
                    m_cancellationTokenSource = new CancellationTokenSource();
                    PlayAudioClipLoop(audioClipAsset, m_cancellationTokenSource.Token).Forget();
                }
                else
                {
                    m_cancellationTokenSource?.Cancel();
                }
            }
        }

        private async UniTask PlayAudioClipLoop(AudioClipAsset audioClipAsset, CancellationToken cancellationToken)
        {
            while (m_multiPlayPreview)
            {
                PlayAudioAsset(audioClipAsset);
                await UniTask.Delay((int)(audioClipAsset.MinIntervalTime * 1000), cancellationToken: cancellationToken);
            }
        }

        private void PlayAudioAsset(AudioClipAsset audioClipAsset)
        {
            if (m_audioSourceCache != null)
            {
                var audioClipRef = audioClipAsset.GetClipReference();
                if (audioClipRef == null || audioClipRef.editorAsset == null) return;

                AudioSource audioSource = m_audioSourceCache.Peek();

                if (audioSource.isPlaying && m_audioSourceCache.Count < AudioManager.K_maxAudioSourceCount)
                {
                    audioSource = m_audioSourceGameObject.AddComponent<AudioSource>();
                    m_audioSourceCache.Enqueue(audioSource);
                }
                else
                {
                    audioSource.Stop();
                    m_audioSourceCache.Enqueue(m_audioSourceCache.Dequeue());
                }

                audioSource.clip = audioClipRef.editorAsset;
                audioSource.volume = audioClipAsset.Volume.RandomValue;
                audioSource.pitch = audioClipAsset.Pitch.RandomValue;
                audioSource.Play();
            }
        }
    }
}
#endif
