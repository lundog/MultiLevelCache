using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskMultiplexing;

namespace MultiLevelCaching
{
    public abstract class MultiLevelCache<TKey, T>
    {
        protected TimeSpan? BackgroundFetchThreshold { get; }
        protected bool EnableFetchMultiplexer => _fetchMultiplexer != null;
        protected bool EnableL1 => _l1Cache != null;
        protected bool EnableL2 => _l2Caches?.Any() ?? false;
        protected bool EnableNegativeCaching { get; }

        protected virtual string CacheName => _cacheName ?? (_cacheName = DefaultCacheName());
        private string _cacheName;

        protected abstract string FormatKey(TKey key);

        private readonly TaskMultiplexer<TKey, T> _fetchMultiplexer;
        private readonly L1Cache<T> _l1Cache;
        private readonly IReadOnlyList<L2Cache<T>> _l2Caches;
        private readonly ILogger<MultiLevelCache<TKey, T>> _logger;

        public MultiLevelCache(
            MultiLevelCacheSettings settings,
            ILoggerFactory loggerFactory = null)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.BackgroundFetchThreshold.HasValue
                && (settings.L1Settings == null || settings.BackgroundFetchThreshold >= settings.L1Settings.SoftDuration)
                && settings.L2Settings.All(s => settings.BackgroundFetchThreshold >= s.SoftDuration))
            {
                throw new ArgumentOutOfRangeException(nameof(settings.BackgroundFetchThreshold), $"If {nameof(settings.BackgroundFetchThreshold)} is set, it must be less than either L1 or L2 SoftDuration.");
            }

            BackgroundFetchThreshold = settings.BackgroundFetchThreshold;
            EnableNegativeCaching = settings.EnableNegativeCaching;

            if (settings.EnableFetchMultiplexer)
            {
                _fetchMultiplexer = new TaskMultiplexer<TKey, T>();
            }

            if (settings.L1Settings != null)
            {
                _l1Cache = new L1Cache<T>(CacheName, settings.L1Settings, loggerFactory?.CreateLogger<L1Cache<T>>());
            }

            _l2Caches = settings.L2Settings.Select(x => new L2Cache<T>(x, loggerFactory?.CreateLogger<L2Cache<T>>())).ToList().AsReadOnly();

            _logger = loggerFactory?.CreateLogger<MultiLevelCache<TKey, T>>();
        }

        public async Task<T> Get(TKey key)
        {
            var cacheItem = (await GetCacheItems(new[] { key }).ConfigureAwait(false)).First().Value.CacheItem;
            
            return cacheItem != null
                ? cacheItem.Value
                : default;
        }

        public async Task<IDictionary<TKey, T>> Get(IEnumerable<TKey> keys)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var cacheItems = await GetCacheItems(keys).ConfigureAwait(false);

            return cacheItems
                .Where(cacheItemPair => cacheItemPair.Value.CacheItem != null)
                .ToDictionary(cacheItemPair => cacheItemPair.Key, cacheItemPair => cacheItemPair.Value.CacheItem.Value);
        }

        public async Task<T> GetOrAdd(TKey key, Func<TKey, Task<T>> fetch)
        {
            if (fetch is null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            T result = default;

            var cacheResult = (await GetCacheItems(new[] { key }).ConfigureAwait(false)).First().Value;

            // Check for cache hit.
            if (cacheResult.CacheItem != null)
            {
                result = cacheResult.CacheItem.Value;

                // Check for stale hit.
                if (cacheResult.CacheItem != null
                    && IsStale(cacheResult.CacheItem))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await FetchAndCacheResult(key, fetch, setL2InBackground: false).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "An error occurred while refreshing a stale cache item. Key={Key}", cacheResult.CacheKey);
                        }
                    });
                }
            }
            // Use fetch on cache miss.
            else
            {
                try
                {
                    result = await FetchAndCacheResult(key, fetch, setL2InBackground: true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Use hard expiration on fetch failure.
                    var recoveredItem = cacheResult.AllCacheItems.FirstOrDefault(cacheItem => TryFromCacheItem(cacheItem, out var _, useHardExpiration: true));
                    if (recoveredItem != null)
                    {
                        result = recoveredItem.Value;
                        _logger?.LogWarning(ex, "A recoverable error occurred while fetching an item after a soft cache miss. Key={Key}", cacheResult.CacheKey);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return result;
        }

        public async Task<IDictionary<TKey, T>> GetOrAdd(IEnumerable<TKey> keys, Func<ICollection<TKey>, Task<IDictionary<TKey, T>>> fetch)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }
            if (fetch is null)
            {
                throw new ArgumentNullException(nameof(fetch));
            }

            var cacheItems = await GetCacheItems(keys).ConfigureAwait(false);

            // Check for cache hits.
            var results = cacheItems
                .Where(cacheItemPair => cacheItemPair.Value.CacheItem != null)
                .ToDictionary(cacheItemPair => cacheItemPair.Key, cacheItemPair => cacheItemPair.Value.CacheItem.Value);

            // Use fetch on cache misses.
            if (results.Count < cacheItems.Count)
            {
                var missedCacheItems = cacheItems
                    .Where(cacheItemPair => cacheItemPair.Value.CacheItem == null)
                    .ToList();

                try
                {
                    var fetchKeys = missedCacheItems
                        .Select(cacheItemPair => cacheItemPair.Key)
                        .ToList();

                    var fetchResults = await FetchAndCacheResults(fetchKeys, fetch, setL2InBackground: true).ConfigureAwait(false);

                    foreach (var fetchResult in fetchResults)
                    {
                        results[fetchResult.Key] = fetchResult.Value;
                    }
                }
                catch (Exception ex)
                {
                    // Use hard expiration on fetch failure.
                    foreach (var missedCacheItem in missedCacheItems)
                    {
                        var recoveredItem = missedCacheItem.Value.AllCacheItems.FirstOrDefault(cacheItem => TryFromCacheItem(cacheItem, out var _, useHardExpiration: true));
                        if (recoveredItem != null)
                        {
                            results[missedCacheItem.Key] = recoveredItem.Value;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    _logger?.LogWarning(ex, "A recoverable error occurred while fetching items after soft cache misses.");
                }
            }

            // Check for stale hits.
            var staleKeys = cacheItems
                .Where(cacheItemPair =>
                    cacheItemPair.Value.CacheItem != null
                    && IsStale(cacheItemPair.Value.CacheItem)
                )
                .Select(cacheItemPair => cacheItemPair.Key)
                .ToList();

            if (staleKeys.Any())
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

            return results;
        }

        public Task Remove(TKey key)
        {
            var keyString = FormatKey(key);

            if (EnableL1)
            {
                _l1Cache.Remove(keyString);
            }

            return EnableL2
                ? Task.WhenAll(_l2Caches.Select(c => c.Remove(keyString)))
                : Task.CompletedTask;
        }

        public Task Remove(IEnumerable<TKey> keys)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var keyStrings = keys
                .Select(FormatKey)
                .ToList();
            if (keyStrings.Count == 0)
            {
                return Task.CompletedTask;
            }

            if (EnableL1)
            {
                _l1Cache.Remove(keyStrings);
            }

            return EnableL2
                ? Task.WhenAll(_l2Caches.Select(c => c.Remove(keyStrings)))
                : Task.CompletedTask;
        }

        public Task Set(TKey key, T value)
        {
            var keyString = FormatKey(key);

            if (EnableL1)
            {
                SetL1(keyString, value);
            }

            return EnableL2
                ? SetL2(keyString, value)
                : Task.CompletedTask;
        }

        public Task Set(IEnumerable<KeyValuePair<TKey, T>> values)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var cachePairs = values
                .Select(valuePair => new KeyValuePair<string, T>(FormatKey(valuePair.Key), valuePair.Value))
                .ToList();
            if (cachePairs.Count == 0)
            {
                return Task.CompletedTask;
            }

            if (EnableL1)
            {
                SetL1(cachePairs);
            }

            return EnableL2
                ? SetL2(cachePairs)
                : Task.CompletedTask;
        }

        private string DefaultCacheName()
            => GetType().FullName;

        private async Task<IDictionary<TKey, GetCacheItemResult>> GetCacheItems(IEnumerable<TKey> keys)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var results = keys
                .Distinct()
                .ToDictionary(key => key, key => new GetCacheItemResult { CacheKey = FormatKey(key) });

            IList<GetCacheItemResult> getResultsToUpdate()
                => results.Values
                    .Where(result => result.CacheItem == null)
                    .ToList();

            IList<KeyValuePair<string, ICacheItem<T>>> getResultsToCopy(IEnumerable<GetCacheItemResult> updatedResults)
                => updatedResults
                    .Where(result => result.CacheItem != null)
                    .Select(result => new KeyValuePair<string, ICacheItem<T>>(result.CacheKey, result.CacheItem))
                    .ToList();

            IEnumerable<string> toCacheKeys(IEnumerable<GetCacheItemResult> sourceResults)
                => sourceResults
                    .Select(result => result.CacheKey);

            void update(IList<GetCacheItemResult> targetResults, IList<ICacheItem<T>> cacheItems)
            {
                for (int i = 0; i < cacheItems.Count; i++)
                {
                    var cacheItem = cacheItems[i];
                    if (cacheItem != null)
                    {
                        var result = targetResults[i];
                        result.AllCacheItems.Add(cacheItem);
                        if (TryFromCacheItem(cacheItem, out var _))
                        {
                            result.CacheItem = cacheItem;
                        }
                    }
                }
            }

            IList<GetCacheItemResult> resultsToUpdate;

            if (EnableL1 && (resultsToUpdate = getResultsToUpdate()).Any())
            {
                var cacheItems = _l1Cache.Get(toCacheKeys(resultsToUpdate));
                update(resultsToUpdate, cacheItems);
            }

            // Use L2 on L1 misses.
            if (EnableL2)
            {
                for (int cacheIndex = 0; (resultsToUpdate = getResultsToUpdate()).Any() && cacheIndex < _l2Caches.Count; cacheIndex++)
                {
                    var cacheItems = await _l2Caches[cacheIndex].Get(toCacheKeys(resultsToUpdate)).ConfigureAwait(false);
                    update(resultsToUpdate, cacheItems);

                    // Set previous L1/L2s.
                    IList<KeyValuePair<string, ICacheItem<T>>> resultsToCopy;
                    if ((EnableL1 || cacheIndex > 0) && (resultsToCopy = getResultsToCopy(resultsToUpdate)).Any())
                    {
                        if (EnableL1)
                        {
                            CopyL1(resultsToCopy);
                        }
                        if (cacheIndex > 0)
                        {
                            CopyL2(resultsToCopy, _l2Caches.Take(cacheIndex));
                        }
                    }
                }
            }

            return results;
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

            if ((EnableL1 || EnableL2) && IsCacheable(value))
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

            if (EnableL1 || EnableL2)
            {
                var valuesToSet = values
                    .Where(valuePair => IsCacheable(valuePair.Value))
                    .Select(valuePair => new KeyValuePair<string, T>(FormatKey(valuePair.Key), valuePair.Value))
                    .ToList();

                if (valuesToSet.Any())
                {
                    if (EnableL1)
                    {
                        SetL1(valuesToSet);
                    }
                    if (EnableL2)
                    {
                        if (setL2InBackground)
                        {
                            _ = Task.Run(() => SetL2(valuesToSet));
                        }
                        else
                        {
                            await SetL2(valuesToSet).ConfigureAwait(false);
                        }
                    }
                }
            }

            return values;
        }

        private void SetL1(string keyString, T value)
            => _l1Cache.Set(keyString, value);

        private void SetL1(IEnumerable<KeyValuePair<string, T>> values)
            => _l1Cache.Set(values);

        private void CopyL1(IEnumerable<KeyValuePair<string, ICacheItem<T>>> cacheItems)
            => _l1Cache.Set(cacheItems);

        private Task SetL2(string keyString, T value)
            => Task.WhenAll(_l2Caches.Select(c => c.Set(keyString, value)));

        private Task SetL2(IEnumerable<KeyValuePair<string, T>> values)
            => Task.WhenAll(_l2Caches.Select(c => c.Set(values)));

        private void CopyL2(IEnumerable<KeyValuePair<string, ICacheItem<T>>> cacheItems, IEnumerable<L2Cache<T>> caches)
            => Task.Run(() => Task.WhenAll(caches.Select(c => c.Set(cacheItems))));

        private static bool TryFromCacheItem(ICacheItem<T> cacheItem, out T value, bool useHardExpiration = false)
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
            else
            {
                value = default;
                return false;
            }
        }

        private bool IsCacheable(T value)
            => EnableNegativeCaching || !EqualityComparer<T>.Default.Equals(value, default);

        private bool IsStale(ICacheItem<T> cacheItem)
            => cacheItem.SoftExpiration - DateTime.UtcNow <= BackgroundFetchThreshold;

        private class GetCacheItemResult
        {
            public string CacheKey { get; set; }

            public ICacheItem<T> CacheItem { get; set; }

            public IList<ICacheItem<T>> AllCacheItems
            {
                get => _allCacheItems ?? (_allCacheItems = new List<ICacheItem<T>>());
                set => _allCacheItems = value;
            }
            private IList<ICacheItem<T>> _allCacheItems;
        }
    }
}
