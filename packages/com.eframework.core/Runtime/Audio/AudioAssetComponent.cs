using EFrame.Runtime;
using UnityEngine;

namespace EFrame.Runtime.Audio
{
    public class AudioAssetComponent : MonoBehaviour
    {
        public void PlayAudioAsset(AudioClip audioClip)
        {
            EFrame.Current?.Audio?.PlaySfx(audioClip);
        }
    }
}
