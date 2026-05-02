using EFramework.Runtime;
using UnityEngine;

namespace EFramework.Runtime.Audio
{
    public class AudioAssetComponent : MonoBehaviour
    {
        public void PlayAudioAsset(AudioClip audioClip)
        {
            EFrame.Current?.Audio?.PlaySfx(audioClip);
        }
    }
}
