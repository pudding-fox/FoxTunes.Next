using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    [TestFixture]
    public class PendingQueueTests
    {
        public ParallelOptions ParallelOptions
        {
            get
            {
                return new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };
            }
        }

        [Test]
        public void CanPendValues()
        {
            var count = default(int);
            var queue = new PendingQueue<int>(TimeSpan.FromSeconds(1));
            queue.Complete += (sender, e) =>
            {
                Assert.AreEqual(100, e.Sequence.Count());
                Interlocked.Increment(ref count);
            };
            Parallel.For(0, 100, index => queue.Enqueue(index));
            Thread.Sleep(TimeSpan.FromSeconds(10));
            Assert.AreEqual(1, count);
        }
    }
}
