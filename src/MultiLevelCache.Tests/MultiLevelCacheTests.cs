using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLevelCaching.Memory;
using MultiLevelCaching.ProtoBuf;
using ProtoBuf;
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
            var cacheItem = new ExpiringCacheItem<ICollection<CustomCacheItem>>
            {
                Value = Array.Empty<CustomCacheItem>(),
                SoftExpiration = DateTime.UtcNow.AddDays(1),
                HardExpiration = DateTime.UtcNow.AddDays(2)
            };
            var cacheItemBytes = serializer.Serialize(cacheItem);
            Mock.Get(l2Provider)
                .Setup(p => p.Get("1"))
                .ReturnsAsync(cacheItemBytes);
            var settings = new MultiLevelCacheSettings<int>(
                key => key.ToString(),
                l2settings: new L2CacheSettings(l2Provider, serializer, new TimeSpan(1, 0, 0))
            );
            var cache = new MultiLevelCache<int, ICollection<CustomCacheItem>>(settings);

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
            var cacheItem = new ExpiringCacheItem<ICollection<CustomCacheItem>>
            {
                Value = Array.Empty<CustomCacheItem>(),
                SoftExpiration = DateTime.UtcNow.AddDays(1),
                HardExpiration = DateTime.UtcNow.AddDays(2)
            };
            var cacheItemBytes = serializer.Serialize(cacheItem);
            Mock.Get(l2Provider)
                .Setup(p => p.Get("1"))
                .ReturnsAsync(cacheItemBytes);
            var settings = new MultiLevelCacheSettings<int>(
                key => key.ToString(),
                l2settings: new L2CacheSettings(l2Provider, serializer, new TimeSpan(1, 0, 0))
            )
            {
                EnableEmptyCollectionOnNull = false
            };
            var cache = new MultiLevelCache<int, ICollection<CustomCacheItem>>(settings);

            var result = await cache.GetOrAdd(1, key => throw new Exception());
            Assert.IsNull(result);
            Mock.Get(l2Provider).Verify(p => p.Get("1"));
        }

        [TestMethod]
        public async Task GetOrAdd_EmptyCollectionOnNullString_Succeeds()
        {
            var l2Provider = Mock.Of<IL2CacheProvider>();
            var serializer = new ProtoBufCacheItemSerializer();
            var cacheItem = new ExpiringCacheItem<string>
            {
                Value = null,
                SoftExpiration = DateTime.UtcNow.AddDays(1),
                HardExpiration = DateTime.UtcNow.AddDays(2)
            };
            var cacheItemBytes = serializer.Serialize(cacheItem);
            Mock.Get(l2Provider)
                .Setup(p => p.Get("1"))
                .ReturnsAsync(cacheItemBytes);
            var settings = new MultiLevelCacheSettings<int>(
                key => key.ToString(),
                l2settings: new L2CacheSettings(l2Provider, serializer, new TimeSpan(1, 0, 0))
            );
            var cache = new MultiLevelCache<int, string>(settings);

            var result = await cache.GetOrAdd(1, key => throw new Exception());
            Assert.IsNull(result);
            Mock.Get(l2Provider).Verify(p => p.Get("1"));
        }

        [ProtoContract]
        public class CustomCacheItem
        {
            [ProtoMember(1)]
            public string Text { get; set; }
        }
    }
}
