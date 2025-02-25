using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [TestFixture]
    public class ReentrantTaskTests
    {
        [Test]
        public async Task SingletonReentrantTaskRespectsPriority()
        {
            var counter1 = 0;
            var counter2 = 0;
            var id = "5C8A060E-885C-4FF1-ABEB-DB046D2D8D1C";
            var task1 = new SingletonReentrantTask(Placeholder.Instance, id, 10, SingletonReentrantTask.PRIORITY_LOW, async cancellationToken =>
            {
                for (; counter1 + counter2 < 10; counter1++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
#if NET40
                    await TaskEx.Delay(100).ConfigureAwait(false);
#else
                    await Task.Delay(100).ConfigureAwait(false);
#endif
                }
            });
            var task2 = new SingletonReentrantTask(Placeholder.Instance, id, 10, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
            {
                for (; counter1 + counter2 < 10; counter2++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
#if NET40
                    await TaskEx.Delay(100).ConfigureAwait(false);
#else
                    await Task.Delay(100).ConfigureAwait(false);
#endif
                }
            });
#if NET40
            await TaskEx.WhenAll(task1.Run(), task2.Run()).ConfigureAwait(false);
#else
            await Task.WhenAll(task1.Run(), task2.Run()).ConfigureAwait(false);
#endif
            Assert.AreEqual(10, counter1 + counter2);
            Assert.Less(counter1, counter2);
        }

        public class Placeholder : BackgroundTask
        {
            public const string ID = "6B056EFA-4341-4B0E-BFB0-78935EEA752C";

            private Placeholder() : base(ID)
            {

            }

            protected override Task OnRun()
            {
                throw new NotImplementedException();
            }

            public static readonly Placeholder Instance = new Placeholder();
        }
    }
}
