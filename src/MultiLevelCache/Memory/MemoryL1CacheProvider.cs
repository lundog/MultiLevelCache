﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiLevelCaching.Memory
{
    public class MemoryL1CacheProvider : IL1CacheProvider
    {
        private readonly MemoryCache _cache;
        private readonly ILogger<MemoryL1CacheProvider> _logger;

        public MemoryL1CacheProvider(
            IOptions<MemoryCacheOptions> optionsAccessor = null,
            ILogger<MemoryL1CacheProvider> logger = null)
        {
            _cache = new MemoryCache(optionsAccessor ?? new MemoryCacheOptions());
            _logger = logger;
        }

        public T Get<T>(string key)
        {
            try
            {
                return _cache.Get<T>(key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while getting an item from MemoryCache. Key={Key}", key);
                return default;
            }
        }

        public IList<T> Get<T>(IEnumerable<string> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            try
            {
                return keys
                    .Select(Get<T>)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while getting items from MemoryCache.");
                return Array.Empty<T>();
            }
        }

        public void Remove(string key)
        {
            try
            {
                _cache.Remove(key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while removing an item from MemoryCache. Key={Key}", key);
            }
        }

        public void Set<T>(string key, T value, TimeSpan duration)
        {
            try
            {
                _cache.Set(key, value, duration);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting an item in MemoryCache. Key={Key}", key);
            }
        }
    }
}
