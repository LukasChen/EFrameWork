using System;

namespace EFramework.Runtime.Procedure
{
    public readonly struct ProcedureEnterContext
    {
        public ProcedureEnterContext(Type previousProcedureType, object payload)
        {
            PreviousProcedureType = previousProcedureType;
            Payload = payload;
        }

        public Type PreviousProcedureType { get; }
        public object Payload { get; }
        public bool HasAnyPayload => Payload != null;

        public bool HasPayloadOfType<T>()
        {
            return Payload is T;
        }

        public bool HasPayload<T>()
        {
            return Payload is T;
        }

        public bool TryGetPayload<T>(out T payload)
        {
            if (Payload is T typedPayload)
            {
                payload = typedPayload;
                return true;
            }

            payload = default;
            return false;
        }
    }
}
