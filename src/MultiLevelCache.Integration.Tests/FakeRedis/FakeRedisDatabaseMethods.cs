using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiLevelCaching.Integration.Tests.FakeRedis
{
    public partial class FakeRedisDatabase
    {
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());
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

        public async Task<long> KeyDeleteAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            if (_millisecondsDelay > 0)
            {
                await Task.Delay(_millisecondsDelay).ConfigureAwait(false);
            }

            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
            return keys.LongLength;
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

            return keys
                .Select(key => (RedisValue)(_cache.Get(key) ?? RedisValue.Null))
                .ToArray();
        }

        public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when)
            => StringSetAsync(key, value, expiry: expiry, keepTtl: false, when: when, flags: CommandFlags.None);

        public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags)
            => StringSetAsync(key, value, expiry: expiry, keepTtl: false, when: when, flags: flags);

        public async Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false, When when = When.Always, CommandFlags flags = CommandFlags.None)
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

        public async Task<bool> StringSetAsync(KeyValuePair<RedisKey, RedisValue>[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            if (_millisecondsDelay > 0)
            {
                await Task.Delay(_millisecondsDelay).ConfigureAwait(false);
            }

            foreach (var valuePair in values)
            {
                _cache.Set(valuePair.Key, valuePair.Value);
            }
            return true;
        }
    }
}
