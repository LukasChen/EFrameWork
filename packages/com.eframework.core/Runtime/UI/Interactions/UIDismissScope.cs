using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFramework.Runtime.UI.Interactions
{
    public sealed class UIDismissScope : IDisposable
    {
        private readonly UIDismissDispatcher m_dispatcher;
        private readonly List<RectTransform> m_insideAreas = new();
        private readonly Action<UIDismissTrigger> m_onDismiss;
        private readonly int m_registeredFrame;
        private bool m_disposed;

        internal UIDismissScope(
            UIDismissDispatcher dispatcher,
            IEnumerable<RectTransform> insideAreas,
            Action<UIDismissTrigger> onDismiss,
            bool ignoreCurrentFrame)
        {
            m_dispatcher = dispatcher;
            m_onDismiss = onDismiss;
            IgnorePointerUntilFrame = ignoreCurrentFrame ? UnityEngine.Time.frameCount : -1;
            m_registeredFrame = UnityEngine.Time.frameCount;

            if (insideAreas == null)
            {
                return;
            }

            foreach (var area in insideAreas)
            {
                if (area != null && !m_insideAreas.Contains(area))
                {
                    m_insideAreas.Add(area);
                }
            }
        }

        public IReadOnlyList<RectTransform> InsideAreas => m_insideAreas;
        public bool Enabled { get; set; } = true;
        public bool IsDisposed => m_disposed;
        internal int RegisteredFrame => m_registeredFrame;
        internal int IgnorePointerUntilFrame { get; }

        public void SetInsideAreas(IEnumerable<RectTransform> insideAreas)
        {
            m_insideAreas.Clear();
            if (insideAreas == null)
            {
                return;
            }

            foreach (var area in insideAreas)
            {
                if (area != null && !m_insideAreas.Contains(area))
                {
                    m_insideAreas.Add(area);
                }
            }
        }

        internal bool Contains(Vector2 screenPosition, Camera uiCamera)
        {
            for (int i = 0; i < m_insideAreas.Count; i++)
            {
                var area = m_insideAreas[i];
                if (area == null)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(area, screenPosition, uiCamera))
                {
                    return true;
                }
            }

            return false;
        }

        internal void Dismiss(UIDismissTrigger trigger)
        {
            if (!Enabled || m_disposed)
            {
                return;
            }

            m_onDismiss?.Invoke(trigger);
        }

        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            m_dispatcher?.Unregister(this);
        }
    }
}
