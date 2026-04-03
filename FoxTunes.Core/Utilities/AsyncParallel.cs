using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    public static class AsyncParallel
    {
        public static Task For(int start, int count, Func<int, Task> factory, CancellationToken cancellationToken, ParallelOptions options)
        {
            return ForEach<int>(Enumerable.Range(start, count), factory, cancellationToken, options);
        }

        public static async Task ForEach<T>(IEnumerable<T> source, Func<T, Task> body, CancellationToken cancellationToken, ParallelOptions options)
        {
            var exceptions = new ConcurrentBag<Exception>();
            var tasks = new ConcurrentBag<Task>();
            if (cancellationToken.IsYieldRequested)
            {
                return;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            using (var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism))
            {
                foreach (var item in source)
                {
                    if (cancellationToken.IsYieldRequested)
                    {
                        break;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    await semaphore.WaitAsync().ConfigureAwait(false);
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await body(item).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                    tasks.Add(task);
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
                if (!exceptions.IsEmpty)
                {
                    if (exceptions.Count == 1)
                    {
                        throw exceptions.First();
                    }
                    else
                    {
                        throw new AggregateException(exceptions);
                    }
                }
            }
        }
    }
}