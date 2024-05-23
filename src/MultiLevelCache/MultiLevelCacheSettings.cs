using System;
using System.Collections.Generic;

namespace MultiLevelCaching
{
    public class MultiLevelCacheSettings
    {
        public TimeSpan? BackgroundFetchThreshold { get; set; }

        public bool EnableFetchMultiplexer { get; set; } = true;

        public bool EnableNegativeCaching { get; set; }

        public L1CacheSettings L1Settings { get; set; }

        public IList<L2CacheSettings> L2Settings
        {
            get => _l2Settings ?? (_l2Settings = new List<L2CacheSettings>());
            set => _l2Settings = value;
        }
        private IList<L2CacheSettings> _l2Settings;
    }
}
