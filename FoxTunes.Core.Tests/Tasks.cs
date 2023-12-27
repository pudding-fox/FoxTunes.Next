using NUnit.Framework;
using System.Threading.Tasks;

namespace FoxTunes
{
    [TestFixture]
    public class Tasks : TestBase
    {
        [Test]
        public async Task BackgroundTaskBlocks()
        {
            using (var task = new TestTask())
            {
                task.InitializeComponent(this.Core);
                await task.Run().ConfigureAwait(false);
                Assert.IsTrue(task.CompletedA);
                Assert.IsTrue(task.CompletedB);
            }
        }

        private class TestTask : BackgroundTask
        {
            public const string ID = "6EB0AEBB-AA4C-4A02-9421-DB9BEF04BF3E";

            public TestTask() : base(ID)
            {
                this.CompletedA = false;
                this.CompletedB = false;
            }

            public volatile bool CompletedA;

            public volatile bool CompletedB;

            public override async Task Run()
            {
                this.CompletedA = false;
                this.CompletedB = false;
                try
                {
                    await base.Run().ConfigureAwait(false);
                }
                finally
                {
                    this.CompletedB = true;
                }
            }

            protected override async Task OnRun()
            {
#if NET40
                await TaskEx.Delay(100).ConfigureAwait(false);
#else
                await Task.Delay(100).ConfigureAwait(false);
#endif
                this.CompletedA = true;
            }
        }
    }
}
