namespace MultiLevelCaching
{
    public class FetchContext
    {
        /// <summary>
        /// The fetch method is being called on a background thread because a cache item's
        /// soft expiration was within the <see cref="MultiLevelCacheSettings.BackgroundFetchThreshold"/>.
        /// </summary>
        public bool IsBackground { get; set; }

        /// <summary>
        /// If the fetch method fails, there is a recoverable cache item available that has not exceeded the hard expiration.
        /// </summary>
        public bool IsRecoverable { get; set; }
    }
}
