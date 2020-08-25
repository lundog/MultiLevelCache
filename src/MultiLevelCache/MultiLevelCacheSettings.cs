using System;

namespace MultiLevelCaching
{
    public class MultiLevelCacheSettings<TKey>
    {
        public TimeSpan? BackgroundFetchThreshold { get; set; }

        public bool EnableEmptyCollectionOnNull { get; set; } = true;

        public bool EnableFetchMultiplexer { get; set; } = true;

        public bool EnableNegativeCaching { get; set; }

        public Func<TKey, string> KeyFormat { get; }

        public L1CacheSettings L1Settings { get; }

        public L2CacheSettings L2Settings { get; }

        public MultiLevelCacheSettings(
            Func<TKey, string> keyFormat,
            L1CacheSettings l1settings = null,
            L2CacheSettings l2settings = null)
        {
            KeyFormat = keyFormat ?? throw new ArgumentNullException(nameof(keyFormat));
            L1Settings = l1settings;
            L2Settings = l2settings;
        }
    }
}
