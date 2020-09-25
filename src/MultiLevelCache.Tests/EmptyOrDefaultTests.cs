using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiLevelCaching.Tests
{
    [TestClass]
    public class EmptyOrDefaultTests
    {
        [TestMethod]
        public void EmptyOrDefault_Succeeds()
        {
            Assert.AreEqual(0, EmptyOrDefault<int>.Value);
            Assert.IsNull(EmptyOrDefault<string>.Value);
            Assert.IsNull(EmptyOrDefault<object>.Value);

            Assert.IsNotNull(EmptyOrDefault<int[]>.Value);
            Assert.IsNotNull(EmptyOrDefault<string[]>.Value);
            Assert.IsNotNull(EmptyOrDefault<object[]>.Value);
            Assert.IsNotNull(EmptyOrDefault<IEnumerable>.Value);
            Assert.IsNotNull(EmptyOrDefault<ICollection>.Value);
            Assert.IsNotNull(EmptyOrDefault<IList>.Value);
            Assert.IsNotNull(EmptyOrDefault<IEnumerable<object>>.Value);
            Assert.IsNotNull(EmptyOrDefault<ICollection<object>>.Value);
            Assert.IsNotNull(EmptyOrDefault<IList<object>>.Value);
            Assert.IsNotNull(EmptyOrDefault<List<object>>.Value);

            Assert.AreEqual(0, EmptyOrDefault<int[]>.Value.Length);
            Assert.AreEqual(0, EmptyOrDefault<string[]>.Value.Length);
            Assert.AreEqual(0, EmptyOrDefault<object[]>.Value.Length);
            Assert.AreEqual(0, EmptyOrDefault<IEnumerable<object>>.Value.Count());
            Assert.AreEqual(0, EmptyOrDefault<ICollection<object>>.Value.Count);
            Assert.AreEqual(0, EmptyOrDefault<IList<object>>.Value.Count);
            Assert.AreEqual(0, EmptyOrDefault<List<object>>.Value.Count);
        }
    }
}
