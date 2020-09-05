using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiLevelCaching.Redis
{
    public class RedisCacheInvalidator : ICacheInvalidator
    {
        private readonly ConcurrentDictionary<IL1CacheProvider, byte> _caches = new ConcurrentDictionary<IL1CacheProvider, byte>();
        private readonly string _channel;
        private readonly ILogger<RedisCacheInvalidator> _logger;
        private readonly ISubscriber _subscriber;

        private Initializer _redisSubscriptionInitializer;
        private bool _redisSubscriptionInitialized;
        private object _redisSubscriptionInitializerLock = new object();

        public RedisCacheInvalidator(
            ISubscriber subscriber,
            string channel = "cache/invalidate",
            ILogger<RedisCacheInvalidator> logger = null)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _channel = channel;
            _logger = logger;
        }

        public void Publish(string key)
        {
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _subscriber.PublishAsync(_channel, key, CommandFlags.FireAndForget).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "An error occurred while publishing a cache invalidation to Redis. Key={Key}", key);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while publishing a cache invalidation to Redis. Key={Key}", key);
            }
        }

        public void Subscribe(IL1CacheProvider cache)
        {
            EnsureRedisSubscriptionInitialized();
            _caches.TryAdd(cache, default);
        }

        private void EnsureRedisSubscriptionInitialized()
        {
            LazyInitializer.EnsureInitialized(
                ref _redisSubscriptionInitializer,
                ref _redisSubscriptionInitialized,
                ref _redisSubscriptionInitializerLock,
                () => new Initializer(() =>
                {
                    try
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                await _subscriber.SubscribeAsync(_channel, (channel, key) =>
                                {
                                    foreach (var cache in _caches.Keys)
                                    {
                                        cache.Remove(key);
                                    }
                                }).ConfigureAwait(false);
                                _logger?.LogInformation("Subscribed to Redis cache invalidations. Channel={_channel}", _channel);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "An error occurred while subscribing to cache invalidations from Redis. Channel={Channel}", _channel);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "An error occurred while subscribing to cache invalidations from Redis. Channel={Channel}", _channel);
                    }
                })
            );
        }

        private class Initializer
        {
            public Initializer(Action init)
                => init();
        }
    }
}
