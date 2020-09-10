using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskMultiplexing;

namespace MultiLevelCaching
{
    public interface IMultiLevelCache<TKey, T>
    {
        Task<T> GetOrAdd(TKey key, Func<TKey, Task<T>> fetch);
        Task<IDictionary<TKey, T>> GetOrAdd(IEnumerable<TKey> keys, Func<ICollection<TKey>, Task<IDictionary<TKey, T>>> fetch);
        Task Remove(TKey key);
        Task Set(TKey key, T value);
    }

	public abstract class MultiLevelCache<TKey, T> : IMultiLevelCache<TKey, T>
	{
        private static Func<T> EmptyCollectionFactory => __emptyCollectionFactoryLazy.Value;
        private static readonly Lazy<Func<T>> __emptyCollectionFactoryLazy = new Lazy<Func<T>>(TypeExtensions.GetEmptyCollectionFactoryOrNull<T>);

        public TimeSpan? BackgroundFetchThreshold { get; }

        public bool EnableEmptyCollectionOnNull { get; }

        public bool EnableFetchMultiplexer => _fetchMultiplexer != null;

        public bool EnableInvalidation => _l1Invalidator != null;

        public bool EnableL1 => _l1Cache != null;

        public bool EnableL2 => _l2Cache != null;

        public bool EnableNegativeCaching { get; }

        public TimeSpan L1HardDuration { get; }

        public TimeSpan L1SoftDuration { get; }

        public TimeSpan L2HardDuration { get; }

        public TimeSpan L2SoftDuration { get; }

        protected abstract string FormatKey(TKey key);

		private readonly TaskMultiplexer<TKey, T> _fetchMultiplexer;
        private readonly IL1CacheProvider _l1Cache;
        private readonly ICacheInvalidator _l1Invalidator;
        private readonly IL2CacheProvider _l2Cache;
        private readonly ICacheItemSerializer _l2Serializer;
        private readonly ILogger<MultiLevelCache<TKey, T>> _logger;

		public MultiLevelCache(
            MultiLevelCacheSettings settings,
            ILogger<MultiLevelCache<TKey, T>> logger = null)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			if (settings.BackgroundFetchThreshold.HasValue
				&& settings.BackgroundFetchThreshold.Value >= settings.L1Settings?.SoftDuration
				&& settings.BackgroundFetchThreshold.Value >= settings.L2Settings?.SoftDuration)
            {
				throw new ArgumentOutOfRangeException(nameof(settings.BackgroundFetchThreshold), $"If {nameof(settings.BackgroundFetchThreshold)} is set, it must be less than either L1 or L2 SoftDuration.");
            }

            if (settings.L1Settings != null)
            {
                if (settings.L1Settings.Provider == null)
                {
                    throw new ArgumentException($"If {nameof(settings.L1Settings)} is set, {nameof(settings.L1Settings.Provider)} is required.", nameof(settings.L1Settings.Provider));
                }

                if (settings.L1Settings.HardDuration < settings.L1Settings.SoftDuration)
                {
                    throw new ArgumentOutOfRangeException(nameof(settings.L1Settings.HardDuration), $"{nameof(settings.L1Settings.HardDuration)} must be greater than or equal to {nameof(settings.L1Settings.SoftDuration)}.");
                }
            }

            if (settings.L2Settings != null)
            {
                if (settings.L2Settings.Provider == null)
                {
                    throw new ArgumentException($"If {nameof(settings.L2Settings)} is set, {nameof(settings.L2Settings.Provider)} is required.", nameof(settings.L2Settings.Provider));
                }

                if (settings.L2Settings.Serializer == null)
                {
                    throw new ArgumentException($"If {nameof(settings.L2Settings)} is set, {nameof(settings.L2Settings.Serializer)} is required.", nameof(settings.L2Settings.Serializer));
                }

                if (settings.L2Settings.HardDuration < settings.L2Settings.SoftDuration)
                {
                    throw new ArgumentOutOfRangeException(nameof(settings.L2Settings.HardDuration), $"{nameof(settings.L2Settings.HardDuration)} must be greater than or equal to {nameof(settings.L2Settings.SoftDuration)}.");
                }
            }

            BackgroundFetchThreshold = settings.BackgroundFetchThreshold;
            EnableEmptyCollectionOnNull = settings.EnableEmptyCollectionOnNull;
            EnableNegativeCaching = settings.EnableNegativeCaching;

            if (settings.EnableFetchMultiplexer)
            {
                _fetchMultiplexer = new TaskMultiplexer<TKey, T>();
            }

            if (settings.L1Settings != null)
            {
                _l1Cache = settings.L1Settings.Provider;

                if (settings.L1Settings.Invalidator != null)
                {
                    _l1Invalidator = settings.L1Settings.Invalidator;
                    _l1Invalidator.Subscribe(_l1Cache);
                }

                L1SoftDuration = settings.L1Settings.SoftDuration;
                L1HardDuration = settings.L1Settings.HardDuration ?? new TimeSpan(settings.L1Settings.SoftDuration.Ticks * 2);
            }

            if (settings.L2Settings != null)
            {
                _l2Cache = settings.L2Settings.Provider;
                _l2Serializer = settings.L2Settings.Serializer;
                L2SoftDuration = settings.L2Settings.SoftDuration;
                L2HardDuration = settings.L2Settings.HardDuration ?? new TimeSpan(settings.L2Settings.SoftDuration.Ticks * 2);
            }

            _logger = logger;
		}

        public async Task<T> GetOrAdd(TKey key, Func<TKey, Task<T>> fetch)
        {
            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            string keyString = FormatKey(key);
            ICacheItem<T> l1CacheItem = null;
            ICacheItem<T> l2CacheItem = null;
            bool isSoftHit = false;
            bool isStale = false;
            T value = default;

            if (EnableL1)
            {
                l1CacheItem = _l1Cache.Get<T>(keyString);
                if (TryFromCacheItem(l1CacheItem, out value))
                {
                    isSoftHit = true;
                    isStale = IsStale(l1CacheItem);
                }
            }

            if (EnableL2 && !isSoftHit)
            {
                // Use L2 on L1 miss.
                var l2CacheItemBytes = await _l2Cache.Get(keyString).ConfigureAwait(false);
                l2CacheItem = l2CacheItemBytes != null
                    ? _l2Serializer.Deserialize<T>(l2CacheItemBytes)
                    : null;

                if (TryFromCacheItem(l2CacheItem, out value))
                {
                    if (EnableL1)
                    {
                        SetL1(keyString, value);
                    }
                    isSoftHit = true;
                    isStale = IsStale(l2CacheItem);
                }
            }

            if (!isSoftHit)
            {
                // Use fetch on L1 & L2 miss.
                try
                {
                    value = await FetchAndCacheResult(key, fetch, setL2InBackground: true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Use hard expiration on fetch failure.
                    if (TryFromCacheItem(l1CacheItem, out value, useHardExpiration: true)
                        || TryFromCacheItem(l2CacheItem, out value, useHardExpiration: true))
                    {
                        _logger?.LogWarning(ex, "A recoverable error occurred while fetching an item after a soft cache miss. Key={Key}", keyString);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (isStale)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await FetchAndCacheResult(key, fetch, setL2InBackground: false).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "An error occurred while refreshing a stale cache item. Key={Key}", keyString);
                    }
                });
            }

            if (value == null
                && EnableEmptyCollectionOnNull
                && EmptyCollectionFactory != null)
            {
                value = EmptyCollectionFactory();
            }

            return value;
        }

        public async Task<IDictionary<TKey, T>> GetOrAdd(IEnumerable<TKey> keys, Func<ICollection<TKey>, Task<IDictionary<TKey, T>>> fetch)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }
            if (fetch == null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            var keysList = keys
                .Distinct()
                .ToList();

            if (!keysList.Any())
            {
                return new Dictionary<TKey, T>();
            }

            var keyStrings = keysList
                .Select(FormatKey)
                .ToList();

            var values = new Dictionary<TKey, T>(keysList.Count);

            IList<ICacheItem<T>> l1CacheItems = null;
            IList<int> l2IndexesToGet = null;
            IList<ICacheItem<T>> l2CacheItems = null;
            IList<TKey> staleKeys = null;

            if (EnableL1)
            {
                l1CacheItems = _l1Cache.Get<T>(keyStrings);

                for (int i = 0; i < l1CacheItems.Count; i++)
                {
                    var cacheItem = l1CacheItems[i];
                    if (TryFromCacheItem(cacheItem, out var value))
                    {
                        var key = keysList[i];

                        values[key] = value;

                        if (IsStale(cacheItem))
                        {
                            if (staleKeys == null)
                            {
                                staleKeys = new List<TKey>();
                            }
                            staleKeys.Add(key);
                        }
                    }
                }
            }

            if (EnableL2 && values.Count < keysList.Count)
            {
                // Use L2 on L1 misses.
                var l2KeyCount = keysList.Count - values.Count;
                l2IndexesToGet = new List<int>(l2KeyCount);
                var l2KeyStrings = new List<string>(l2KeyCount);
                for (int i = 0; i < keysList.Count; i++)
                {
                    var key = keysList[i];
                    if (!values.ContainsKey(key))
                    {
                        l2IndexesToGet.Add(i);
                        var keyString = keyStrings[i];
                        l2KeyStrings.Add(keyString);
                    }
                }
                var l2CacheItemBytes = await _l2Cache.Get(l2KeyStrings).ConfigureAwait(false);
                l2CacheItems = l2CacheItemBytes
                    .Select(cacheItemBytes => cacheItemBytes != null
                        ? _l2Serializer.Deserialize<T>(cacheItemBytes)
                        : null
                    )
                    .ToList();

                for (int i = 0; i < l2CacheItems.Count; i++)
                {
                    var cacheItem = l2CacheItems[i];
                    if (TryFromCacheItem(cacheItem, out var value))
                    {
                        var keyIndex = l2IndexesToGet[i];
                        var key = keysList[keyIndex];

                        if (EnableL1)
                        {
                            var keyString = keyStrings[keyIndex];
                            SetL1(keyString, value);
                        }

                        values[key] = value;

                        if (IsStale(cacheItem))
                        {
                            if (staleKeys == null)
                            {
                                staleKeys = new List<TKey>();
                            }
                            staleKeys.Add(key);
                        }
                    }
                }
            }

            if (values.Count < keysList.Count)
            {
                // Use fetch on L1 & L2 misses.
                var fetchKeys = keysList
                    .Where(key => !values.ContainsKey(key))
                    .ToList();
                IDictionary<TKey, T> fetchValues;
                try
                {
                    fetchValues = await FetchAndCacheResults(fetchKeys, fetch, setL2InBackground: true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Use hard expiration on fetch failure.
                    var recoveredValues = new Dictionary<TKey, T>(fetchKeys.Count);

                    if (l1CacheItems != null)
                    {
                        for (int i = 0; i < l1CacheItems.Count; i++)
                        {
                            var key = keysList[i];
                            if (!values.ContainsKey(key)
                                && !recoveredValues.ContainsKey(key)
                                && TryFromCacheItem(l1CacheItems[i], out var value, useHardExpiration: true))
                            {
                                recoveredValues[key] = value;
                            }
                        }
                    }

                    if (recoveredValues.Count < fetchKeys.Count && l2CacheItems != null)
                    {
                        for (int i = 0; i < l2CacheItems.Count; i++)
                        {
                            var keyIndex = l2IndexesToGet[i];
                            var key = keysList[keyIndex];
                            if (!values.ContainsKey(key)
                                && !recoveredValues.ContainsKey(key)
                                && TryFromCacheItem(l2CacheItems[i], out var value, useHardExpiration: true))
                            {
                                recoveredValues[key] = value;
                            }
                        }
                    }

                    if (recoveredValues.Count == fetchKeys.Count)
                    {
                        _logger?.LogWarning(ex, "A recoverable error occurred while fetching items after soft cache misses.");
                        fetchValues = recoveredValues;
                    }
                    else
                    {
                        throw;
                    }
                }

                foreach (var fetchValuePair in fetchValues)
                {
                    values[fetchValuePair.Key] = fetchValuePair.Value;
                }
            }

            if (staleKeys?.Any() == true)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await FetchAndCacheResults(staleKeys, fetch, setL2InBackground: false).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "An error occurred while refreshing stale cache items.");
                    }
                });
            }

            return values;
        }

        public async Task Remove(TKey key)
        {
            var keyString = FormatKey(key);

            if (EnableL1)
            {
                _l1Cache.Remove(keyString);
                if (EnableInvalidation)
                {
                    _l1Invalidator.Publish(keyString);
                }
            }

            if (EnableL2)
            {
                await _l2Cache.Remove(keyString).ConfigureAwait(false);
            }
        }

        public Task Set(TKey key, T value)
        {
            var keyString = FormatKey(key);

            if (EnableL1)
            {
                SetL1(keyString, value);
            }

            if (EnableL2)
            {
                return SetL2(keyString, value);
            }
            else
            {
                return Task.CompletedTask;
            }    
        }

        private async Task<T> FetchAndCacheResult(
            TKey key,
            Func<TKey, Task<T>> fetch,
            bool setL2InBackground)
        {
            var value = await (EnableFetchMultiplexer
                ? _fetchMultiplexer.GetMultiplexed(key, fetch)
                : fetch(key)
            ).ConfigureAwait(false);

            if (IsCacheable(value))
            {
                var keyString = FormatKey(key);

                if (EnableL1)
                {
                    SetL1(keyString, value);
                }
                if (EnableL2)
                {
                    if (setL2InBackground)
                    {
                        _ = Task.Run(() => SetL2(keyString, value));
                    }
                    else
                    {
                        await SetL2(keyString, value).ConfigureAwait(false);
                    }
                }
            }

            return value;
        }

        private async Task<IDictionary<TKey, T>> FetchAndCacheResults(
            ICollection<TKey> keys,
            Func<ICollection<TKey>, Task<IDictionary<TKey, T>>> fetch,
            bool setL2InBackground)
        {
            var values = await (EnableFetchMultiplexer
                ? _fetchMultiplexer.GetMultiplexed(keys, fetch)
                : fetch(keys)
            ).ConfigureAwait(false);

            if (!values.Any())
            {
                return values;
            }

            var l2ValuesToSet = EnableL2
                ? new List<KeyValuePair<string, T>>(values.Count)
                : null;
            
            foreach (var valuePair in values)
            {
                if (IsCacheable(valuePair.Value))
                {
                    var keyString = FormatKey(valuePair.Key);

                    if (EnableL1)
                    {
                        SetL1(keyString, valuePair.Value);
                    }
                    if (EnableL2)
                    {
                        l2ValuesToSet.Add(new KeyValuePair<string, T>(keyString, valuePair.Value));
                    }
                }
            }

            if (l2ValuesToSet?.Any() == true)
            {
                var setL2Tasks = l2ValuesToSet.Select(valuePair => SetL2(valuePair.Key, valuePair.Value));
                if (setL2InBackground)
                {
                    _ = Task.Run(() => Task.WhenAll(setL2Tasks));
                }
                else
                {
                    await Task.WhenAll(setL2Tasks).ConfigureAwait(false);
                }
            }

            return values;
        }

        private void SetL1(string keyString, T value)
        {
            _l1Cache.Set(
                keyString,
                value,
                softExpiration: DateTime.UtcNow.Add(L1SoftDuration),
                hardExpiration: DateTime.UtcNow.Add(L1HardDuration)
            );
        }

        private Task SetL2(string keyString, T value)
        {
            var bytes = _l2Serializer.Serialize(
                value,
                softExpiration: DateTime.UtcNow.Add(L2SoftDuration),
                hardExpiration: DateTime.UtcNow.Add(L2HardDuration)
            );
            if (bytes == null)
            {
                return Task.CompletedTask;
            }
            return _l2Cache.Set(keyString, bytes, L2HardDuration);
        }

        private bool TryFromCacheItem(ICacheItem<T> cacheItem, out T value, bool useHardExpiration = false)
        {
            if (cacheItem != null
                && (
                    (useHardExpiration && cacheItem.HardExpiration > DateTime.UtcNow)
                    ||
                    (!useHardExpiration && cacheItem.SoftExpiration > DateTime.UtcNow)
                )
            )
            {
                value = cacheItem.Value;
                return true;
            }
            value = default;
            return false;
        }

        private bool IsCacheable(T value)
            => EnableNegativeCaching || !EqualityComparer<T>.Default.Equals(value, default);

        private bool IsStale(ICacheItem<T> cacheItem)
            => cacheItem.SoftExpiration - DateTime.UtcNow <= BackgroundFetchThreshold;
    }
}
