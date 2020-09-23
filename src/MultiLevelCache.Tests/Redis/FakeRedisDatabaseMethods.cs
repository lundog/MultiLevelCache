using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MultiLevelCache.Tests.Redis
{
    public partial class FakeRedisDatabase
    {
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly int _millisecondsDelay;

        public FakeRedisDatabase(
            int millisecondsDelay = 0)
        {
            _millisecondsDelay = millisecondsDelay;
        }

        public async Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            if (_millisecondsDelay > 0)
            {
                await Task.Delay(_millisecondsDelay).ConfigureAwait(false);
            }

            _cache.Remove(key);
            return true;
        }

        public async Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            if (_millisecondsDelay > 0)
            {
                await Task.Delay(_millisecondsDelay).ConfigureAwait(false);
            }

            return (RedisValue)(_cache.Get(key) ?? RedisValue.Null);
        }

        public async Task<RedisValue[]> StringGetAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            if (_millisecondsDelay > 0)
            {
                await Task.Delay(_millisecondsDelay).ConfigureAwait(false);
            }

            var values = keys
                .Select(key => (RedisValue)(_cache.Get(key) ?? RedisValue.Null))
                .ToArray();
            return values;
        }

        public async Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            if (_millisecondsDelay > 0)
            {
                await Task.Delay(_millisecondsDelay).ConfigureAwait(false);
            }

            if (expiry.HasValue)
            {
                _cache.Set(key, value, expiry.Value);
            }
            else
            {
                _cache.Set(key, value);
            }
            return true;
        }
    }
}
