using System;
using Cysharp.Threading.Tasks;

namespace EFrameWork.Runtime.UI.Handles
{
    public enum UIViewHandleState
    {
        Created,
        Closed,
        Opening,
        Open,
        Closing,
        Released
    }

    public sealed class UIViewHandle<TView> : IDisposable, IUIViewHandle where TView : BindingViewBase
    {
        private int m_operationVersion;

        public UIViewHandle(string assetPath, TView view)
        {
            AssetPath = assetPath ?? throw new ArgumentNullException(nameof(assetPath));
            TypedView = view ?? throw new ArgumentNullException(nameof(view));
            State = UIViewHandleState.Created;
        }

        public string AssetPath { get; }

        public TView TypedView { get; }

        public BindingViewBase View => TypedView;

        public UIViewHandleState State { get; private set; }

        public bool IsAlive => State != UIViewHandleState.Released && TypedView != null && !TypedView.IsDisposed;

        public bool IsReleased => State == UIViewHandleState.Released;

        public bool IsOpening => State == UIViewHandleState.Opening;

        public bool IsClosing => State == UIViewHandleState.Closing;

        public bool IsShowing => (State == UIViewHandleState.Open || State == UIViewHandleState.Opening) && TypedView.gameObject != null && TypedView.gameObject.activeSelf;

        public bool UsingCache => IsAlive && TypedView.UsingCache;

        private bool CanStartClose => State == UIViewHandleState.Open || State == UIViewHandleState.Opening;

        public void Open(UILayer? layer = null)
        {
            if (!IsAlive)
            {
                return;
            }

            m_operationVersion++;
            State = UIViewHandleState.Opening;
            TypedView.KillTransition();
            TypedView.PrepareForOpen(layer);
            State = UIViewHandleState.Open;
        }

        public async UniTask OpenAsync(UILayer? layer = null)
        {
            if (!IsAlive)
            {
                return;
            }

            int operationVersion = ++m_operationVersion;
            State = UIViewHandleState.Opening;
            TypedView.KillTransition();
            TypedView.PrepareForOpen(layer);

            try
            {
                await TypedView.PlayOpenTransitionAsync();
            }
            finally
            {
                if (operationVersion == m_operationVersion && State != UIViewHandleState.Released)
                {
                    State = UIViewHandleState.Open;
                }
            }
        }

        public void Close()
        {
            if (!IsAlive || !CanStartClose)
            {
                return;
            }

            m_operationVersion++;
            State = UIViewHandleState.Closing;
            TypedView.PrepareForClose(true);

            if (UsingCache)
            {
                TypedView.DeactivateForReuse();
                State = UIViewHandleState.Closed;
                return;
            }

            TypedView.Release();
            State = UIViewHandleState.Released;
        }

        public async UniTask CloseAsync()
        {
            if (!IsAlive || !CanStartClose)
            {
                return;
            }

            int operationVersion = ++m_operationVersion;
            State = UIViewHandleState.Closing;
            TypedView.PrepareForClose(false);

            try
            {
                await TypedView.PlayCloseTransitionAsync();
            }
            finally
            {
                if (operationVersion == m_operationVersion && State != UIViewHandleState.Released)
                {
                    if (UsingCache)
                    {
                        TypedView.DeactivateForReuse();
                        State = UIViewHandleState.Closed;
                    }
                    else
                    {
                        TypedView.Release();
                        State = UIViewHandleState.Released;
                    }
                }
            }
        }

        public void CloseAndDestroy()
        {
            if (!IsAlive)
            {
                return;
            }

            var dispatchCloseEvent = State == UIViewHandleState.Open || State == UIViewHandleState.Opening;

            m_operationVersion++;
            State = UIViewHandleState.Closing;
            TypedView.PrepareForClose(true, dispatchCloseEvent);
            TypedView.Release(true);
            State = UIViewHandleState.Released;
        }

        public void Dispose()
        {
            CloseAndDestroy();
        }
    }
}
