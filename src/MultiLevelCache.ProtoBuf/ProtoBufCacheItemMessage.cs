using ProtoBuf;

namespace MultiLevelCaching.ProtoBuf
{
    [ProtoContract]
    internal class ProtoBufCacheItemMessage<T> : ICacheItemMessage<T>
    {
        [ProtoMember(1)]
        public string Key { get; set; }

        [ProtoMember(2)]
        public ProtoBufCacheItem<T> CacheItem { get; set; }

        ICacheItem<T> ICacheItemMessage<T>.CacheItem => CacheItem;
    }
}
