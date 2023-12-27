using NUnit.Framework;
using System;
using System.Diagnostics;

namespace FoxTunes
{
    [TestFixture]
    public class RateLimiterTests
    {
        [Test]
        public void Test001()
        {
            var stopwatch = Stopwatch.StartNew();
            var interval = 100;
            var count = 0;
            var rateLimiter = new RateLimiter(interval);
            for (var a = 0; a < 10; a++)
            {
                var action = new Action(() =>
                {
                    count++;
                });
                rateLimiter.Exec(action);
            }
            stopwatch.Stop();
            Assert.AreEqual(10, count);
            Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 900);
        }
    }
}
