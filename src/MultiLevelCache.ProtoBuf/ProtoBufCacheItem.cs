using ProtoBuf;
using System;

namespace MultiLevelCaching.ProtoBuf
{
    [ProtoContract]
    internal class ProtoBufCacheItem<T> : ICacheItem<T>
    {
        [ProtoMember(1)]
        public T Value { get; set; }

        [ProtoMember(2)]
        public DateTime SoftExpiration { get; set; }

        [ProtoMember(3)]
        public DateTime HardExpiration { get; set; }
    }
}
