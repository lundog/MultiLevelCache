using System;

namespace MultiLevelCaching
{
    public class L2CacheSettings
    {
        public IL2CacheProvider Provider { get; set; }

        public ICacheItemSerializer Serializer { get; set; }

        public TimeSpan SoftDuration { get; set; }

        public TimeSpan? HardDuration { get; set; }
    }
}
