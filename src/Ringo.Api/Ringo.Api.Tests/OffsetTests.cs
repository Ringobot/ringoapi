using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ringo.Api.Models;
using Ringo.Api.Services;
using System;

namespace Ringo.Api.Tests
{
    [TestClass]
    public class OffsetTests
    {
        [TestMethod]
        public void Usage()
        {
            var now = DateTimeOffset.UtcNow;

            var offset = new Offset(
                now, 
                TimeSpan.FromMilliseconds(1000), 
                TimeSpan.FromMilliseconds(100), 
                TimeSpan.FromSeconds(360));

            Assert.IsTrue(offset.PositionNow() > offset.PositionAtEpoch);
            

        }
    }
}
