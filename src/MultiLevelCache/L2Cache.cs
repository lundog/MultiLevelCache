using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiLevelCaching
{
    internal class L2Cache<T>
    {
        private TimeSpan SoftDuration { get; }
        private TimeSpan HardDuration { get; }

        private readonly IL2CacheProvider _provider;
        private readonly ICacheItemSerializer _serializer;
        private readonly ILogger<L2Cache<T>> _logger;

        public L2Cache(
            L2CacheSettings settings,
            ILogger<L2Cache<T>> logger = null)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (settings.HardDuration < settings.SoftDuration)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.HardDuration), $"{nameof(settings.HardDuration)} must be greater than or equal to {nameof(settings.SoftDuration)}.");
            }

            _provider = settings.Provider ?? throw new ArgumentException($"If {nameof(MultiLevelCacheSettings.L2Settings)} is set, {nameof(settings.Provider)} is required.", nameof(settings.Provider));
            _serializer = settings.Serializer ?? throw new ArgumentException($"If {nameof(MultiLevelCacheSettings.L2Settings)} is set, {nameof(settings.Serializer)} is required.", nameof(settings.Serializer));
            SoftDuration = settings.SoftDuration;
            HardDuration = settings.HardDuration ?? new TimeSpan(settings.SoftDuration.Ticks * 2);
            _logger = logger;
        }

        public async Task<ICacheItem<T>> Get(string key)
        {
            try
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                var bytes = await _provider.Get(key).ConfigureAwait(false);
                return bytes != null
                    ? _serializer.Deserialize<T>(bytes)
                    : null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while getting an item from L2 cache. Key={Key}", key);
                return null;
            }
        }

        public async Task<IList<ICacheItem<T>>> Get(IEnumerable<string> keys)
        {
            try
            {
                if (keys is null)
                {
                    throw new ArgumentNullException(nameof(keys));
                }

                return (await _provider.Get(keys).ConfigureAwait(false))
                    .Select(bytes => bytes != null
                        ? _serializer.Deserialize<T>(bytes)
                        : null
                    )
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while getting items from L2 cache.");
                return Array.Empty<ICacheItem<T>>();
            }
        }

        public async Task Remove(string key)
        {
            try
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                await _provider.Remove(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while removing an item from L2 cache. Key={Key}", key);
            }
        }

        public async Task Remove(IEnumerable<string> keys)
        {
            try
            {
                if (keys is null)
                {
                    throw new ArgumentNullException(nameof(keys));
                }

                await _provider.Remove(keys).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while removing items from L2 cache.");
            }
        }

        public async Task Set(string key, T value)
        {
            try
            {
                var bytes = Serialize(value);
                if (bytes != null)
                {
                    await Set(key, bytes).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting an item in L2 cache. Key={Key}", key);
            }
        }

        public async Task Set(IEnumerable<KeyValuePair<string, T>> values)
        {
            try
            {
                if (values is null)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                var byteValues = values
                    .Select(valuePair => new KeyValuePair<string, byte[]>(valuePair.Key, Serialize(valuePair.Value)))
                    .Where(valuePair => valuePair.Value != null)
                    .ToList();
                if (byteValues.Count == 0)
                {
                    return;
                }

                await Set(byteValues).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting items in L2 cache.");
            }
        }

        public async Task Set(IEnumerable<KeyValuePair<string, ICacheItem<T>>> cacheItems)
        {
            try
            {
                if (cacheItems is null)
                {
                    throw new ArgumentNullException(nameof(cacheItems));
                }

                var byteValues = cacheItems
                    .Select(valuePair => new KeyValuePair<string, byte[]>(
                        valuePair.Key,
                        Serialize(valuePair.Value.Value, softExpirationLimit: valuePair.Value.SoftExpiration, hardExpirationLimit: valuePair.Value.HardExpiration)
                    ))
                    .Where(valuePair => valuePair.Value != null)
                    .ToList();
                if (byteValues.Count == 0)
                {
                    return;
                }

                await Set(byteValues).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting items in L2 cache.");
            }
        }

        private async Task Set(string key, byte[] bytes)
        {
            try
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                await _provider.Set(key, bytes, HardDuration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting an item in L2 cache. Key={Key}", key);
            }
        }

        private async Task Set(IEnumerable<KeyValuePair<string, byte[]>> values)
        {
            try
            {
                if (values is null)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                await _provider.Set(values, HardDuration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting items in L2 cache.");
            }
        }

        private byte[] Serialize(T value, DateTime? softExpirationLimit = null, DateTime? hardExpirationLimit = null)
            => _serializer.Serialize(value, softExpiration: ToSoftExpiration(softExpirationLimit), hardExpiration: ToHardExpiration(hardExpirationLimit));

        private DateTime ToSoftExpiration(DateTime? limit = null)
            => DateTime.UtcNow.Add(SoftDuration).Min(limit);

        private DateTime ToHardExpiration(DateTime? limit = null)
            => DateTime.UtcNow.Add(HardDuration).Min(limit);
    }
}
