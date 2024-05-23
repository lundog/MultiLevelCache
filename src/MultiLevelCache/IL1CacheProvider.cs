using System;

namespace MultiLevelCaching
{
    public interface IL1CacheProvider
    {
        ICacheItem<T> Get<T>(string key);
        void Remove(string key);
        void Set<T>(string key, T value, DateTime softExpiration, DateTime hardExpiration);
    }
}
