using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MultiLevelCaching.ProtoBuf.Tests
{
    [TestClass]
    public class EmptyArrayOrDefaultTests
    {
        [TestMethod]
        public void EmptyArrayOrDefault_Succeeds()
        {
            Assert.AreEqual(0, EmptyArrayOrDefault<int>.Value);
            Assert.IsNull(EmptyArrayOrDefault<string>.Value);
            Assert.IsNull(EmptyArrayOrDefault<object>.Value);

            Assert.IsNotNull(EmptyArrayOrDefault<int[]>.Value);
            Assert.IsNotNull(EmptyArrayOrDefault<string[]>.Value);
            Assert.IsNotNull(EmptyArrayOrDefault<object[]>.Value);
            Assert.IsNotNull(EmptyArrayOrDefault<IEnumerable>.Value);
            Assert.IsNotNull(EmptyArrayOrDefault<ICollection>.Value);
            Assert.IsNotNull(EmptyArrayOrDefault<IList>.Value);
            Assert.IsNotNull(EmptyArrayOrDefault<IEnumerable<object>>.Value);
            Assert.IsNotNull(EmptyArrayOrDefault<ICollection<object>>.Value);
            Assert.IsNotNull(EmptyArrayOrDefault<IList<object>>.Value);

            Assert.AreEqual(0, EmptyArrayOrDefault<int[]>.Value.Length);
            Assert.AreEqual(0, EmptyArrayOrDefault<string[]>.Value.Length);
            Assert.AreEqual(0, EmptyArrayOrDefault<object[]>.Value.Length);
            Assert.AreEqual(0, EmptyArrayOrDefault<IEnumerable<object>>.Value.Count());
            Assert.AreEqual(0, EmptyArrayOrDefault<ICollection<object>>.Value.Count);
            Assert.AreEqual(0, EmptyArrayOrDefault<IList<object>>.Value.Count);

            // List type should return null.
            Assert.IsNull(EmptyArrayOrDefault<List<object>>.Value);
        }
    }
}
