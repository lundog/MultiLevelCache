namespace MultiLevelCaching
{
    public interface ICacheItemMessage<T>
    {
        string Key { get; }

        ICacheItem<T> CacheItem { get; }
    }
}
