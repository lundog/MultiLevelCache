using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiLevelCaching
{
    internal class L1Cache<T>
    {
        private TimeSpan SoftDuration { get; }
        private TimeSpan HardDuration { get; }
        private CacheItemPublishMode PublishMode { get; }

        private readonly IL1CacheProvider _provider;
        private readonly ICacheItemPublisher<T> _publisher;
        private readonly ILogger<L1Cache<T>> _logger;

        public L1Cache(
            string cacheName,
            L1CacheSettings settings,
            ILogger<L1Cache<T>> logger = null)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (settings.HardDuration < settings.SoftDuration)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.HardDuration), $"{nameof(settings.HardDuration)} must be greater than or equal to {nameof(settings.SoftDuration)}.");
            }

            _provider = settings.Provider ?? throw new ArgumentException($"If {nameof(MultiLevelCacheSettings.L1Settings)} is set, {nameof(settings.Provider)} is required.", nameof(settings.Provider));
            SoftDuration = settings.SoftDuration;
            HardDuration = settings.HardDuration ?? new TimeSpan(settings.SoftDuration.Ticks * 2);
            _logger = logger;

            if (settings.PublishSettings?.PublisherFactory != null
                && settings.PublishSettings?.PublishMode != CacheItemPublishMode.Disabled)
            {
                _publisher = settings.PublishSettings.PublisherFactory.NewPublisher<T>(
                    cacheName,
                    settings.PublishSettings.PublishMode,
                    settings.Provider,
                    settings.PublishSettings.Serializer ?? throw new ArgumentException($"If {nameof(L1CacheSettings.PublishSettings)} is set, {nameof(settings.PublishSettings.Serializer)} is required.", nameof(settings.PublishSettings.Serializer))
                );
                PublishMode = settings.PublishSettings.PublishMode;
            }
            else
            {
                PublishMode = CacheItemPublishMode.Disabled;
            }
        }

        public ICacheItem<T> Get(string key)
        {
            try
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                return _provider.Get<T>(key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while getting an item from L1 cache. Key={Key}", key);
                return default;
            }
        }

        public IList<ICacheItem<T>> Get(IEnumerable<string> keys)
        {
            try
            {
                if (keys is null)
                {
                    throw new ArgumentNullException(nameof(keys));
                }

                return keys
                    .Select(Get)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while getting items from L1 cache.");
                return Array.Empty<ICacheItem<T>>();
            }
        }

        public void Remove(string key)
        {
            try
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                _provider.Remove(key);

                if (PublishMode.IsPublishEnabled())
                {
                    _publisher?.PublishRemove(key);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while removing an item from L1 cache. Key={Key}", key);
            }
        }

        public void Remove(IEnumerable<string> keys)
        {
            try
            {
                if (keys is null)
                {
                    throw new ArgumentNullException(nameof(keys));
                }

                foreach (var key in keys)
                {
                    Remove(key);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while removing items from L1 cache.");
            }
        }

        public void Set(string key, T value)
            => SetInner(key, value);

        public void Set(IEnumerable<KeyValuePair<string, T>> values)
        {
            try
            {
                if (values is null)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                foreach (var valuePair in values)
                {
                    SetInner(valuePair.Key, valuePair.Value);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting items in L1 cache.");
            }
        }

        public void Set(IEnumerable<KeyValuePair<string, ICacheItem<T>>> values)
        {
            try
            {
                if (values is null)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                foreach (var valuePair in values)
                {
                    SetInner(valuePair.Key, valuePair.Value.Value, valuePair.Value.SoftExpiration, valuePair.Value.HardExpiration);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting items in L1 cache.");
            }
        }

        private void SetInner(string key, T value, DateTime? softExpirationLimit = null, DateTime? hardExpirationLimit = null)
        {
            try
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                var softExpiration = ToSoftExpiration(softExpirationLimit);
                var hardExpiration = ToHardExpiration(hardExpirationLimit);

                _provider.Set(
                    key,
                    value,
                    softExpiration: softExpiration,
                    hardExpiration: hardExpiration
                );

                if (PublishMode.IsPublishEnabled())
                {
                    _publisher?.PublishSet(
                        key,
                        value,
                        softExpiration: softExpiration,
                        hardExpiration: hardExpiration
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting an item in L1 cache. Key={Key}", key);
            }
        }

        private DateTime ToSoftExpiration(DateTime? limit = null)
            => DateTime.UtcNow.Add(SoftDuration).Min(limit);

        private DateTime ToHardExpiration(DateTime? limit = null)
            => DateTime.UtcNow.Add(HardDuration).Min(limit);
    }
}
