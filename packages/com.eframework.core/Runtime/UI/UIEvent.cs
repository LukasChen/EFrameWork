using EFramework.Runtime.Event;

namespace EFramework.Runtime.UI
{
    public struct UIInteractionEvent : IEvent
    {
        public string UIName;
        public string ElementName;
        public string InteractionType;
        public string Value;
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
