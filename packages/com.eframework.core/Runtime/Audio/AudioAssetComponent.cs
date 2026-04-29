using EFrameWork.Runtime;
using UnityEngine;

namespace EFrameWork.Runtime.Audio
{
    public class AudioAssetComponent : MonoBehaviour
    {
        public void PlayAudioAsset(AudioClip audioClip)
        {
            EFrame.Current?.Audio?.PlaySfx(audioClip);
        }
    }
}
