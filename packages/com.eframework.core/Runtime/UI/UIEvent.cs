using EFramework.Runtime.Event;

namespace EFramework.Runtime.UI
{
    public struct ButtonClickEvent : IEvent
    {
        public string UIName;
        public string ButtonName;
    }

    public struct UIOpenEvent : IEvent
    {
        public string UIName;
    }

    public struct UICloseEvent : IEvent
    {
        public string UIName;
    }
}