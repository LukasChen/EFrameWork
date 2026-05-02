using System;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Audio;
using EFramework.Runtime.DataStorage;
using EFramework.Runtime.Event;
using EFramework.Runtime.UI;
using UnityEngine;

namespace EFramework.Runtime
{
    public sealed class EFrameContext : IDisposable
    {
        private bool m_disposed;

        public EFrameContext(
            EFrameComponent component,
            IAssetService assets,
            IUIService ui,
            IAudioService audio,
            IDataService data,
            IEventService events,
            ICoroutineService coroutine)
        {
            Component = component;
            Assets = assets;
            UI = ui;
            Audio = audio;
            Data = data;
            Events = events;
            Coroutine = coroutine;
        }

        public EFrameComponent Component { get; }
        public IAssetService Assets { get; }
        public IUIService UI { get; }
        public IAudioService Audio { get; }
        public IDataService Data { get; }
        public IEventService Events { get; }
        public ICoroutineService Coroutine { get; }

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

            Audio?.Dispose();
            UI?.Dispose();
            Assets?.Dispose();
            Data?.Dispose();
            Coroutine?.Dispose();
            Events?.ClearAll();
        }
    }
}
