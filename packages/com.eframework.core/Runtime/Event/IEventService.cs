using System;

namespace EFrameWork.Runtime.Event
{
    public interface IEventService
    {
        void Subscribe<T>(Action<T> handler) where T : struct, IEvent;
        void Unsubscribe<T>(Action<T> handler) where T : struct, IEvent;
        void Dispatch<T>(T evt) where T : struct, IEvent;
        void ClearAll();
    }

    public sealed class EventService : IEventService
    {
        public void Subscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            EventBus.Subscribe(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            EventBus.Unsubscribe(handler);
        }

        public void Dispatch<T>(T evt) where T : struct, IEvent
        {
            EventBus.Dispatch(evt);
        }

        public void ClearAll()
        {
            EventBus.ClearAll();
        }
    }
}
