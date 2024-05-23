using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiLevelCaching.Redis
{
    public class RedisL2CacheProvider : IL2CacheProvider
    {
        private readonly Func<Task<IDatabaseAsync>> _redisDbAsyncFactory;
        private Lazy<Task<IDatabaseAsync>> _redisDbAsyncLazy;

        public RedisL2CacheProvider(Func<Task<IDatabaseAsync>> redisDbAsyncFactory)
        {
            _redisDbAsyncFactory = redisDbAsyncFactory ?? throw new ArgumentNullException(nameof(redisDbAsyncFactory));

            InitializeRedisDbAsyncLazy();
        }

        public async Task<byte[]> Get(string key)
        {
            var redisDb = await GetRedisDb().ConfigureAwait(false);
            return await redisDb.StringGetAsync(key).ConfigureAwait(false);
        }

        public async Task<IList<byte[]>> Get(IEnumerable<string> keys)
        {
            var redisKeys = keys.Select(k => new RedisKey(k)).ToArray();
            if (redisKeys.Length == 0)
            {
                return Array.Empty<byte[]>();
            }

            var redisDb = await GetRedisDb().ConfigureAwait(false);
            var redisValues = await redisDb.StringGetAsync(redisKeys).ConfigureAwait(false);
            return redisValues
                .Select(v => v.IsNull ? null : (byte[])v)
                .ToList();
        }

        public async Task Remove(string key)
        {
            var redisDb = await GetRedisDb().ConfigureAwait(false);
            await redisDb.KeyDeleteAsync(key).ConfigureAwait(false);
        }

        public async Task Remove(IEnumerable<string> keys)
        {
            var redisKeys = keys.Select(k => new RedisKey(k)).ToArray();
            if (redisKeys.Length == 0)
            {
                return;
            }

            var redisDb = await GetRedisDb().ConfigureAwait(false);
            await redisDb.KeyDeleteAsync(redisKeys).ConfigureAwait(false);
        }

        public async Task Set(string key, byte[] value, TimeSpan duration)
        {
            var redisDb = await GetRedisDb().ConfigureAwait(false);
            await redisDb.StringSetAsync(key, value, expiry: duration).ConfigureAwait(false);
        }

        public async Task Set(IEnumerable<KeyValuePair<string, byte[]>> values, TimeSpan duration)
        {
            var redisDb = await GetRedisDb().ConfigureAwait(false);
            // Sadly, Redis doesn't support MSET with expiry.
            await Task.WhenAll(values.Select(valuePair => redisDb.StringSetAsync(valuePair.Key, valuePair.Value, expiry: duration))).ConfigureAwait(false);
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
