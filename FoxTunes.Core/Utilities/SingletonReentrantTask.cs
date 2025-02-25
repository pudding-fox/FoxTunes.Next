using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class SingletonReentrantTask : BaseComponent, IDisposable
    {
        public const int TIMEOUT = 1000;

        public const byte PRIORITY_HIGH = 0;

        public const byte PRIORITY_NORMAL = 100;

        public const byte PRIORITY_LOW = 255;

        public static readonly ConcurrentDictionary<string, SingletonReentrantTaskContainer> Instances = new ConcurrentDictionary<string, SingletonReentrantTaskContainer>();

        private SingletonReentrantTask(ICancellable cancellable, string id)
        {
            this.Cancellable = cancellable;
            this.Cancellable.CancellationRequested += this.OnCancellationRequested;
            this.Instance = Instances.AddOrUpdate(id, new SingletonReentrantTaskContainer(id, this), (key, value) =>
            {
                if (!value.Instances.Add(this))
                {
                    throw new InvalidOperationException(string.Format("Failed to register instance with id: {0}", id));
                }
                return value;
            });
        }

        public SingletonReentrantTask(ICancellable cancellable, string id, byte priority, Func<CancellationToken, Task> factory) : this(cancellable, id, TIMEOUT, priority, factory)
        {

        }

        public SingletonReentrantTask(ICancellable cancellable, string id, int timeout, byte priority, Func<CancellationToken, Task> factory) : this(cancellable, id)
        {
            this.Timeout = timeout;
            this.Priority = priority;
            this.Factory = factory;
        }

        public ICancellable Cancellable { get; private set; }

        public SingletonReentrantTaskContainer Instance { get; private set; }

        public int Timeout { get; private set; }

        public byte Priority { get; private set; }

        public Func<CancellationToken, Task> Factory { get; private set; }

        public virtual async Task Run()
        {
            Logger.Write(this, LogLevel.Trace, "Begin executing task: {0} with priority {1}.", this.Instance.Id, this.Priority);
            do
            {
#if NET40
                if (!this.Instance.Semaphore.Wait(this.Timeout))
#else
                if (!await this.Instance.Semaphore.WaitAsync(this.Timeout).ConfigureAwait(false))
#endif
                {
                    Logger.Write(this, LogLevel.Trace, "Failed to acquire lock after {0}ms", this.Timeout);
                    if (object.ReferenceEquals(this, this.Instance.Instances.OrderBy(instance => instance.Priority).FirstOrDefault()))
                    {
                        Logger.Write(this, LogLevel.Trace, "Cancelling other tasks.");
                        this.Instance.CancellationToken.Cancel();
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Trace, "Yielding to other tasks.");
                    }
                    continue;
                }
                Logger.Write(this, LogLevel.Trace, "Acquired lock.");
                var success = true;
                try
                {
                    if (this.Cancellable.IsCancellationRequested)
                    {
                        Logger.Write(this, LogLevel.Trace, "Task was cancelled.");
                        return;
                    }
                    this.Instance.CancellationToken.Reset();
                    await this.Factory(this.Instance.CancellationToken).ConfigureAwait(false);
                    if (this.Instance.CancellationToken.IsCancellationRequested)
                    {
                        Logger.Write(this, LogLevel.Trace, "Task was cancelled.");
                        success = false;
                    }
                }
                finally
                {
                    Logger.Write(this, LogLevel.Trace, "Releasing lock.");
                    this.Instance.Semaphore.Release();
                }
                if (success)
                {
                    break;
                }
                else
                {
                    Logger.Write(this, LogLevel.Trace, "Retrying in {0}ms", this.Timeout);
#if NET40
                    await TaskEx.Delay(this.Timeout).ConfigureAwait(false);
#else
                    await Task.Delay(this.Timeout).ConfigureAwait(false);
#endif
                }
            } while (true);
            Logger.Write(this, LogLevel.Trace, "Task was executed successfully.");
        }

        protected virtual void OnCancellationRequested(object sender, EventArgs e)
        {
            this.Instance.CancellationToken.Cancel();
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
            var value = default(SingletonReentrantTaskContainer);
            if (this.Cancellable != null)
            {
                this.Cancellable.CancellationRequested -= this.OnCancellationRequested;
            }
            if (!Instances.TryGetValue(this.Instance.Id, out value) || !value.Instances.Remove(this))
            {
                throw new InvalidOperationException(string.Format("Failed to unregister instance with id: {0}", this.Instance.Id));
            }
        }

        ~SingletonReentrantTask()
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

        public class SingletonReentrantTaskContainer
        {
            private SingletonReentrantTaskContainer()
            {
                this.Semaphore = new SemaphoreSlim(1, 1);
                this.CancellationToken = new CancellationToken();
            }

            public SingletonReentrantTaskContainer(string id, SingletonReentrantTask instance) : this()
            {
                this.Id = id;
                this.Instances = new HashSet<SingletonReentrantTask>(new[] { instance });
            }

            public SemaphoreSlim Semaphore { get; private set; }

            public CancellationToken CancellationToken { get; private set; }

            public string Id { get; private set; }

            public HashSet<SingletonReentrantTask> Instances { get; private set; }
        }
    }
}
