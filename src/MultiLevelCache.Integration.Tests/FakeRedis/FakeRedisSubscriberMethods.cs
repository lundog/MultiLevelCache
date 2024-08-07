﻿using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MultiLevelCaching.Integration.Tests.FakeRedis
{
    public partial class FakeRedisSubscriber
    {
        private readonly ConcurrentDictionary<RedisChannel, ConcurrentBag<Action<RedisChannel, RedisValue>>> _handlersByChannel = new();

        public Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
        {
            long clients = 0;
            if (_handlersByChannel.TryGetValue(channel, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    clients++;
                    handler(channel, message);
                }
            }
            return Task.FromResult(clients);
        }

        public Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None)
        {
            var handlers = _handlersByChannel.GetOrAdd(channel, _ => new ConcurrentBag<Action<RedisChannel, RedisValue>>());
            handlers.Add(handler);
            return Task.CompletedTask;
        }
    }
}
