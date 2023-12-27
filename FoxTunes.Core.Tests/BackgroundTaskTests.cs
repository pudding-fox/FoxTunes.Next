using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [TestFixture]
    public class BackgroundTaskTests : TestBase
    {
        [Test]
        public void BackgroundTaskRespectsConcurrency()
        {
#if NET40
            var tasks = Enumerable.Range(0, 1024).Select(index => TaskEx.Run(async () =>
#else
            var tasks = Enumerable.Range(0, 1024).Select(index => Task.Run(async () =>
#endif
            {
                using (var task = new Task001())
                {
                    task.InitializeComponent(this.Core);
                    await task.Run().ConfigureAwait(false);
                }
            })).ToArray();
            Task.WaitAll(tasks);
            Assert.AreEqual(1024, Task001.Counter);
        }

        public class Task001 : BackgroundTask
        {
            public const string ID = "9353EE88-67FC-4BB7-ADFA-FFA195AAAE91";

            public static volatile int Counter = 0;

            public Task001() : base(ID)
            {
            }

            protected override Task OnRun()
            {
                Counter++;
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
        }
    }
}
