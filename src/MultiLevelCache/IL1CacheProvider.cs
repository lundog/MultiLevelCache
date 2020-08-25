using System;
using System.Collections.Generic;

namespace MultiLevelCaching
{
    public interface IL1CacheProvider
    {
        T Get<T>(string key);
        IList<T> Get<T>(IEnumerable<string> keys);
        void Remove(string key);
        void Set<T>(string key, T value, TimeSpan duration);
    }
}
