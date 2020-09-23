using System;
using System.Collections.Generic;

namespace MultiLevelCaching
{
    public interface IL1CacheProvider
    {
        ICacheItem<T> Get<T>(string key);
        IList<ICacheItem<T>> Get<T>(IEnumerable<string> keys);
        void Remove(string key);
        void Set<T>(string key, T value, DateTime softExpiration, DateTime hardExpiration);
    }
}
