using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    [TestFixture]
    public class AsyncDebouncerTests
    {
        [Test]
        public void Test001()
        {
            var timeout = 100;
            var count = 0;
            var debouncer = new AsyncDebouncer(timeout);
            var tasks = new HashSet<Task>();
            for (var a = 0; a < 10; a++)
            {
                Func<Task> task = () =>
                {
                    Interlocked.Increment(ref count);
#if NET40
                    return TaskEx.FromResult(false);
#else
                    return Task.CompletedTask;
#endif
                };
                tasks.Add(debouncer.Exec(task));
            }
            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(1, count, "Expected 1 execution.");
        }

        [Test]
        public void Test002()
        {
            var timeout = 100;
            var count = 0;
            var debouncer = new AsyncDebouncer(timeout);
            var tasks = new HashSet<Task>(); 
            Func<Task> task = () =>
            {
                Interlocked.Increment(ref count);
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            };
            for (var a = 0; a < 10; a++)
            {
                tasks.Add(debouncer.Exec(task));
            }
            tasks.Add(debouncer.ExecNow(task));
            debouncer.Wait();
            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(1, count, "Expected 1 execution.");
        }
    }
}
