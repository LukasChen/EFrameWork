using System;
using EFrameWork.Runtime.Asset;
using EFrameWork.Runtime.Audio;
using EFrameWork.Runtime.DataStorage;
using EFrameWork.Runtime.Event;
using EFrameWork.Runtime.UI;
using EFrameWork.Runtime.Vibration;
using UnityEngine;

namespace EFrameWork.Runtime
{
    public sealed class EFrameContext : IDisposable
    {
        private bool m_disposed;

        public EFrameContext(
            EFrameComponent component,
            IAssetService assets,
            IUIService ui,
            IAudioService audio,
            IAudioEventService audioEvents,
            IDataService data,
            IEventService events,
            ICoroutineService coroutine,
            IVibrationService vibration)
        {
            Component = component;
            Assets = assets;
            UI = ui;
            Audio = audio;
            AudioEvents = audioEvents;
            Data = data;
            Events = events;
            Coroutine = coroutine;
            Vibration = vibration;
        }

        public EFrameComponent Component { get; }
        public IAssetService Assets { get; }
        public IUIService UI { get; }
        public IAudioService Audio { get; }
        public IAudioEventService AudioEvents { get; }
        public IDataService Data { get; }
        public IEventService Events { get; }
        public ICoroutineService Coroutine { get; }
        public IVibrationService Vibration { get; }

        public Camera SceneCamera => Component != null ? Component.SceneCamera : null;
        public Camera UICamera => Component != null ? Component.UICamera : null;
        public float FPS { get; internal set; }

        public void InjectInto(GameObject gameObject)
        {
            if (gameObject == null) return;

            var behaviours = gameObject.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IEFrameContextAware aware)
                {
                    aware.BindContext(this);
                }
            }
        }

        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            AudioEvents?.Dispose();
            Audio?.Dispose();
            UI?.Dispose();
            Assets?.Dispose();
            Data?.Dispose();
            Coroutine?.Dispose();
            Events?.ClearAll();
        }
    }
}
