using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLevelCache.Tests.Redis;
using MultiLevelCaching.Memory;
using MultiLevelCaching.ProtoBuf;
using MultiLevelCaching.Redis;
using ProtoBuf;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultiLevelCaching.Tests
{
    [TestClass]
    public class MultiLevelCacheTests
    {
        [TestMethod]
        public async Task GetOrAdd_EmptyCollectionOnNull_Succeeds()
        {
            var l2Provider = Mock.Of<IL2CacheProvider>();
            var serializer = new ProtoBufCacheItemSerializer();
            var value = Array.Empty<CustomCacheItem>();
            var cacheItemBytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Mock.Get(l2Provider)
                .Setup(p => p.Get("1"))
                .ReturnsAsync(cacheItemBytes);
            var settings = new MultiLevelCacheSettings
            {
                L2Settings = new L2CacheSettings
                {
                    Provider = l2Provider,
                    Serializer = serializer,
                    SoftDuration = new TimeSpan(0, 10, 0)
                }
            };
            var cache = new TestCache<int, ICollection<CustomCacheItem>>(settings);

            var result = await cache.GetOrAdd(1, key => throw new Exception());
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Mock.Get(l2Provider).Verify(p => p.Get("1"));
        }

        [TestMethod]
        public async Task GetOrAdd_EmptyCollectionOnNullDisabled_Succeeds()
        {
            var l2Provider = Mock.Of<IL2CacheProvider>();
            var serializer = new ProtoBufCacheItemSerializer();
            var value = Array.Empty<CustomCacheItem>();
            var cacheItemBytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Mock.Get(l2Provider)
                .Setup(p => p.Get("1"))
                .ReturnsAsync(cacheItemBytes);
            var settings = new MultiLevelCacheSettings
            {
                L2Settings = new L2CacheSettings
                {
                    Provider = l2Provider,
                    Serializer = serializer,
                    SoftDuration = new TimeSpan(0, 10, 0)
                },
                EnableEmptyCollectionOnNull = false
            };
            var cache = new TestCache<int, ICollection<CustomCacheItem>>(settings);

            var result = await cache.GetOrAdd(1, key => throw new Exception());
            Assert.IsNull(result);
            Mock.Get(l2Provider).Verify(p => p.Get("1"));
        }

        [TestMethod]
        public async Task GetOrAdd_EmptyCollectionOnNullString_Succeeds()
        {
            var l2Provider = Mock.Of<IL2CacheProvider>();
            var serializer = new ProtoBufCacheItemSerializer();
            var value = (string)null;
            var cacheItemBytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Mock.Get(l2Provider)
                .Setup(p => p.Get("1"))
                .ReturnsAsync(cacheItemBytes);
            var settings = new MultiLevelCacheSettings
            {
                L2Settings = new L2CacheSettings
                {
                    Provider = l2Provider,
                    Serializer = serializer,
                    SoftDuration = new TimeSpan(0, 10, 0)
                }
            };
            var cache = new TestCache<int, string>(settings);

            var result = await cache.GetOrAdd(1, key => throw new Exception());
            Assert.IsNull(result);
            Mock.Get(l2Provider).Verify(p => p.Get("1"));
        }

        [TestMethod]
        public void GetOrAdd_ManyRequests_Succeeds()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var l1Provider = new MemoryL1CacheProvider(logger: loggerFactory.CreateLogger<MemoryL1CacheProvider>());
            var subscriber = new FakeRedisSubscriber();
            var invalidator = new RedisCacheInvalidator(() => Task.FromResult<ISubscriber>(subscriber), logger: loggerFactory.CreateLogger<RedisCacheInvalidator>());
            var redisDb = new FakeRedisDatabase(millisecondsDelay: 1);
            var l2Provider = new RedisL2CacheProvider(() => Task.FromResult<IDatabaseAsync>(redisDb), logger: loggerFactory.CreateLogger<RedisL2CacheProvider>());
            var serializer = new ProtoBufCacheItemSerializer(logger: loggerFactory.CreateLogger<ProtoBufCacheItemSerializer>());
            var settings = new MultiLevelCacheSettings
            {
                L1Settings = new L1CacheSettings
                {
                    Provider = l1Provider,
                    Invalidator = invalidator,
                    SoftDuration = new TimeSpan(0, 0, 10)
                },
                L2Settings = new L2CacheSettings
                {
                    Provider = l2Provider,
                    Serializer = serializer,
                    SoftDuration = new TimeSpan(0, 0, 20)
                },
                BackgroundFetchThreshold = new TimeSpan(0, 0, 5)
            };
            var cache = new TestCache<int, string>(settings, logger: loggerFactory.CreateLogger<TestCache<int, string>>());

            var db = new Dictionary<int, string>(Enumerable.Range(0, 1000)
                .Select(key => new KeyValuePair<int, string>(key, key.ToString()))
            );

            Enumerable.Range(0, 10)
                .InvokeThreads(threadIdObject =>
                {
                    var threadId = (int)threadIdObject;
                    var random = new Random(threadId);

                    for (int i = 0; i < 1000; i++)
                    {
                        var key = random.Next(db.Count);
                        var value = cache.GetOrAdd(key, k => Fetch(k, db, millisecondsDelay: 10)).Result;
                        Assert.AreEqual(key.ToString(), value);
                    }
                });
        }

        [TestMethod]
        public void GetOrAddMany_ManyRequests_Succeeds()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var l1Provider = new MemoryL1CacheProvider(logger: loggerFactory.CreateLogger<MemoryL1CacheProvider>());
            var subscriber = new FakeRedisSubscriber();
            var invalidator = new RedisCacheInvalidator(() => Task.FromResult<ISubscriber>(subscriber), logger: loggerFactory.CreateLogger<RedisCacheInvalidator>());
            var redisDb = new FakeRedisDatabase(millisecondsDelay: 1);
            var l2Provider = new RedisL2CacheProvider(() => Task.FromResult<IDatabaseAsync>(redisDb), logger: loggerFactory.CreateLogger<RedisL2CacheProvider>());
            var serializer = new ProtoBufCacheItemSerializer(logger: loggerFactory.CreateLogger<ProtoBufCacheItemSerializer>());
            var settings = new MultiLevelCacheSettings
            {
                L1Settings = new L1CacheSettings
                {
                    Provider = l1Provider,
                    Invalidator = invalidator,
                    SoftDuration = new TimeSpan(0, 0, 10)
                },
                L2Settings = new L2CacheSettings
                {
                    Provider = l2Provider,
                    Serializer = serializer,
                    SoftDuration = new TimeSpan(0, 0, 20)
                },
                BackgroundFetchThreshold = new TimeSpan(0, 0, 5)
            };
            var cache = new TestCache<int, string>(settings, logger: loggerFactory.CreateLogger<TestCache<int, string>>());

            var db = new Dictionary<int, string>(Enumerable.Range(0, 1000)
                .Select(key => new KeyValuePair<int, string>(key, key.ToString()))
            );

            Enumerable.Range(0, 10)
                .InvokeThreads(threadIdObject =>
                {
                    var threadId = (int)threadIdObject;
                    var random = new Random(threadId);

                    for (int i = 0; i < 1000; i++)
                    {
                        var keys = Enumerable.Range(0, random.Next(100))
                            .Select(_ => random.Next(db.Count))
                            .Distinct()
                            .ToList();
                        var values = cache.GetOrAdd(keys, k => Fetch(k, db, millisecondsDelay: 10)).Result;
                        Assert.IsNotNull(values);
                        Assert.AreEqual(keys.Count, values.Count);
                        foreach (var key in keys)
                        {
                            Assert.IsTrue(values.TryGetValue(key, out var value));
                            Assert.AreEqual(key.ToString(), value);
                        }
                    }
                });
        }

        private async Task<T> Fetch<TKey, T>(TKey key, IReadOnlyDictionary<TKey, T> db, int millisecondsDelay = 0)
        {
            if (millisecondsDelay > 0)
            {
                await Task.Delay(millisecondsDelay);
            }

            return db.GetValueOrDefault(key);
        }

        private async Task<IDictionary<TKey, T>> Fetch<TKey, T>(ICollection<TKey> keys, IReadOnlyDictionary<TKey, T> db, int millisecondsDelay = 0)
        {
            if (millisecondsDelay > 0)
            {
                await Task.Delay(millisecondsDelay);
            }

            var values = new Dictionary<TKey, T>(keys.Count);
            foreach (var key in keys)
            {
                var value = db.GetValueOrDefault(key);
                values[key] = value;
            }

            return values;
        }

        private class TestCache<TKey, T> : MultiLevelCache<TKey, T>
        {
            public TestCache(
                MultiLevelCacheSettings settings,
                ILogger<TestCache<TKey, T>> logger = null)
                : base(settings, logger)
            { }

            protected override string FormatKey(TKey key)
                => key.ToString();
        }

        [ProtoContract]
        public class CustomCacheItem
        {
            [ProtoMember(1)]
            public string Text { get; set; }
        }
    }
}
