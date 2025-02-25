using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class AsyncEventArgs<T> : EventArgs
    {
        protected AsyncEventArgs()
        {
            this.Sources = new ConcurrentBag<TaskCompletionSource<T>>();
        }

        public Deferral<T> Defer()
        {
            var source = new TaskCompletionSource<T>();
            var deferral = new Deferral<T>(result => source.SetResult(result));
            if (!this.Sources.TryAdd(source))
            {
                //TODO: Warn.
            }
            return deferral;
        }

        public IProducerConsumerCollection<TaskCompletionSource<T>> Sources { get; private set; }

        public async Task Complete()
        {
            foreach (var source in this.Sources)
            {
                await source.Task.ConfigureAwait(false);
            }
        }
    }

    public delegate void AsyncEventHandler<T>(object sender, AsyncEventArgs<T> e);

    public class AsyncEventArgs : AsyncEventArgs<object>
    {
    }

    public delegate void AsyncEventHandler(object sender, AsyncEventArgs e);
}
