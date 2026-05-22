using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace FoxTunes
{
    public class Debouncer : IDisposable
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static readonly object SyncRoot = new object();

        public Debouncer(int timeout)
        {
            this.Timer = new global::System.Timers.Timer(timeout);
            this.Timer.AutoReset = false;
            this.Timer.Elapsed += this.OnElapsed;
        }

        public Debouncer(TimeSpan timeout) : this(Convert.ToInt32(timeout.TotalMilliseconds))
        {

        }

        public void Exec(Action action)
        {
            lock (SyncRoot)
            {
                this.Action = action;
                if (this.Timer != null)
                {
                    this.Timer.Stop();
                    this.Timer.Start();
                }
            }
        }

        public void Cancel(Action action)
        {
            lock (SyncRoot)
            {
                this.Action = action;
            }
        }

        protected virtual void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (SyncRoot)
                {
                    if (this.Action != null)
                    {
                        this.Action();
                        this.Action = null;
                    }
                }
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        public Action Action { get; private set; }

        public global::System.Timers.Timer Timer { get; private set; }

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
            lock (SyncRoot)
            {
                if (this.Timer != null)
                {
                    this.Timer.Elapsed -= this.OnElapsed;
                    this.Timer.Dispose();
                    this.Timer = null;
                }
            }
            //Execute any pending actions.
            this.OnElapsed(this, default(ElapsedEventArgs));
        }

        ~Debouncer()
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

    public class Debouncer<T1, T2> : IDisposable
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        private readonly object SyncRoot = new object();

        private List<T1> Arg1 { get; set; }

        private List<T2> Arg2 { get; set; }

        private Action<IEnumerable<T1>, IEnumerable<T2>> Action { get; set; }

        private Debouncer()
        {
            this.Arg1 = new List<T1>();
            this.Arg2 = new List<T2>();
        }

        public Debouncer(int timeout) : this()
        {
            this.Timer = new global::System.Timers.Timer(timeout);
            this.Timer.AutoReset = false;
            this.Timer.Elapsed += this.OnElapsed;
        }

        public Debouncer(TimeSpan timeout) : this(Convert.ToInt32(timeout.TotalMilliseconds))
        {

        }

        protected global::System.Timers.Timer Timer { get; private set; }

        public void Exec(Action<IEnumerable<T1>, IEnumerable<T2>> action, IEnumerable<T1> arg1, IEnumerable<T2> arg2)
        {
            lock (this.SyncRoot)
            {
                this.Action = action;
                this.Arg1.AddRange(arg1);
                this.Arg2.AddRange(arg2);
                this.Timer.Stop();
                this.Timer.Start();
            }
        }

        public void Cancel()
        {
            lock (this.SyncRoot)
            {
                this.Timer.Stop();
                this.Action = null;
                this.Arg1.Clear();
                this.Arg2.Clear();
            }
        }

        protected virtual void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (this.SyncRoot)
                {
                    if (this.Action != null)
                    {
                        this.Action(this.Arg1.ToArray(), this.Arg2.ToArray());
                        this.Action = null;
                    }
                    this.Arg1.Clear();
                    this.Arg2.Clear();
                }
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
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
            lock (SyncRoot)
            {
                if (this.Timer != null)
                {
                    this.Timer.Elapsed -= this.OnElapsed;
                    this.Timer.Dispose();
                    this.Timer = null;
                }
            }
        }

        ~Debouncer()
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
