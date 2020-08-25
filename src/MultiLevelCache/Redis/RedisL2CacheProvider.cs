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
        private readonly IDatabaseAsync _redisDb;

        public RedisL2CacheProvider(
            IDatabaseAsync redisDb,
            ILogger<RedisL2CacheProvider> logger = null)
        {
            _logger = logger;
            _redisDb = redisDb ?? throw new ArgumentNullException(nameof(redisDb));
        }

        public async Task<byte[]> Get(string key)
        {
            try
            {
                return await _redisDb.StringGetAsync(key).ConfigureAwait(false);
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

            var redisKeys = keys.Cast<RedisKey>().ToArray();
            if (redisKeys.Length == 0)
            {
                return Array.Empty<byte[]>();
            }

            try
            {
                var redisValues = await _redisDb.StringGetAsync(redisKeys).ConfigureAwait(false);
                var values = redisValues
                    .Cast<byte[]>()
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
                await _redisDb.KeyDeleteAsync(key).ConfigureAwait(false);
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
                await _redisDb.StringSetAsync(key, value, expiry: duration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while setting an item in Redis. Key={Key}", key);
            }
        }
    }
}
