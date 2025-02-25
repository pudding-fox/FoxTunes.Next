#if NET40
using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace FoxTunes
{
    public class TrackingThreadLocal<T> : IDisposable
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public TrackingThreadLocal()
        {
            this.ThreadLocal = new ThreadLocal<T>();
            this.Values = new ConcurrentBag<T>();
        }

        public ThreadLocal<T> ThreadLocal { get; private set; }

        public ConcurrentBag<T> Values { get; private set; }

        public bool IsValueCreated
        {
            get
            {
                return this.ThreadLocal.IsValueCreated;
            }
        }

        public T Value
        {
            get
            {
                return this.ThreadLocal.Value;
            }
            set
            {
                this.ThreadLocal.Value = value;
                this.Values.Add(value);
            }
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
            this.ThreadLocal.Dispose();
        }

        ~TrackingThreadLocal()
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
#endif