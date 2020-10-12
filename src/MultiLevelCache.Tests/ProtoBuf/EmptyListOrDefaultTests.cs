using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLevelCaching.ProtoBuf;
using System.Collections.Generic;

namespace MultiLevelCaching.Tests.ProtoBuf
{
    [TestClass]
    public class EmptyListOrDefaultTests
    {
        [TestMethod]
        public void EmptyListOrDefault_Succeeds()
        {
            Assert.AreEqual(0, ListHelpers.EmptyListOrDefault<int>());
            Assert.IsNull(ListHelpers.EmptyListOrDefault<string>());
            Assert.IsNull(ListHelpers.EmptyListOrDefault<object>());

            Assert.IsNotNull(ListHelpers.EmptyListOrDefault<List<object>>());
            Assert.AreEqual(0, ListHelpers.EmptyListOrDefault<List<object>>().Count);
        }
    }
}
