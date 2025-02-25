using NUnit.Framework;

namespace FoxTunes
{
    [TestFixture]
    public class MetricTests
    {
        [Test]
        public void CanAverage()
        {
            var metric = new Metric(10);
            for (var a = 0; a < 10; a++)
            {
                metric.Append(a);
            }
            Assert.AreEqual(6, metric.Average(10));
        }

        [Test]
        public void CanNextInc()
        {
            var metric = new Metric(10);
            for (var a = 0; a < 10; a++)
            {
                metric.Append(a);
            }
            Assert.AreEqual(11, metric.Next(10));
        }


        [Test]
        public void CanNextDec()
        {
            var metric = new Metric(10);
            for (var a = 10; a > 0; a--)
            {
                metric.Append(a);
            }
            Assert.AreEqual(-1, metric.Next(0));
        }
    }
}
