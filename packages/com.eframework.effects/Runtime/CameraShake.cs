using EFramework.Runtime.Tween;
using UnityEngine;

namespace EFramework.Extensions.Effects
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
            if (camera == null || power <= 0)
            {
                return;
            }

            CameraShakeParam param = Presets[Mathf.Clamp(power, 0, Presets.Length - 1)];
            if (param.Duration <= 0f || param.Strength <= 0f)
            {
                return;
            }

            var cameraTransform = camera.transform;
            EFrameTween.Kill(cameraTransform, true);

            var origin = cameraTransform.position;
            var offsets = CreateOffsets(param);
            EFrameTween.Float(0f, 1f, param.Duration, progress =>
            {
                var samplePosition = progress * (offsets.Length - 1);
                var sampleIndex = Mathf.Min(Mathf.FloorToInt(samplePosition), offsets.Length - 2);
                var sampleProgress = samplePosition - sampleIndex;
                var damping = 1f - EFrameEaseUtility.Evaluate(EFrameEase.OutQuad, progress);
                var offset = Vector3.LerpUnclamped(offsets[sampleIndex], offsets[sampleIndex + 1], sampleProgress) * damping;
                cameraTransform.position = origin + offset;
            }, new EFrameTweenOptions
            {
                Target = cameraTransform,
                Ease = EFrameEase.Linear,
                OnComplete = () => cameraTransform.position = origin
            });
        }

        private static Vector3[] CreateOffsets(CameraShakeParam param)
        {
            var count = Mathf.Max(2, param.Vibrato + 1);
            var offsets = new Vector3[count];
            for (var index = 0; index < count - 1; index++)
            {
                offsets[index] = Random.insideUnitSphere * param.Strength;
            }

            offsets[count - 1] = Vector3.zero;
            return offsets;
        }
    }
}
