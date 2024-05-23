using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace MultiLevelCaching.Redis
{
    public class RedisCacheItemPublisherFactory : ICacheItemPublisherFactory
    {
        private readonly Func<Task<ISubscriber>> _subscriberFactory;
        private readonly ILoggerFactory _loggerFactory;

        public RedisCacheItemPublisherFactory(
            Func<Task<ISubscriber>> subscriberFactory,
            ILoggerFactory loggerFactory = null)
        {
            _subscriberFactory = subscriberFactory ?? throw new ArgumentNullException(nameof(subscriberFactory));
            _loggerFactory = loggerFactory;
        }

        public ICacheItemPublisher<T> NewPublisher<T>(
            string cacheName,
            CacheItemPublishMode publishMode,
            IL1CacheProvider cache,
            ICacheItemSerializer serializer)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                throw new ArgumentNullException(nameof(cacheName));
            }

            return new RedisCacheItemPublisher<T>(
                _subscriberFactory,
                ToChannel(cacheName),
                publishMode,
                cache ?? throw new ArgumentNullException(nameof(cache)),
                serializer ?? throw new ArgumentNullException(nameof(serializer)),
                _loggerFactory?.CreateLogger<RedisCacheItemPublisher<T>>()
            );
        }

        protected virtual string ToChannel(string cacheName)
            => $"CacheItemPublisher:{cacheName}";
    }
}
