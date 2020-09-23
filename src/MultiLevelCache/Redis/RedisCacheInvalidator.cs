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
        private readonly Func<Task<ISubscriber>> _subscriberAsyncFactory;
        private Lazy<Task<ISubscriber>> _subscriberAsyncLazy;

        private Initializer _redisSubscriptionInitializer;
        private bool _redisSubscriptionInitialized;
        private object _redisSubscriptionInitializerLock = new object();

        public RedisCacheInvalidator(
            Func<Task<ISubscriber>> subscriberAsyncFactory,
            string channel = "cache/invalidate",
            ILogger<RedisCacheInvalidator> logger = null)
        {
            _subscriberAsyncFactory = subscriberAsyncFactory ?? throw new ArgumentNullException(nameof(subscriberAsyncFactory));
            _channel = channel;
            _logger = logger;

            InitializeSubscriberAsyncLazy();
        }

        public void Publish(string key)
        {
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var subscriber = await GetSubscriber().ConfigureAwait(false);
                        await subscriber.PublishAsync(_channel, key, CommandFlags.FireAndForget).ConfigureAwait(false);
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
                                var subscriber = await GetSubscriber().ConfigureAwait(false);
                                await subscriber.SubscribeAsync(_channel, (channel, key) =>
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

        private void InitializeSubscriberAsyncLazy()
        {
            _subscriberAsyncLazy = new Lazy<Task<ISubscriber>>(async () =>
            {
                try
                {
                    return await _subscriberAsyncFactory().ConfigureAwait(false);
                }
                catch
                {
                    InitializeSubscriberAsyncLazy();
                    throw;
                }
            });
        }

        private Task<ISubscriber> GetSubscriber()
            => _subscriberAsyncLazy.Value;

        private class Initializer
        {
            public Initializer(Action init)
                => init();
        }
    }
}
