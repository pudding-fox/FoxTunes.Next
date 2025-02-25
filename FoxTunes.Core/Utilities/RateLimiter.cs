using FoxTunes.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class RateLimiter : IDisposable
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public RateLimiter(int interval)
        {
            this.Interval = interval;
            this.WaitHandle = new AutoResetEvent(true);
        }

        public RateLimiter(TimeSpan interval) : this(Convert.ToInt32(interval.TotalMilliseconds))
        {

        }

        public int Interval { get; set; }

        public AutoResetEvent WaitHandle { get; private set; }

        public void Exec(Action action)
        {
            this.WaitHandle.WaitOne();
            action();
#if NET40
            var task = TaskEx.Run(async () =>
#else
            var task = Task.Run(async () =>
#endif
            {
                try
                {
                    if (this.IsDisposed)
                    {
                        return;
                    }
#if NET40
                    await TaskEx.Delay(this.Interval).ConfigureAwait(false);
#else
                    await Task.Delay(this.Interval).ConfigureAwait(false);
#endif
                    if (this.IsDisposed)
                    {
                        return;
                    }
                    this.WaitHandle.Set();
                }
                catch
                {
                    //Nothing can be done.
                }
            });
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.WaitHandle != null)
            {
                this.WaitHandle.Dispose();
            }
        }

        ~RateLimiter()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
