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
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize("Hello World!", DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);
        }

        [TestMethod]
        public void Serialize_CacheItemInt_Succeeds()
        {
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(100, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);
        }

        [TestMethod]
        public void Serialize_CacheItemObject_Succeeds()
        {
            var value = new CustomCacheItem
            {
                Text = "Hello World!"
            };
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);
        }

        [TestMethod]
        public void Deserialize_CacheItem_Succeeds()
        {
            var value = new CustomCacheItem
            {
                Text = "Hello World!"
            };
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);

            var result = serializer.Deserialize<CustomCacheItem>(bytes);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(value.Text, result.Value.Text);
        }

        [TestMethod]
        public void Deserialize_EmptyCollectionOnNull_ReturnsEmpty()
        {
            var value = Array.Empty<int>();
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);

            var result = serializer.Deserialize<ICollection<int>>(bytes);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(0, result.Value.Count);
        }

        [TestMethod]
        public void Deserialize_EmptyCollectionOnNullDisabled_ReturnsNull()
        {
            var value = Array.Empty<int>();
            var serializer = new ProtoBufCacheItemSerializer() { EnableEmptyCollectionOnNull = false };

            var bytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);

            var result = serializer.Deserialize<ICollection<int>>(bytes);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Value);
        }

        [TestMethod]
        public void Deserialize_EmptyCollectionOnNullArray_ReturnsEmpty()
        {
            var value = Array.Empty<int>();
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);

            var result = serializer.Deserialize<int[]>(bytes);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(0, result.Value.Length);
        }

        [TestMethod]
        public void Deserialize_EmptyCollectionOnNullList_ReturnsEmpty()
        {
            var value = Array.Empty<int>();
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);

            var result = serializer.Deserialize<List<int>>(bytes);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(0, result.Value.Count);
        }

        [TestMethod]
        public void Deserialize_EmptyCollectionOnNullString_ReturnsNull()
        {
            var value = Array.Empty<int>();
            var serializer = new ProtoBufCacheItemSerializer();

            var bytes = serializer.Serialize(value, DateTime.UtcNow.AddMinutes(10), DateTime.UtcNow.AddMinutes(20));
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual(0, bytes.Length);

            var result = serializer.Deserialize<ICollection<int>>(bytes);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(0, result.Value.Count);
        }

        [ProtoContract]
        public class CustomCacheItem
        {
            [ProtoMember(1)]
            public string Text { get; set; }
        }
    }
}
