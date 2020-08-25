using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
            var value = Array.Empty<CustomCacheItem>();
            var cacheItemBytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Mock.Get(l2Provider)
                .Setup(p => p.Get("1"))
                .ReturnsAsync(cacheItemBytes);
            var settings = new MultiLevelCacheSettings<int>(
                key => key.ToString(),
                l2settings: new L2CacheSettings(l2Provider, serializer, new TimeSpan(0, 10, 0))
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
            var value = Array.Empty<CustomCacheItem>();
            var cacheItemBytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
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
            var value = (string)null;
            var cacheItemBytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
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
