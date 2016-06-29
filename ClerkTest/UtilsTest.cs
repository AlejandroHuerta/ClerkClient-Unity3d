using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClerkTest {
    /// <summary>
    /// Summary description for UtilsTest
    /// </summary>
    [TestClass]
    public class UtilsTest {
        [TestMethod]
        public void UnixTimeToDateTime() {
            var date = new DateTime(2016, 2, 26, 0, 17, 6, DateTimeKind.Utc);
            Assert.AreEqual(date, Clerk.Utils.UnixTimeToDateTime(1456445826000));
        }
    }
}
