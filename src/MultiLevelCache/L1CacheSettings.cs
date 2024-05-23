using System;

namespace MultiLevelCaching
{
    public class L1CacheSettings
    {
        public IL1CacheProvider Provider { get; set; }

        public TimeSpan SoftDuration { get; set; }

        public TimeSpan? HardDuration { get; set; }

        public CacheItemPublishSettings PublishSettings { get; set; }
    }
}
