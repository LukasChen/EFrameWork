using EFrameWork.Runtime.Event;

namespace EFrameWork.Runtime.UI
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