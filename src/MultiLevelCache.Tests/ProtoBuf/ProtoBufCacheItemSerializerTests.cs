using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLevelCaching.ProtoBuf;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultiLevelCaching.Tests.ProtoBuf
{
    [TestClass]
    public class ProtoBufCacheItemSerializerTests
	{
        [TestMethod]
        public void Serialize_CacheItemString_Succeeds()
        {
            var cacheItem = new ExpiringCacheItem<string>
            {
                Value = "Hello World!",
                SoftExpiration = DateTime.UtcNow.AddMinutes(10),
                HardExpiration = DateTime.UtcNow.AddMinutes(20)
            };
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(cacheItem);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);
        }

        [TestMethod]
        public void Serialize_CacheItemInt_Succeeds()
        {
            var cacheItem = new ExpiringCacheItem<int>
            {
                Value = 0,
                SoftExpiration = DateTime.UtcNow.AddMinutes(10),
                HardExpiration = DateTime.UtcNow.AddMinutes(20)
            };
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(cacheItem);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);
        }

        [TestMethod]
        public void Serialize_CacheItemObject_Succeeds()
        {
            var cacheItem = new ExpiringCacheItem<CustomCacheItem>
            {
                Value = new CustomCacheItem
                {
                    Text = "Hello World!"
                },
                SoftExpiration = DateTime.UtcNow.AddMinutes(10),
                HardExpiration = DateTime.UtcNow.AddMinutes(20)
            };
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(cacheItem);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);
        }

        [TestMethod]
        public void Serialize_CacheItemObjectTwice_Succeeds()
        {
            var cacheItem = new ExpiringCacheItem<CustomCacheItem>
            {
                Value = new CustomCacheItem
                {
                    Text = "Hello World!"
                },
                SoftExpiration = DateTime.UtcNow.AddMinutes(10),
                HardExpiration = DateTime.UtcNow.AddMinutes(20)
            };
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(cacheItem);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);

            bytes = serializer.Serialize(cacheItem);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);
        }

        [TestMethod]
        public void Deserialize_CacheItem_Succeeds()
        {
            var cacheItem = new ExpiringCacheItem<CustomCacheItem>
            {
                Value = new CustomCacheItem
                {
                    Text = "Hello World!"
                },
                SoftExpiration = DateTime.UtcNow.AddMinutes(10),
                HardExpiration = DateTime.UtcNow.AddMinutes(20)
            };
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(cacheItem);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);

            var result = serializer.Deserialize<ExpiringCacheItem<CustomCacheItem>>(bytes);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(cacheItem.Value.Text, result.Value.Text);
            Assert.AreEqual(cacheItem.SoftExpiration, result.SoftExpiration);
            Assert.AreEqual(cacheItem.HardExpiration, result.HardExpiration);
            Assert.AreEqual(cacheItem.StaleTime, result.StaleTime);
        }

        [ProtoContract]
        public class CustomCacheItem
        {
            [ProtoMember(1)]
            public string Text { get; set; }
        }
    }
}
