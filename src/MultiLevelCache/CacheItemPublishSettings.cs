namespace MultiLevelCaching
{
    public class CacheItemPublishSettings
    {
        public ICacheItemPublisherFactory PublisherFactory { get; set; }

        public CacheItemPublishMode PublishMode { get; set; }

        public ICacheItemSerializer Serializer { get; set; }
    }
}
