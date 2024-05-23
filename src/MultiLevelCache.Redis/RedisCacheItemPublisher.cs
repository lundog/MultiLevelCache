using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiLevelCaching.Redis
{
    internal class RedisCacheItemPublisher<T> : ICacheItemPublisher<T>
    {
        private readonly IL1CacheProvider _cache;
        private readonly RedisChannel _channel;
        private readonly ILogger<RedisCacheItemPublisher<T>> _logger;
        private readonly ICacheItemSerializer _serializer;
        private readonly Func<Task<ISubscriber>> _subscriberFactory;
        private Lazy<Task<ISubscriber>> _subscriberLazy;
        private Lazy<Task> _subscribeLazy;

        public RedisCacheItemPublisher(
            Func<Task<ISubscriber>> subscriberFactory,
            string channel,
            CacheItemPublishMode publishMode,
            IL1CacheProvider cache,
            ICacheItemSerializer serializer,
            ILogger<RedisCacheItemPublisher<T>> logger = null)
        {
            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentNullException(nameof(channel));
            }

            _subscriberFactory = subscriberFactory ?? throw new ArgumentNullException(nameof(subscriberFactory));
            _channel = RedisChannel.Literal(channel);
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger;

            InitializeSubscriberLazy();

            if (publishMode.IsSubscribeEnabled())
            {
                InitializeSubscribeLazy();
                EnsureSubscribed();
            }
        }

        public void PublishRemove(string key)
        {
            try
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                var bytes = _serializer.SerializeRemoveMessage<T>(key);
                if (bytes == null)
                {
                    return;
                }

                PublishInner(bytes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while publishing a cache invalidation to Redis. Channel={Channel}, Key={Key}", _channel, key);
            }
        }

        public void PublishSet(string key, T value, DateTime softExpiration, DateTime hardExpiration)
        {
            try
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                var bytes = _serializer.SerializeSetMessage(
                    key,
                    value,
                    softExpiration: softExpiration,
                    hardExpiration: hardExpiration
                );
                if (bytes == null)
                {
                    return;
                }

                PublishInner(bytes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while publishing a cache item to Redis. Channel={Channel}, Key={Key}", _channel, key);
            }
        }

        private void InitializeSubscriberLazy()
        {
            _subscriberLazy = new Lazy<Task<ISubscriber>>(async () =>
            {
                try
                {
                    return await _subscriberFactory().ConfigureAwait(false);
                }
                catch
                {
                    InitializeSubscriberLazy();
                    throw;
                }
            });
        }

        private void InitializeSubscribeLazy()
        {
            _subscribeLazy = new Lazy<Task>(async () =>
            {
                try
                {
                    var subscriber = await GetSubscriber().ConfigureAwait(false);
                    await subscriber.SubscribeAsync(_channel, (channel, value) => HandleMessage(value)).ConfigureAwait(false);
                    _logger?.LogInformation("Subscribed to Redis cache messages. Channel={_channel}", _channel);
                }
                catch
                {
                    InitializeSubscribeLazy();
                    throw;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private Task<ISubscriber> GetSubscriber()
            => _subscriberLazy.Value;

        private void EnsureSubscribed()
        {
            try
            {
                var subscribeTask = GetSubscribeTask();
                if (!subscribeTask.IsCompleted)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await subscribeTask.ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "An error occurred while subscribing to Redis cache messages. Channel={Channel}", _channel);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while subscribing to Redis cache messages. Channel={Channel}", _channel);
            }
        }

        private Task GetSubscribeTask()
            => _subscribeLazy.Value;

        private void HandleMessage(byte[] bytes)
        {
            if (bytes == null)
            {
                return;
            }

            try
            {
                var message = _serializer.DeserializeMessage<T>(bytes);
                if (message?.Key == null)
                {
                    return;
                }

                if (message.CacheItem != null)
                {
                    _cache.Set(
                        message.Key,
                        message.CacheItem.Value,
                        softExpiration: message.CacheItem.SoftExpiration,
                        hardExpiration: message.CacheItem.HardExpiration
                    );
                }
                else
                {
                    _cache.Remove(message.Key);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while processing a Redis cache message. Channel={Channel}", _channel);
            }
        }

        private void PublishInner(byte[] bytes)
        {
            try
            {
                if (bytes is null)
                {
                    throw new ArgumentNullException(nameof(bytes));
                }

                Task.Run(async () =>
                {
                    try
                    {
                        var subscriber = await GetSubscriber().ConfigureAwait(false);
                        await subscriber.PublishAsync(_channel, bytes, CommandFlags.FireAndForget).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "An error occurred while publishing a cache item or message to Redis. Channel={Channel}", _channel);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while publishing a cache item or message to Redis. Channel={Channel}", _channel);
            }
        }
    }
}
