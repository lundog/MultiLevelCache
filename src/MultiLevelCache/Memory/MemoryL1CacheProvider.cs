using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;

namespace MultiLevelCaching.Memory
{
    public class MemoryL1CacheProvider : IL1CacheProvider
    {
        private readonly MemoryCache _cache;

        public MemoryL1CacheProvider(IOptions<MemoryCacheOptions> optionsAccessor = null)
        {
            _cache = new MemoryCache(optionsAccessor ?? new MemoryCacheOptions());
        }

        public ICacheItem<T> Get<T>(string key)
            => _cache.Get<MemoryCacheItem<T>>(key);

        public void Remove(string key)
            => _cache.Remove(key);

        public void Set<T>(string key, T value, DateTime softExpiration, DateTime hardExpiration)
            => _cache.Set(
                key,
                new MemoryCacheItem<T>
                {
                    Value = value,
                    SoftExpiration = softExpiration,
                    HardExpiration = hardExpiration
                },
                hardExpiration
            );
    }
}
