using Microsoft.Extensions.Logging;
using MultiLevelCaching.Memory;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MultiLevelCaching.Tests
{
    [TestClass]
    public class CacheTests
    {
        [TestMethod]
        public async Task GetOrAdd_ManyRequests_Succeeds()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var l1Provider = new MemoryL1CacheProvider();

            var settings = new MultiLevelCacheSettings
            {
                L1Settings = new L1CacheSettings
                {
                    Provider = l1Provider,
                    SoftDuration = new TimeSpan(0, 0, 1)
                },
                BackgroundFetchThreshold = new TimeSpan(0, 0, 0, 0, 500)
            };
            var cache = new TestCache<int, string>(settings, loggerFactory);

            var db = new Dictionary<int, string>(Enumerable.Range(0, 1000)
                .Select(key => new KeyValuePair<int, string>(key, key.ToString()))
            );

            Enumerable.Range(0, 10)
                .InvokeThreads(threadIdObject =>
                {
                    int key;
                    string value;

                    for (int i = 0; i < 1000; i++)
                    {
                        key = Random.Shared.Next(db.Count);
                        value = cache.GetOrAdd(key, (_key, fetchContext) => Fetch(_key, fetchContext, db, millisecondsDelay: 10)).Result;
                        Assert.AreEqual(key.ToString(), value);
                    }
                });

            await Task.Delay(1000);

            Enumerable.Range(0, 10)
                .InvokeThreads(threadIdObject =>
                {
                    int key;

                    for (int i = 0; i < 100; i++)
                    {
                        key = ((int)threadIdObject * 100) + i;
                        cache.Remove(key).Wait();
                    }
                });

            await Task.Delay(1000);
        }

        [TestMethod]
        public async Task GetOrAddMany_ManyRequests_Succeeds()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var l1Provider = new MemoryL1CacheProvider();

            var settings = new MultiLevelCacheSettings
            {
                L1Settings = new L1CacheSettings
                {
                    Provider = l1Provider,
                    SoftDuration = new TimeSpan(0, 0, 1)
                },
                BackgroundFetchThreshold = new TimeSpan(0, 0, 0, 0, 500)
            };
            var cache = new TestCache<int, string>(settings, loggerFactory);

            var db = new Dictionary<int, string>(Enumerable.Range(0, 1000)
                .Select(key => new KeyValuePair<int, string>(key, key.ToString()))
            );

            Enumerable.Range(0, 10)
                .InvokeThreads(threadIdObject =>
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        var keys = Enumerable.Range(0, Random.Shared.Next(100))
                            .Select(_ => Random.Shared.Next(db.Count))
                            .Distinct()
                            .ToList();
                        var values = cache.GetOrAdd(keys, (_keys, fetchContext) => Fetch(_keys, fetchContext, db, millisecondsDelay: 20)).Result;
                        Assert.IsNotNull(values);
                        Assert.AreEqual(keys.Count, values.Count);
                        foreach (var key in keys)
                        {
                            Assert.IsTrue(values.TryGetValue(key, out var value));
                            Assert.AreEqual(key.ToString(), value);
                        }
                    }
                });

            await Task.Delay(1000);

            await cache.Remove(Enumerable.Range(0, 1000));
        }

        private static async Task<T> Fetch<TKey, T>(TKey key, FetchContext fetchContext, IReadOnlyDictionary<TKey, T> db, ILogger logger = null, int millisecondsDelay = 0)
        {
            if (millisecondsDelay > 0)
            {
                await Task.Delay(millisecondsDelay);
            }

            logger?.LogInformation("Fetch Key={Key}, IsBackground={IsBackground}, IsRecoverable={IsRecoverable}", key, fetchContext.IsBackground, fetchContext.IsRecoverable);

            return db.GetValueOrDefault(key);
        }

        private static async Task<IDictionary<TKey, T>> Fetch<TKey, T>(ICollection<TKey> keys, FetchContext fetchContext, IReadOnlyDictionary<TKey, T> db, ILogger logger = null, int millisecondsDelay = 0)
        {
            if (millisecondsDelay > 0)
            {
                await Task.Delay(millisecondsDelay);
            }

            logger?.LogInformation("Fetch KeyCount={KeyCount}, IsBackground={IsBackground}, IsRecoverable={IsRecoverable}", keys.Count, fetchContext.IsBackground, fetchContext.IsRecoverable);

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
                ILoggerFactory loggerFactory = null)
                : base(settings, loggerFactory)
            { }

            protected override string FormatKey(TKey key)
                => $"{CacheName}:{key}";
        }
    }
}
