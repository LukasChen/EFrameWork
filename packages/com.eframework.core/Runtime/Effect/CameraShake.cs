using DG.Tweening;
using EFrameWork.Runtime;
using EFrameWork.Runtime.UI;
using UnityEngine;

namespace EFrameWork.Runtime.Effect
{
    public static class CameraShakeFx
    {
        public struct CameraShakeParam
        {
            public float Duration;
            public float Strength;
            public int Vibrato;
        }

        public static CameraShakeParam[] Presets =
        {
            new CameraShakeParam
            {
                Duration = 0,
                Strength = 0,
                Vibrato = 0
            },
            new CameraShakeParam
            {
                Duration = 0.2f,
                Strength = 0.08f,
                Vibrato = 30
            },
            new CameraShakeParam
            {
                Duration = 0.3f,
                Strength = 0.1f,
                Vibrato = 25
            },
            new CameraShakeParam
            {
                Duration = 0.45f,
                Strength = 0.15f,
                Vibrato = 20
            },
            new CameraShakeParam
            {
                Duration = 0.3f,
                Strength = 0.2f,
                Vibrato = 20
            }
        };
        public static void Play(Camera camera, int power)
        {
            CameraShakeParam param = Presets[Mathf.Clamp(power, 0, Presets.Length)];
            if (power == 0) return;
            Vector3 pos = camera.transform.position;
            Vector3 rot = camera.transform.rotation.eulerAngles;
            camera.DOShakePosition(param.Duration, param.Strength, param.Vibrato, 45).SetEase(Ease.OutQuad).OnComplete(() => { camera.transform.position = pos; });
        }
    }
}