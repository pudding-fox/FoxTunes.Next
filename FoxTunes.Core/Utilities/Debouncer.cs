using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
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

        private Debouncer()
        {
            this.Actions = new HashSet<Action>(ActionComparer.Instance);
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

        public void Exec(Action action)
        {
            lock (SyncRoot)
            {
                this.Actions.Add(action);
                if (this.Timer != null)
                {
                    this.Timer.Stop();
                    this.Timer.Start();
                }
            }
        }

        public void ExecNow(Action action)
        {
            lock (SyncRoot)
            {
                this.Actions.Remove(action);
                action();
            }
        }

        public void Cancel(Action action)
        {
            lock (SyncRoot)
            {
                this.Actions.Remove(action);
            }
        }

        protected virtual void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (SyncRoot)
                {
                    foreach (var action in this.Actions)
                    {
                        action();
                    }
                    this.Actions.Clear();
                }
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        public HashSet<Action> Actions { get; private set; }

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

        public class ActionComparer : IEqualityComparer<Action>
        {
            public bool Equals(Action x, Action y)
            {
                return x.Method.Equals(y.Method);
            }

            public int GetHashCode(Action obj)
            {
                return obj.Method.GetHashCode();
            }

            public static readonly IEqualityComparer<Action> Instance = new ActionComparer();
        }
    }
}
