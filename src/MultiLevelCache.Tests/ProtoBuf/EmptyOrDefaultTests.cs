using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLevelCaching.ProtoBuf;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MultiLevelCaching.Tests.ProtoBuf
{
    [TestClass]
    public class EmptyOrDefaultTests
    {
        [TestMethod]
        public void EmptyOrDefault_Succeeds()
        {
            Assert.AreEqual(0, CollectionHelpers.EmptyOrDefault<int>());
            Assert.IsNull(CollectionHelpers.EmptyOrDefault<string>());
            Assert.IsNull(CollectionHelpers.EmptyOrDefault<object>());

            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<int[]>());
            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<string[]>());
            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<object[]>());
            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<IEnumerable>());
            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<ICollection>());
            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<IList>());
            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<IEnumerable<object>>());
            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<ICollection<object>>());
            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<IList<object>>());

            Assert.AreEqual(0, CollectionHelpers.EmptyOrDefault<int[]>().Length);
            Assert.AreEqual(0, CollectionHelpers.EmptyOrDefault<string[]>().Length);
            Assert.AreEqual(0, CollectionHelpers.EmptyOrDefault<object[]>().Length);
            Assert.AreEqual(0, CollectionHelpers.EmptyOrDefault<IEnumerable<object>>().Count());
            Assert.AreEqual(0, CollectionHelpers.EmptyOrDefault<ICollection<object>>().Count);
            Assert.AreEqual(0, CollectionHelpers.EmptyOrDefault<IList<object>>().Count);

            // List type should return an empty List.
            Assert.IsNotNull(CollectionHelpers.EmptyOrDefault<List<object>>());
            Assert.AreEqual(0, CollectionHelpers.EmptyOrDefault<List<object>>().Count);
        }
    }
}
