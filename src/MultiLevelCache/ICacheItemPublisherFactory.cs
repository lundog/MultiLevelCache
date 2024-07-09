namespace MultiLevelCaching
{
    public interface ICacheItemPublisherFactory
    {
        ICacheItemPublisher<T> NewPublisher<T>(string cacheName, CacheItemPublishMode publishMode, IL1CacheProvider cache, ICacheItemSerializer serializer);
    }
}
