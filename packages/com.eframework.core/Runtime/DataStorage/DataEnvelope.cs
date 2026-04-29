using System;

namespace EFrameWork.Runtime.DataStorage
{
    [Serializable]
    public sealed class DataEnvelope<T>
    {
        public int Version;
        public long SavedAt;
        public T Data;
    }
}
