using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFramework.Runtime.UI
{
    public enum UIQueueItemState
    {
        Pending,
        Showing,
        Completed,
        Canceled,
        Failed
    }

    public sealed class UIQueueTicket
    {
        private readonly UniTaskCompletionSource m_completionSource = new();

        internal UIQueueTicket(UIControllerBase controller, UILayer? layer, int priority, string name)
        {
            Controller = controller;
            Layer = layer;
            Priority = priority;
            Name = string.IsNullOrEmpty(name) ? controller.GetType().Name : name;
            State = UIQueueItemState.Pending;
        }

        public UIControllerBase Controller { get; }
        public UILayer? Layer { get; }
        public int Priority { get; }
        public string Name { get; }
        public UIQueueItemState State { get; private set; }
        public Exception Exception { get; private set; }
        public UniTask Completion => m_completionSource.Task;

        internal void MarkShowing()
        {
            State = UIQueueItemState.Showing;
        }

        internal void MarkCompleted()
        {
            State = UIQueueItemState.Completed;
            m_completionSource.TrySetResult();
        }

        internal void MarkCanceled()
        {
            State = UIQueueItemState.Canceled;
            m_completionSource.TrySetResult();
        }

        internal void MarkFailed(Exception exception)
        {
            Exception = exception;
            State = UIQueueItemState.Failed;
            m_completionSource.TrySetException(exception);
        }
    }

    public interface IUIQueue
    {
        bool IsRunning { get; }
        bool IsPaused { get; }
        int PendingCount { get; }
        UIQueueTicket Current { get; }
        bool EnableDebugLog { get; set; }

        event Action<UIQueueTicket> OnEnqueued;
        event Action<UIQueueTicket> OnStarted;
        event Action<UIQueueTicket> OnCompleted;
        event Action<UIQueueTicket> OnCanceled;
        event Action<UIQueueTicket, Exception> OnFailed;
        event Action OnPaused;
        event Action OnResumed;
        event Action OnIdle;

        UIQueueTicket Enqueue(UIControllerBase controller, UILayer? layer = null, int priority = 0, string name = null);
        UniTask EnqueueAsync(UIControllerBase controller, UILayer? layer = null, int priority = 0, string name = null);
        UniTask WaitUntilIdleAsync();
        void Pause(bool hideCurrent = false);
        void Resume();
        void CancelCurrent();
        void ClearPending();
        void Reset();
    }

    public sealed class UIQueue : IUIQueue
    {
        private sealed class QueueEntry : IComparable<QueueEntry>
        {
            public UIQueueTicket Ticket;
            public long Order;

            public int CompareTo(QueueEntry other)
            {
                int priorityCompare = Ticket.Priority.CompareTo(other.Ticket.Priority);
                return priorityCompare != 0 ? priorityCompare : Order.CompareTo(other.Order);
            }
        }

        private readonly List<QueueEntry> m_pending = new();
        private long m_enqueueOrder;
        private bool m_isRunning;
        private bool m_isPaused;
        private bool m_cancelCurrentRequested;
        private UIQueueTicket m_current;
        private UniTaskCompletionSource m_idleCompletionSource;

        public bool IsRunning => m_isRunning;
        public bool IsPaused => m_isPaused;
        public int PendingCount => m_pending.Count;
        public UIQueueTicket Current => m_current;
        public bool EnableDebugLog { get; set; }

        public event Action<UIQueueTicket> OnEnqueued;
        public event Action<UIQueueTicket> OnStarted;
        public event Action<UIQueueTicket> OnCompleted;
        public event Action<UIQueueTicket> OnCanceled;
        public event Action<UIQueueTicket, Exception> OnFailed;
        public event Action OnPaused;
        public event Action OnResumed;
        public event Action OnIdle;

        public UIQueueTicket Enqueue(UIControllerBase controller, UILayer? layer = null, int priority = 0, string name = null)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            var ticket = new UIQueueTicket(controller, layer, priority, name);
            m_pending.Add(new QueueEntry
            {
                Ticket = ticket,
                Order = m_enqueueOrder++
            });
            m_pending.Sort();

            Log($"Enqueued: {ticket.Name} (Priority: {priority}, Pending: {m_pending.Count})");
            OnEnqueued?.Invoke(ticket);
            StartIfNeeded();
            return ticket;
        }

        public UniTask EnqueueAsync(UIControllerBase controller, UILayer? layer = null, int priority = 0, string name = null)
        {
            return Enqueue(controller, layer, priority, name).Completion;
        }

        public UniTask WaitUntilIdleAsync()
        {
            if (!m_isRunning && m_pending.Count == 0)
            {
                return UniTask.CompletedTask;
            }

            if (m_idleCompletionSource == null)
            {
                m_idleCompletionSource = new UniTaskCompletionSource();
            }
            return m_idleCompletionSource.Task;
        }

        public void Pause(bool hideCurrent = false)
        {
            if (m_isPaused)
            {
                return;
            }

            m_isPaused = true;
            Log("Paused");
            OnPaused?.Invoke();

            if (hideCurrent)
            {
                m_current?.Controller.Hide();
            }
        }

        public void Resume()
        {
            if (!m_isPaused)
            {
                return;
            }

            m_isPaused = false;
            Log("Resumed");
            OnResumed?.Invoke();
            StartIfNeeded();
        }

        public void CancelCurrent()
        {
            if (m_current == null)
            {
                return;
            }

            m_cancelCurrentRequested = true;
            m_current.Controller.Hide();
        }

        public void ClearPending()
        {
            foreach (var entry in m_pending)
            {
                entry.Ticket.MarkCanceled();
                OnCanceled?.Invoke(entry.Ticket);
            }

            m_pending.Clear();
        }

        public void Reset()
        {
            ClearPending();
            CancelCurrent();
            m_current = null;
            m_isRunning = false;
            m_isPaused = false;
            m_cancelCurrentRequested = false;
            m_enqueueOrder = 0;
            m_idleCompletionSource?.TrySetResult();
            m_idleCompletionSource = null;
        }

        private void StartIfNeeded()
        {
            if (m_isRunning || m_isPaused || m_pending.Count == 0)
            {
                return;
            }

            RunAsync().Forget();
        }

        private async UniTask RunAsync()
        {
            if (m_isRunning)
            {
                return;
            }

            m_isRunning = true;
            try
            {
                while (!m_isPaused && m_pending.Count > 0)
                {
                    var entry = m_pending[0];
                    m_pending.RemoveAt(0);
                    await ShowTicketAsync(entry.Ticket);
                }
            }
            finally
            {
                m_isRunning = false;
                if (!m_isPaused && m_pending.Count == 0)
                {
                    m_idleCompletionSource?.TrySetResult();
                    m_idleCompletionSource = null;
                    OnIdle?.Invoke();
                }
            }
        }

        private async UniTask ShowTicketAsync(UIQueueTicket ticket)
        {
            m_current = ticket;
            m_cancelCurrentRequested = false;
            ticket.MarkShowing();
            Log($"Showing: {ticket.Name}");
            OnStarted?.Invoke(ticket);

            var completionSource = new UniTaskCompletionSource();
            void OnClosed()
            {
                completionSource.TrySetResult();
            }

            ticket.Controller.OnClosed += OnClosed;
            try
            {
                await ticket.Controller.ShowAsync(ticket.Layer);
                await completionSource.Task;

                if (m_cancelCurrentRequested)
                {
                    ticket.MarkCanceled();
                    OnCanceled?.Invoke(ticket);
                }
                else
                {
                    ticket.MarkCompleted();
                    OnCompleted?.Invoke(ticket);
                }
            }
            catch (Exception ex)
            {
                ticket.MarkFailed(ex);
                Debug.LogError($"[UIQueue] Failed to show '{ticket.Name}': {ex}");
                OnFailed?.Invoke(ticket, ex);
            }
            finally
            {
                ticket.Controller.OnClosed -= OnClosed;
                if (ReferenceEquals(m_current, ticket))
                {
                    m_current = null;
                }
                m_cancelCurrentRequested = false;
            }
        }

        private void Log(string message)
        {
            if (EnableDebugLog)
            {
                Debug.Log($"[UIQueue] {message}");
            }
        }
    }
}
