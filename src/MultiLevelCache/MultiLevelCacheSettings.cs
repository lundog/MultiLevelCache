using System;

namespace MultiLevelCaching
{
    public class MultiLevelCacheSettings
    {
        public TimeSpan? BackgroundFetchThreshold { get; set; }

        public bool EnableEmptyCollectionOnNull { get; set; } = true;

        public bool EnableFetchMultiplexer { get; set; } = true;

        public bool EnableNegativeCaching { get; set; }

        public L1CacheSettings L1Settings { get; set; }

        public L2CacheSettings L2Settings { get; set; }
    }
}
