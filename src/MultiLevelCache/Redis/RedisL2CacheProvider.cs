using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiLevelCaching.Redis
{
    public class RedisL2CacheProvider : IL2CacheProvider
    {
        private readonly ILogger<RedisL2CacheProvider> _logger;
        private readonly Func<Task<IDatabaseAsync>> _redisDbAsyncFactory;
        private Lazy<Task<IDatabaseAsync>> _redisDbAsyncLazy;

        public RedisL2CacheProvider(
            Func<Task<IDatabaseAsync>> redisDbAsyncFactory,
            ILogger<RedisL2CacheProvider> logger = null)
        {
            _logger = logger;
            _redisDbAsyncFactory = redisDbAsyncFactory ?? throw new ArgumentNullException(nameof(redisDbAsyncFactory));

            InitializeRedisDbAsyncLazy();
        }

        public async Task<byte[]> Get(string key)
        {
            try
            {
                var redisDb = await GetRedisDb().ConfigureAwait(false);
                return await redisDb.StringGetAsync(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while getting an item from Redis. Key={Key}", key);
                return null;
            }
        }

        public async Task<IList<byte[]>> Get(IEnumerable<string> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var redisKeys = keys.Select(k => new RedisKey(k)).ToArray();
            if (redisKeys.Length == 0)
            {
                return Array.Empty<byte[]>();
            }

            try
            {
                var redisDb = await GetRedisDb().ConfigureAwait(false);
                var redisValues = await redisDb.StringGetAsync(redisKeys).ConfigureAwait(false);
                var values = redisValues
                    .Select(v => v.IsNull ? null : (byte[])v)
                    .ToList();
                return values;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while getting items from Redis.");
                return Array.Empty<byte[]>();
            }
        }

        public async Task Remove(string key)
        {
            try
            {
                var redisDb = await GetRedisDb().ConfigureAwait(false);
                await redisDb.KeyDeleteAsync(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while removing an item from Redis. Key={Key}", key);
            }
        }

        public async Task Set(string key, byte[] value, TimeSpan duration)
        {
            try
            {
                var redisDb = await GetRedisDb().ConfigureAwait(false);
                await redisDb.StringSetAsync(key, value, expiry: duration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting an item in Redis. Key={Key}", key);
            }
        }

        private void InitializeRedisDbAsyncLazy()
        {
            _redisDbAsyncLazy = new Lazy<Task<IDatabaseAsync>>(async () =>
            {
                try
                {
                    return await _redisDbAsyncFactory().ConfigureAwait(false);
                }
                catch
                {
                    InitializeRedisDbAsyncLazy();
                    throw;
                }
            });
        }

        private Task<IDatabaseAsync> GetRedisDb()
            => _redisDbAsyncLazy.Value;

    }
}
